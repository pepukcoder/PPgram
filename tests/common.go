package main

import (
	"context"
	"crypto/tls"
	"encoding/binary"
	"io"
	"os"
	"time"

	"github.com/ppgram/server/protomsg"
	quic "github.com/quic-go/quic-go"
	"google.golang.org/protobuf/proto"
)

type frameType uint8

const (
	frameTypeData     frameType = 5
	testCloseCode               = quic.ApplicationErrorCode(0)
)

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

func getenvDefault(key, fallback string) string {
	v := os.Getenv(key)
	if v == "" {
		return fallback
	}
	return v
}

func requestOnce(addr, serverName, alpn string, timeout time.Duration, op string, body []byte) (*protomsg.ResponseEnvelope, error) {
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
	defer conn.CloseWithError(testCloseCode, "client test complete")

	stream, err := conn.OpenStreamSync(ctx)
	if err != nil {
		return nil, err
	}
	defer stream.Close()

	reqBytes, err := proto.Marshal(&protomsg.RequestEnvelope{Op: op, Request: body})
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

	resp := &protomsg.ResponseEnvelope{}
	if err := proto.Unmarshal(frame.payload, resp); err != nil {
		return nil, err
	}
	return resp, nil
}
