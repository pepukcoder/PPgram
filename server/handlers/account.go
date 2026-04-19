package handlers

import (
	"errors"

	authsvc "github.com/ppgram/server/auth"
	"github.com/ppgram/server/core"
	"github.com/ppgram/server/core/session"
	"github.com/ppgram/server/protomsg"
	"github.com/ppgram/server/router"
	"google.golang.org/protobuf/proto"
)

func HandleAccountDelete(c *router.ResponseContext) error {
	var req protomsg.AccountDeleteRequest
	if err := proto.Unmarshal(c.Request, &req); err != nil {
		return c.SendCode(core.ErrCodeBadRequest)
	}

	timestamp, err := authsvc.ScheduleAccountDeletion(req.GetUsername(), req.GetPassword())
	if err != nil {
		if errors.Is(err, authsvc.ErrInvalidCredentials) {
			return c.SendCode(core.ErrCodeInvalidCredentials)
		}
		return c.SendCode(core.ErrCodeInternal)
	}

	if err := c.Send(core.ErrCodeOK, "", &protomsg.AccountDeleteResponse{Timestamp: timestamp}); err != nil {
		return err
	}

	if c.Session != nil {
		session.InvalidateUserSessions(c.Session.UserID())
	}

	return nil
}

func HandleAccountDeleteCancel(c *router.ResponseContext) error {
	var req protomsg.AccountDeleteCancelRequest
	if err := proto.Unmarshal(c.Request, &req); err != nil {
		return c.SendCode(core.ErrCodeBadRequest)
	}

	err := authsvc.CancelAccountDeletion(req.GetUsername(), req.GetPassword())
	if err != nil {
		if errors.Is(err, authsvc.ErrInvalidCredentials) {
			return c.SendCode(core.ErrCodeInvalidCredentials)
		}
		if errors.Is(err, authsvc.ErrAccountDeletionInProgress) {
			return c.SendStatus(core.ErrCodeForbidden, "account deletion already started")
		}
		return c.SendCode(core.ErrCodeInternal)
	}

	return c.SendCode(core.ErrCodeOK)
}
