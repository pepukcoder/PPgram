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
	return c.base.sendResponse(statusCode, message, response)
}

func (c *ServerUpdatesContext) SendUpdate(seq uint64, end bool, update *protomsg.ServerUpdateResponse) error {
	if update == nil {
		return ErrUpdateBodyRequired
	}
	return c.base.sendUpdate(seq, end, update)
}

func decodeServerUpdatesRequest(raw []byte) (*protomsg.ServerUpdateRequest, error) {
	request := &protomsg.ServerUpdateRequest{}
	if err := proto.Unmarshal(raw, request); err != nil {
		return nil, fmt.Errorf("decode server update request: %w", err)
	}
	return request, nil
}
