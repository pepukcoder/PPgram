package middleware

import (
	"github.com/ppgram/server/router"
)

func AuthMiddleware(c *router.BaseContext) error {
	return c.Next()
}
