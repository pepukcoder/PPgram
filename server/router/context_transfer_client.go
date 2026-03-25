package router

import (
	"fmt"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
	"google.golang.org/protobuf/proto"
)

type ClientTransferContext struct {
	base    *BaseContext
	Request *protomsg.ClientTransferRequest
}

func NewClientTransferContext(base *BaseContext) (*ClientTransferContext, error) {
	request, err := decodeClientTransferRequest(base.Bytes)
	if err != nil {
		return nil, err
	}
	return &ClientTransferContext{base: base, Request: request}, nil
}

func (c *ClientTransferContext) Send(statusCode core.PPStatusCode, message string, response *protomsg.ClientTransferResponse) error {
	if response == nil {
		return ErrResponseBodyRequired
	}
	return c.base.sendResponse(statusCode, message, response)
}

func decodeClientTransferRequest(raw []byte) (*protomsg.ClientTransferRequest, error) {
	request := &protomsg.ClientTransferRequest{}
	if err := proto.Unmarshal(raw, request); err != nil {
		return nil, fmt.Errorf("decode client transfer request: %w", err)
	}
	return request, nil
}
