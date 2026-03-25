package router

import (
	"context"

	protomsg "github.com/ppgram/server/protomsg"
	"github.com/ppgram/server/transport"
	"google.golang.org/protobuf/proto"
)

type Middleware func(*BaseContext) error
type Handler func(*BaseContext) error

type BaseContext struct {
	context.Context

	Request  *Request
	Response *Response
	Stream   *transport.QuicStream

	params map[string]string
	values map[string]any

	chain    []Middleware
	endpoint Handler
	index    int
}

func newBaseContext(ctx context.Context, request *Request, stream *transport.QuicStream, chain []Middleware, endpoint Handler) *BaseContext {
	if ctx == nil {
		ctx = context.Background()
	}

	return &BaseContext{
		Context:  ctx,
		Request:  request,
		Response: &Response{},
		Stream:   stream,
		params:   map[string]string{},
		values:   map[string]any{},
		chain:    chain,
		endpoint: endpoint,
		index:    -1,
	}
}

func (c *BaseContext) Next() error {
	c.index++
	if c.index < len(c.chain) {
		return c.chain[c.index](c)
	}
	if c.endpoint != nil {
		return c.endpoint(c)
	}
	return nil
}

func (c *BaseContext) Value(key string, value any) {
	c.values[key] = value
}

func (c *BaseContext) GetValue(key string) (any, bool) {
	value, ok := c.values[key]
	return value, ok
}

func (c *BaseContext) Param(key string) string {
	return c.params[key]
}

func (c *BaseContext) SetParam(key, value string) {
	c.params[key] = value
}

func (c *BaseContext) Close() error {
	return c.Stream.Close()
}

func (c *BaseContext) StatusCode(code uint32) *BaseContext {
	c.Response.StatusCode = code
	return c
}

func (c *BaseContext) Message(message string) *BaseContext {
	c.Response.Message = message
	return c
}

func (c *BaseContext) Payload(payload []byte) *BaseContext {
	c.Response.Body = payload
	return c
}

func (c *BaseContext) Send() error {
	return writeResponseFrame(c.Stream, c.Response)
}

func (c *BaseContext) SendResponse(payload []byte) error {
	return c.Payload(payload).Send()
}

type ResponseContext struct{ *BaseContext }
type ServerStreamContext struct{ *BaseContext }
type ClientStreamContext struct{ *BaseContext }
type BidirectionalContext struct{ *BaseContext }
type ServerTransferContext struct{ *BaseContext }
type ClientTransferContext struct{ *BaseContext }

func writeResponseFrame(stream *transport.QuicStream, response *Response) error {
	pbResponse := &protomsg.Response{}
	if response != nil {
		pbResponse.StatusCode = response.StatusCode
		pbResponse.Message = response.Message
		pbResponse.Body = response.Body
	}

	buf, err := proto.Marshal(pbResponse)
	if err != nil {
		return err
	}

	return stream.WriteFrame(transport.FrameTypeData, buf)
}
