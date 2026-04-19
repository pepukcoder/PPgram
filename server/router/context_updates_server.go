package router

import (
	"fmt"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
	"google.golang.org/protobuf/proto"
)

type ServerUpdatesContext struct {
	base    *BaseContext
	Request *protomsg.ServerUpdateRequest

	nextSeq uint64
	ended   bool
}

func NewServerUpdatesContext(base *BaseContext) (*ServerUpdatesContext, error) {
	request, err := decodeServerUpdatesRequest(base.Bytes)
	if err != nil {
		return nil, err
	}
	return &ServerUpdatesContext{base: base, Request: request}, nil
}

func (c *ServerUpdatesContext) Send(statusCode core.PPStatusCode, message string, response *protomsg.ServerUpdateResponse) error {
	if response == nil {
		return ErrResponseBodyRequired
	}
	return c.base.SendResponse(statusCode, message, response)
}

func (c *ServerUpdatesContext) SendUpdate(end bool, update *protomsg.ServerUpdateResponse) error {
	if c.ended {
		return nil
	}

	if update == nil {
		return ErrUpdateBodyRequired
	}

	if err := c.base.SendUpdate(c.nextSeq, end, update); err != nil {
		return err
	}

	c.nextSeq++
	c.ended = end
	return nil
}

func decodeServerUpdatesRequest(raw []byte) (*protomsg.ServerUpdateRequest, error) {
	request := &protomsg.ServerUpdateRequest{}
	if err := proto.Unmarshal(raw, request); err != nil {
		return nil, fmt.Errorf("decode server update request: %w", err)
	}
	return request, nil
}
