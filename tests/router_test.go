package main

import (
	"context"
	"crypto/tls"
	"os"
	"testing"
	"time"

	protomsg "github.com/ppgram/server/protomsg"
	quic "github.com/quic-go/quic-go"
	"google.golang.org/protobuf/proto"
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
		wantStatusCode uint32
		wantMessage    string
		wantBody       []byte
	}{
		{
			name:           "echo",
			op:             "echo",
			body:           []byte("ping"),
			wantStatusCode: 0,
			wantMessage:    "ok",
			wantBody:       []byte("ping"),
		},
		{
			name:           "empty op",
			op:             "",
			body:           []byte("ignored"),
			wantStatusCode: 11001,
			wantMessage:    "route op is required",
			wantBody:       nil,
		},
		{
			name:           "invalid op",
			op:             "echo-1",
			body:           []byte("ignored"),
			wantStatusCode: 11001,
			wantMessage:    "invalid route",
			wantBody:       nil,
		},
	}

	for _, tc := range cases {
		tc := tc
		t.Run(tc.name, func(t *testing.T) {
			resp, err := requestOnce(addr, serverName, alpn, timeout, tc.op, tc.body)
			if err != nil {
				t.Skipf("integration server unavailable: %v", err)
			}

			if resp.GetStatusCode() != tc.wantStatusCode {
				t.Fatalf("status_code mismatch: got=%d want=%d", resp.GetStatusCode(), tc.wantStatusCode)
			}
			if resp.GetMessage() != tc.wantMessage {
				t.Fatalf("message mismatch: got=%q want=%q", resp.GetMessage(), tc.wantMessage)
			}
			if string(resp.GetBody()) != string(tc.wantBody) {
				t.Fatalf("body mismatch: got=%q want=%q", string(resp.GetBody()), string(tc.wantBody))
			}
		})
	}
}

func requestOnce(addr, serverName, alpn string, timeout time.Duration, op string, body []byte) (*protomsg.Response, error) {
	ctx, cancel := context.WithTimeout(context.Background(), timeout)
	defer cancel()

	tlsConfig := &tls.Config{
		ServerName:         serverName,
		NextProtos:         []string{alpn},
		InsecureSkipVerify: true,
	}

	conn, err := quic.DialAddr(ctx, addr, tlsConfig, &quic.Config{})
	if err != nil {
		return nil, err
	}
	defer conn.CloseWithError(0, "test done")

	stream, err := conn.OpenStreamSync(ctx)
	if err != nil {
		return nil, err
	}
	defer stream.Close()

	reqBytes, err := proto.Marshal(&protomsg.Request{Op: op, Body: body})
	if err != nil {
		return nil, err
	}
	if err := writeFrame(stream, frameTypeData, reqBytes); err != nil {
		return nil, err
	}

	frame, err := readFrame(stream)
	if err != nil {
		return nil, err
	}

	resp := &protomsg.Response{}
	if err := proto.Unmarshal(frame.payload, resp); err != nil {
		return nil, err
	}
	return resp, nil
}

func getenvDefault(key, fallback string) string {
	v := os.Getenv(key)
	if v == "" {
		return fallback
	}
	return v
}
