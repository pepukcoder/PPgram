package router

import (
	"context"
	"errors"
	"fmt"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/core/session"
	"github.com/ppgram/server/protomsg"
	"github.com/ppgram/server/transport"
	"google.golang.org/protobuf/proto"
)

var (
	ErrResponseBodyRequired = errors.New("response body is required")
	ErrUpdateBodyRequired   = errors.New("update body is required")
	ErrInvalidSequence      = errors.New("invalid sequence")
)

type Middleware func(*BaseContext) error
type Handler func(*BaseContext) error

type BaseContext struct {
	context.Context

	stream  *transport.QuicStream
	Op      route
	Bytes   []byte
	Session *session.DeviceSession

	params map[string]string
	values map[string]any

	chain    []Middleware
	endpoint Handler
	index    int
}

func newBaseContext(ctx context.Context, stream *transport.QuicStream, chain []Middleware, endpoint Handler) *BaseContext {
	if ctx == nil {
		ctx = context.Background()
	}

	return &BaseContext{
		Context:  ctx,
		stream:   stream,
		params:   map[string]string{},
		values:   map[string]any{},
		chain:    chain,
		endpoint: endpoint,
		index:    -1,
	}
}

func (c *BaseContext) Next() error {
	c.index++
	if c.index >= len(c.chain) {
		if c.endpoint != nil {
			return c.endpoint(c)
		}
		return nil
	}
	return c.chain[c.index](c)
}

func (c *BaseContext) SetValue(key string, value any) {
	c.values[key] = value
}

func (c *BaseContext) GetValue(key string) (any, bool) {
	value, ok := c.values[key]
	return value, ok
}

func (c *BaseContext) GetParam(key string) string {
	return c.params[key]
}

func (c *BaseContext) SetParam(key, value string) {
	c.params[key] = value
}

func (c *BaseContext) Close() error {
	return c.stream.Close()
}

func (c *BaseContext) writeEnvelope(envelope proto.Message) error {
	bytes, err := proto.Marshal(envelope)
	if err != nil {
		return err
	}

	return c.stream.WriteFrame(transport.Frame{
		FrameType: transport.FrameTypeData,
		Payload:   bytes,
	})
}

func (c *BaseContext) SendResponse(statusCode core.PPStatusCode, message string, response proto.Message) error {
	var responseBytes []byte
	if response != nil {
		bytes, err := proto.Marshal(response)
		if err != nil {
			return err
		}
		responseBytes = bytes
	}

	return c.writeEnvelope(&protomsg.ResponseEnvelope{
		StatusCode: uint32(statusCode),
		Message:    message,
		Response:   responseBytes,
	})
}

func (c *BaseContext) SendUpdate(seq uint64, end bool, update proto.Message) error {
	if c.Session != nil && c.Session.IsRevoked() {
		return session.ErrSessionRevoked
	}

	updateBytes, err := proto.Marshal(update)
	if err != nil {
		return err
	}

	return c.writeEnvelope(&protomsg.UpdateEnvelope{
		Seq:    seq,
		End:    end,
		Update: updateBytes,
	})
}

func (c *BaseContext) ReadUpdate() (*protomsg.UpdateEnvelope, error) {
	if c.Session != nil && c.Session.IsRevoked() {
		return nil, session.ErrSessionRevoked
	}

	frame, err := c.stream.ReadFrame()
	if err != nil {
		return nil, err
	}

	envelope := &protomsg.UpdateEnvelope{}
	if err := proto.Unmarshal(frame.Payload, envelope); err != nil {
		return nil, fmt.Errorf("decode update envelope: %w", err)
	}

	return envelope, nil
}
