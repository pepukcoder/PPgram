package transport

import (
	"context"
	"net"

	"github.com/ppgram/server/utils"
	quic "github.com/quic-go/quic-go"
)

type QuicConnection struct {
	conn *quic.Conn
	id   string
}

func (c *QuicConnection) AcceptStream(ctx context.Context) (*QuicStream, error) {
	stream, err := c.conn.AcceptStream(ctx)
	if err != nil {
		return nil, err
	}

	streamID := utils.NewUID("s")
	return &QuicStream{stream: stream, connID: c.id, id: streamID}, nil
}

func (c *QuicConnection) OpenStream(ctx context.Context) (*QuicStream, error) {
	stream, err := c.conn.OpenStreamSync(ctx)
	if err != nil {
		return nil, err
	}

	streamID := utils.NewUID("s")
	return &QuicStream{stream: stream, connID: c.id, id: streamID}, nil
}

func (c *QuicConnection) ID() string {
	return c.id
}

func (c *QuicConnection) SetID(id string) {
	c.id = id
}

func (c *QuicConnection) CloseWithError(code quic.ApplicationErrorCode, reason string) error {
	return c.conn.CloseWithError(code, reason)
}

func (c *QuicConnection) LocalAddr() net.Addr {
	return c.conn.LocalAddr()
}

func (c *QuicConnection) RemoteAddr() net.Addr {
	return c.conn.RemoteAddr()
}
