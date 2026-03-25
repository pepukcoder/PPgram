package main

import (
	"testing"
	"time"

	"github.com/ppgram/server/core"
)

func TestRoutes(t *testing.T) {
	addr := getenvDefault("PPGRAM_TEST_ADDR", "127.0.0.1:4433")
	serverName := getenvDefault("PPGRAM_TEST_SERVER_NAME", "localhost")
	alpn := getenvDefault("PPGRAM_TEST_ALPN", "ppproto/1.0")
	timeout := 5 * time.Second

	cases := []struct {
		name           string
		op             string
		body           []byte
		wantStatusCode core.PPStatusCode
		wantMessage    string
		wantBody       []byte
	}{
		{
			name:           "ping",
			op:             "ping",
			body:           nil,
			wantStatusCode: core.ErrCodeOK,
			wantMessage:    "pong",
			wantBody:       nil,
		},
		{
			name:           "empty op",
			op:             "",
			body:           []byte("ignored"),
			wantStatusCode: core.ErrCodeBadRequest,
			wantMessage:    "route op is required",
			wantBody:       nil,
		},
		{
			name:           "invalid op",
			op:             "echo-1",
			body:           []byte("ignored"),
			wantStatusCode: core.ErrCodeBadRequest,
			wantMessage:    "invalid route",
			wantBody:       nil,
		},
	}

	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			resp, err := requestOnce(addr, serverName, alpn, timeout, tc.op, tc.body)
			if err != nil {
				t.Skipf("integration server unavailable: %v", err)
			}

			if resp.GetStatusCode() != uint32(tc.wantStatusCode) {
				t.Fatalf("status_code mismatch: got=%d want=%d", resp.GetStatusCode(), tc.wantStatusCode)
			}
			if resp.GetMessage() != tc.wantMessage {
				t.Fatalf("message mismatch: got=%q want=%q", resp.GetMessage(), tc.wantMessage)
			}
			if string(resp.GetResponse()) != string(tc.wantBody) {
				t.Fatalf("body mismatch: got=%q want=%q", string(resp.GetResponse()), string(tc.wantBody))
			}
		})
	}
}
