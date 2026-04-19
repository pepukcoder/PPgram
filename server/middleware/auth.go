package middleware

import (
	db "github.com/ppgram/database"
	"github.com/ppgram/server/core"
	"github.com/ppgram/server/router"
)

func AuthMiddleware(c *router.BaseContext) error {
	if c.Session == nil || !c.Session.IsAuthenticated() {
		return c.SendResponse(core.ErrCodeUnauthorized, "", nil)
	}

	defaultDB, err := db.DefaultDB()
	if err != nil {
		return c.SendResponse(core.ErrCodeInternal, "", nil)
	}

	pending, err := defaultDB.PendingAccountDeletions.ExistsActiveByUserID(c.Session.UserID())
	if err != nil {
		return c.SendResponse(core.ErrCodeInternal, "", nil)
	}
	if pending {
		return c.SendResponse(core.ErrCodeUnauthorized, "", nil)
	}

	return c.Next()
}
