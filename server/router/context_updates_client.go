package router

import (
	"fmt"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
	"google.golang.org/protobuf/proto"
)

type ClientUpdatesContext struct {
	base *BaseContext

	Request *protomsg.ClientUpdateRequest
	Seq     uint64
	Update  []byte
	ended   bool

	nextSeq uint64
}

func NewClientUpdatesContext(base *BaseContext) (*ClientUpdatesContext, error) {
	request, err := decodeClientUpdatesRequest(base.Bytes)
	if err != nil {
		return nil, err
	}
	return &ClientUpdatesContext{base: base, Request: request}, nil
}

func (c *ClientUpdatesContext) Send(statusCode core.PPStatusCode, message string, response *protomsg.ClientUpdateResponse) error {
	if response == nil {
		return ErrResponseBodyRequired
	}
	return c.base.SendResponse(statusCode, message, response)
}

func (c *ClientUpdatesContext) NextUpdate() (last bool, err error) {
	if c.ended {
		return true, nil
	}

	envelope, err := c.base.ReadUpdate()
	if err != nil {
		return false, err
	}

	if envelope.GetSeq() != c.nextSeq {
		return false, ErrInvalidSequence
	}

	c.nextSeq++
	c.Seq = envelope.GetSeq()
	c.Update = envelope.GetUpdate()
	c.ended = envelope.GetEnd()
	return c.ended, nil
}

func decodeClientUpdatesRequest(raw []byte) (*protomsg.ClientUpdateRequest, error) {
	request := &protomsg.ClientUpdateRequest{}
	if err := proto.Unmarshal(raw, request); err != nil {
		return nil, fmt.Errorf("decode client update request: %w", err)
	}
	return request, nil
}
