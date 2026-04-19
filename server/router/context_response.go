package router

import (
	"github.com/ppgram/server/core"
	"github.com/ppgram/server/core/session"
	"google.golang.org/protobuf/proto"
)

type ResponseContext struct {
	base    *BaseContext
	Request []byte
	Session *session.DeviceSession
}

func NewResponseContext(base *BaseContext) (*ResponseContext, error) {
	return &ResponseContext{
		base:    base,
		Request: base.Bytes,
		Session: base.Session,
	}, nil
}

func (c *ResponseContext) Send(statusCode core.PPStatusCode, message string, response proto.Message) error {
	return c.base.SendResponse(statusCode, message, response)
}

func (c *ResponseContext) SendStatus(statusCode core.PPStatusCode, message string) error {
	return c.base.SendResponse(statusCode, message, nil)
}

func (c *ResponseContext) SendCode(statusCode core.PPStatusCode) error {
	return c.base.SendResponse(statusCode, "", nil)
}
