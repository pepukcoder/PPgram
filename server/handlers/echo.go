package handlers

import "github.com/ppgram/server/router"

func Echo(c *router.ResponseContext) error {
	return c.StatusCode(0).Message("ok").Payload(c.Request.Body).Send()
}
