package router

import (
	"fmt"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
	"google.golang.org/protobuf/proto"
)

type ClientUpdatesContext struct {
	base    *BaseContext
	Request *protomsg.ClientUpdateRequest
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
	return c.base.sendResponse(statusCode, message, response)
}

func (c *ClientUpdatesContext) SendUpdate(seq uint64, end bool, update *protomsg.ClientUpdateResponse) error {
	if update == nil {
		return ErrUpdateBodyRequired
	}
	return c.base.sendUpdate(seq, end, update)
}

func decodeClientUpdatesRequest(raw []byte) (*protomsg.ClientUpdateRequest, error) {
	request := &protomsg.ClientUpdateRequest{}
	if err := proto.Unmarshal(raw, request); err != nil {
		return nil, fmt.Errorf("decode client update request: %w", err)
	}
	return request, nil
}
