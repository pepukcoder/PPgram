package handlers

import (
	"github.com/ppgram/server/core"
	"github.com/ppgram/server/router"
)

func PingHandler(c *router.ResponseContext) error {
	return c.SendStatus(core.ErrCodeOK, "pong")
}
