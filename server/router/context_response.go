package router

import (
	"github.com/ppgram/server/core"
	"google.golang.org/protobuf/proto"
)

type ResponseContext struct {
	base    *BaseContext
	Request []byte
}

func NewResponseContext(base *BaseContext) (*ResponseContext, error) {
	return &ResponseContext{base: base, Request: base.Bytes}, nil
}

func (c *ResponseContext) Send(statusCode core.PPStatusCode, message string, response proto.Message) error {
	return c.base.sendResponse(statusCode, message, response)
}

func (c *ResponseContext) SendStatus(statusCode core.PPStatusCode, message string) error {
	return c.Send(statusCode, message, nil)
}

func (c *ResponseContext) SendCode(statusCode core.PPStatusCode) error {
	return c.SendStatus(statusCode, "")
}

func (c *ResponseContext) SendMessage(message string) error {
	return c.SendStatus(core.ErrCodeOK, message)
}
