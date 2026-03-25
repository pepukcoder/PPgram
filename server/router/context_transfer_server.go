package router

import (
	"fmt"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
	"google.golang.org/protobuf/proto"
)

type ServerTransferContext struct {
	base    *BaseContext
	Request *protomsg.ServerTransferRequest
}

func NewServerTransferContext(base *BaseContext) (*ServerTransferContext, error) {
	request, err := decodeServerTransferRequest(base.Bytes)
	if err != nil {
		return nil, err
	}
	return &ServerTransferContext{base: base, Request: request}, nil
}

func (c *ServerTransferContext) Send(statusCode core.PPStatusCode, message string, response *protomsg.ServerTransferResponse) error {
	if response == nil {
		return ErrResponseBodyRequired
	}
	return c.base.sendResponse(statusCode, message, response)
}

func decodeServerTransferRequest(raw []byte) (*protomsg.ServerTransferRequest, error) {
	request := &protomsg.ServerTransferRequest{}
	if err := proto.Unmarshal(raw, request); err != nil {
		return nil, fmt.Errorf("decode server transfer request: %w", err)
	}
	return request, nil
}
