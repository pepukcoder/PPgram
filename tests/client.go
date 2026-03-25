package main

import (
	"context"
	"crypto/tls"
	"encoding/binary"
	"flag"
	"fmt"
	"io"
	"log"
	"time"

	protomsg "github.com/ppgram/server/protomsg"
	quic "github.com/quic-go/quic-go"
	"google.golang.org/protobuf/proto"
)

type frameType uint8

const (
	frameTypeData frameType = 5
)

type testCase struct {
	name string
	op   string
	body []byte
}

func main() {
	addr := flag.String("addr", "127.0.0.1:4433", "QUIC server address")
	serverName := flag.String("server-name", "localhost", "TLS server name")
	alpn := flag.String("alpn", "ppproto/1.0", "ALPN protocol")
	message := flag.String("message", "ping", "message for echo route")
	insecure := flag.Bool("insecure", true, "skip TLS certificate verification")
	timeout := flag.Duration("timeout", 5*time.Second, "dial timeout")
	flag.Parse()

	tests := []testCase{
		{name: "valid echo", op: "echo", body: []byte(*message)},
		{name: "empty op", op: "", body: []byte("ignored")},
		{name: "invalid op", op: "echo-1", body: []byte("ignored")},
	}

	for _, tc := range tests {
		if err := runCase(*addr, *serverName, *alpn, *insecure, *timeout, tc); err != nil {
			fmt.Printf("[%s] error: %v\n", tc.name, err)
		}
	}
}

func runCase(addr, serverName, alpn string, insecure bool, timeout time.Duration, tc testCase) error {
	ctx, cancel := context.WithTimeout(context.Background(), timeout)
	defer cancel()

	tlsConfig := &tls.Config{
		ServerName:         serverName,
		NextProtos:         []string{alpn},
		InsecureSkipVerify: insecure,
	}

	conn, err := quic.DialAddr(ctx, addr, tlsConfig, &quic.Config{})
	if err != nil {
		return fmt.Errorf("dial failed: %w", err)
	}
	defer conn.CloseWithError(0, "client done")

	stream, err := conn.OpenStreamSync(ctx)
	if err != nil {
		return fmt.Errorf("open stream failed: %w", err)
	}
	defer stream.Close()

	req := &protomsg.Request{Op: tc.op, Body: tc.body}
	buf, err := proto.Marshal(req)
	if err != nil {
		return fmt.Errorf("marshal request failed: %w", err)
	}

	if err := writeFrame(stream, frameTypeData, buf); err != nil {
		return fmt.Errorf("write frame failed: %w", err)
	}

	frame, err := readFrame(stream)
	if err != nil {
		return fmt.Errorf("read frame failed: %w", err)
	}

	resp := &protomsg.Response{}
	if err := proto.Unmarshal(frame.payload, resp); err != nil {
		return fmt.Errorf("decode response failed: %w", err)
	}

	fmt.Printf("[%s] op=%q status=%d message=%q body=%q\n", tc.name, tc.op, resp.GetStatusCode(), resp.GetMessage(), string(resp.GetBody()))
	return nil
}

type frame struct {
	typeID  frameType
	payload []byte
}

func writeFrame(stream *quic.Stream, t frameType, payload []byte) error {
	header := make([]byte, 5)
	header[0] = byte(t)
	binary.BigEndian.PutUint32(header[1:], uint32(len(payload)))

	if _, err := stream.Write(header); err != nil {
		return err
	}
	if len(payload) == 0 {
		return nil
	}

	written := 0
	for written < len(payload) {
		n, err := stream.Write(payload[written:])
		written += n
		if err != nil {
			return err
		}
		if n == 0 {
			return io.ErrShortWrite
		}
	}
	return nil
}

func readFrame(stream *quic.Stream) (*frame, error) {
	header := make([]byte, 5)
	if _, err := io.ReadFull(stream, header); err != nil {
		return nil, err
	}

	length := binary.BigEndian.Uint32(header[1:])
	payload := make([]byte, length)
	if length > 0 {
		if _, err := io.ReadFull(stream, payload); err != nil {
			return nil, err
		}
	}

	return &frame{typeID: frameType(header[0]), payload: payload}, nil
}

func init() {
	log.SetFlags(0)
}
