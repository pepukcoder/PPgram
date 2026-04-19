package main

import (
	"fmt"
	"testing"
	"time"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/protomsg"
)

func TestAuthFlows(t *testing.T) {
	addr := getenvDefault("PPGRAM_TEST_ADDR", "127.0.0.1:4433")
	serverName := getenvDefault("PPGRAM_TEST_SERVER_NAME", "localhost")
	alpn := getenvDefault("PPGRAM_TEST_ALPN", "ppproto/1.0")
	timeout := 5 * time.Second

	username := fmt.Sprintf("authflow_%d", time.Now().UnixNano())
	password := "ppgram_test_password"

	client := connectTestClient(t, addr, serverName, alpn, timeout)
	defer client.close()

	t.Run("register", func(t *testing.T) {
		regResp, err := client.request(t, "auth.register", &protomsg.AuthRegisterRequest{
			Username:   username,
			Password:   password,
			DeviceId:   "auth_dev_1",
			DeviceName: "auth-device-1",
		})
		if err != nil {
			t.Fatalf("register failed: %v", err)
		}
		if regResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("register status: got=%d msg=%q", regResp.GetStatusCode(), regResp.GetMessage())
		}
	})

	t.Run("username_taken_on_second_register", func(t *testing.T) {
		resp, err := client.request(t, "auth.register", &protomsg.AuthRegisterRequest{
			Username:   username,
			Password:   password,
			DeviceId:   "auth_dev_2",
			DeviceName: "auth-device-2",
		})
		if err != nil {
			t.Fatalf("second register failed: %v", err)
		}
		if resp.GetStatusCode() != uint32(core.ErrCodeUsernameTaken) {
			t.Fatalf("second register expected username taken: got=%d msg=%q", resp.GetStatusCode(), resp.GetMessage())
		}
	})

	var firstToken string
	loginResp, err := client.request(t, "auth.login.credentials", &protomsg.AuthLoginByCredentialsRequest{
		Username:   username,
		Password:   password,
		DeviceId:   "auth_dev_1",
		DeviceName: "auth-device-1",
	})
	t.Run("login_by_credentials", func(t *testing.T) {
		if err != nil {
			t.Fatalf("login credentials failed: %v", err)
		}
		if loginResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("login credentials status: got=%d msg=%q", loginResp.GetStatusCode(), loginResp.GetMessage())
		}
		firstToken = parseAuthResponse(t, loginResp)
	})

	var secondToken string
	rotateResp, err := client.request(t, "auth.login.token", &protomsg.AuthLoginByTokenRequest{SessionToken: firstToken})
	t.Run("token_rotate", func(t *testing.T) {
		if err != nil {
			t.Fatalf("first token login failed: %v", err)
		}
		if rotateResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("first token login status: got=%d msg=%q", rotateResp.GetStatusCode(), rotateResp.GetMessage())
		}
		secondToken = parseAuthResponse(t, rotateResp)
	})

	reuseOldResp, err := client.request(t, "auth.login.token", &protomsg.AuthLoginByTokenRequest{SessionToken: firstToken})
	t.Run("reuse_old_token_rejected", func(t *testing.T) {
		if err != nil {
			t.Fatalf("reuse old token request failed: %v", err)
		}
		if reuseOldResp.GetStatusCode() != uint32(core.ErrCodeInvalidToken) {
			t.Fatalf("reuse old token expected invalid token: got=%d msg=%q", reuseOldResp.GetStatusCode(), reuseOldResp.GetMessage())
		}
	})

	newTokenResp, err := client.request(t, "auth.login.token", &protomsg.AuthLoginByTokenRequest{SessionToken: secondToken})
	t.Run("new_rotated_token_accepted", func(t *testing.T) {
		if err != nil {
			t.Fatalf("second token login failed: %v", err)
		}
		if newTokenResp.GetStatusCode() != uint32(core.ErrCodeOK) {
			t.Fatalf("second token login status: got=%d msg=%q", newTokenResp.GetStatusCode(), newTokenResp.GetMessage())
		}
	})
}
