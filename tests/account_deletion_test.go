package main

import (
	"fmt"
	"testing"
	"time"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
)

func TestAccountDeletionFlow(t *testing.T) {
	addr := getenvDefault("PPGRAM_TEST_ADDR", "127.0.0.1:4433")
	serverName := getenvDefault("PPGRAM_TEST_SERVER_NAME", "localhost")
	alpn := getenvDefault("PPGRAM_TEST_ALPN", "ppproto/1.0")
	timeout := 5 * time.Second

	username := fmt.Sprintf("accdel_%d", time.Now().UnixNano())
	password := "ppgram_test_password"

	registerClient := connectTestClient(t, addr, serverName, alpn, timeout)
	defer registerClient.close()

	t.Run("register", func(t *testing.T) {
		registerResp, err := registerClient.request(t, "auth.register", &protomsg.AuthRegisterRequest{
			Username:    username,
			DisplayName: "Acc Del",
			Password:    password,
			DeviceId:    "accdel_reg",
			DeviceName:  "accdel-register",
		})
		if err != nil {
			t.Fatalf("register failed: %v", err)
		}
		if registerResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("register status: got=%d msg=%q", registerResp.GetStatusCode(), registerResp.GetMessage())
		}
	})

	loginClient := connectTestClient(t, addr, serverName, alpn, timeout)
	defer loginClient.close()
	t.Run("login", func(t *testing.T) {
		loginResp, err := loginClient.request(t, "auth.login.credentials", &protomsg.AuthLoginByCredentialsRequest{
			Username:   username,
			Password:   password,
			DeviceId:   "accdel_dev_1",
			DeviceName: "accdel-device-1",
		})
		if err != nil {
			t.Fatalf("login failed: %v", err)
		}
		if loginResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("login status: got=%d msg=%q", loginResp.GetStatusCode(), loginResp.GetMessage())
		}
	})

	t.Run("delete_account", func(t *testing.T) {
		delResp, err := loginClient.request(t, "account.delete", &protomsg.AccountDeleteRequest{
			Username: username,
			Password: password,
		})
		if err != nil {
			t.Fatalf("account.delete failed: %v", err)
		}
		if delResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("account.delete status: got=%d msg=%q", delResp.GetStatusCode(), delResp.GetMessage())
		}
	})

	t.Run("session_invalidated_after_delete", func(t *testing.T) {
		resp, err := loginClient.request(t, "account.delete", &protomsg.AccountDeleteRequest{
			Username: username,
			Password: password,
		})
		if err != nil {
			t.Fatalf("expected unauthorized response on invalidated session: %v", err)
		}
		if resp.GetStatusCode() != uint32(core.ErrCodeUnauthorized) {
			t.Fatalf("expected unauthorized on invalidated session: got=%d msg=%q", resp.GetStatusCode(), resp.GetMessage())
		}
	})

	postDeleteConn := connectTestClient(t, addr, serverName, alpn, timeout)
	defer postDeleteConn.close()
	t.Run("login_rejected_while_pending", func(t *testing.T) {
		postDeleteLoginResp, err := postDeleteConn.request(t, "auth.login.credentials", &protomsg.AuthLoginByCredentialsRequest{
			Username:   username,
			Password:   password,
			DeviceId:   "accdel_after_delete",
			DeviceName: "accdel-after-delete",
		})
		if err != nil {
			t.Fatalf("post-delete login request failed: %v", err)
		}
		if postDeleteLoginResp.GetStatusCode() != uint32(core.ErrCodeForbidden) {
			t.Fatalf("expected forbidden while account pending deletion: got=%d msg=%q", postDeleteLoginResp.GetStatusCode(), postDeleteLoginResp.GetMessage())
		}
		if postDeleteLoginResp.GetMessage() != "account pending deletion" {
			t.Fatalf("expected pending deletion message: got=%q", postDeleteLoginResp.GetMessage())
		}
	})

	cancelConn := connectTestClient(t, addr, serverName, alpn, timeout)
	defer cancelConn.close()
	t.Run("cancel_pending_deletion", func(t *testing.T) {
		cancelResp, err := cancelConn.request(t, "account.delete.cancel", &protomsg.AccountDeleteCancelRequest{
			Username: username,
			Password: password,
		})
		if err != nil {
			t.Fatalf("account.delete.cancel failed: %v", err)
		}
		if cancelResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("account.delete.cancel status: got=%d msg=%q", cancelResp.GetStatusCode(), cancelResp.GetMessage())
		}
	})

	postCancelConn := connectTestClient(t, addr, serverName, alpn, timeout)
	defer postCancelConn.close()
	t.Run("login_allowed_after_cancel", func(t *testing.T) {
		postCancelResp, err := postCancelConn.request(t, "auth.login.credentials", &protomsg.AuthLoginByCredentialsRequest{
			Username:   username,
			Password:   password,
			DeviceId:   "accdel_after_cancel",
			DeviceName: "accdel-after-cancel",
		})
		if err != nil {
			t.Fatalf("post-cancel login failed: %v", err)
		}
		if postCancelResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("post-cancel login status: got=%d msg=%q", postCancelResp.GetStatusCode(), postCancelResp.GetMessage())
		}
	})
}
