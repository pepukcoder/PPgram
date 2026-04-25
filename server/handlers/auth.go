package handlers

import (
	"errors"

	db "github.com/ppgram/database"
	authsvc "github.com/ppgram/server/auth"
	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
	"github.com/ppgram/server/router"
	"google.golang.org/protobuf/proto"
)

func HandleUserRegister(c *router.ResponseContext) error {
	var req protomsg.AuthRegisterRequest

	err := proto.Unmarshal(c.Request, &req)
	if err != nil {
		return c.SendCode(core.ErrCodeBadRequest)
	}

	result, err := authsvc.Register(authsvc.AuthCredentials{
		Username:    req.Username,
		DisplayName: req.DisplayName,
		Password:    req.Password,
		DeviceID:    req.DeviceId,
		DeviceName:  req.DeviceName,
	})
	if err != nil {
		if errors.Is(err, authsvc.ErrInvalidUsername) {
			return c.SendStatus(core.ErrCodeInvalidParameters, "invalid username")
		}
		if errors.Is(err, authsvc.ErrPasswordRequired) {
			return c.SendStatus(core.ErrCodeInvalidParameters, "password is required")
		}
		if errors.Is(err, authsvc.ErrDeviceIDRequired) {
			return c.SendStatus(core.ErrCodeInvalidParameters, "device_id is required")
		}
		if errors.Is(err, db.ErrUsernameTaken) {
			return c.SendCode(core.ErrCodeUsernameTaken)
		}
		return c.SendCode(core.ErrCodeInternal)
	}

	if c.Session != nil {
		c.Session.Authenticate(result.UserID, result.DeviceID, result.DeviceName)
	}

	return c.Send(core.ErrCodeOK, "", &protomsg.AuthResponse{
		SessionToken: result.SessionToken,
	})
}

func HandleUserLoginByCredentials(c *router.ResponseContext) error {
	var req protomsg.AuthLoginByCredentialsRequest

	err := proto.Unmarshal(c.Request, &req)
	if err != nil {
		return c.SendCode(core.ErrCodeBadRequest)
	}

	result, err := authsvc.LoginByCredentials(authsvc.AuthCredentials{
		Username:   req.Username,
		Password:   req.Password,
		DeviceID:   req.DeviceId,
		DeviceName: req.DeviceName,
	})
	if err != nil {
		if errors.Is(err, authsvc.ErrDeviceIDRequired) {
			return c.SendStatus(core.ErrCodeInvalidParameters, "device_id is required")
		}
		if errors.Is(err, authsvc.ErrAccountPendingDeletion) {
			return c.SendStatus(core.ErrCodeForbidden, "account pending deletion")
		}
		if errors.Is(err, authsvc.ErrInvalidCredentials) {
			return c.SendCode(core.ErrCodeInvalidCredentials)
		}
		return c.SendCode(core.ErrCodeInternal)
	}

	if c.Session != nil {
		c.Session.Authenticate(result.UserID, result.DeviceID, result.DeviceName)
	}

	return c.Send(core.ErrCodeOK, "", &protomsg.AuthResponse{
		SessionToken: result.SessionToken,
	})
}

func HandleUserLoginByToken(c *router.ResponseContext) error {
	var req protomsg.AuthLoginByTokenRequest

	err := proto.Unmarshal(c.Request, &req)
	if err != nil {
		return c.SendCode(core.ErrCodeBadRequest)
	}

	result, err := authsvc.LoginByToken(req.SessionToken)
	if err != nil {
		if errors.Is(err, authsvc.ErrSessionTokenNeeded) {
			return c.SendStatus(core.ErrCodeInvalidParameters, "session token must be provided")
		}
		if errors.Is(err, authsvc.ErrAccountPendingDeletion) {
			return c.SendStatus(core.ErrCodeForbidden, "account pending deletion")
		}
		if errors.Is(err, authsvc.ErrInvalidToken) {
			return c.SendCode(core.ErrCodeInvalidToken)
		}
		return c.SendCode(core.ErrCodeInternal)
	}

	if c.Session != nil {
		c.Session.Authenticate(result.UserID, result.DeviceID, result.DeviceName)
	}

	return c.Send(core.ErrCodeOK, "", &protomsg.AuthResponse{
		SessionToken: result.SessionToken,
	})
}
