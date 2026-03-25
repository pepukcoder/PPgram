package transport

import (
	"context"
	"net"

	quic "github.com/quic-go/quic-go"
)

type QuicConnection struct {
	conn *quic.Conn
}

func (c *QuicConnection) AcceptStream(ctx context.Context) (*QuicStream, error) {
	stream, err := c.conn.AcceptStream(ctx)
	if err != nil {
		return nil, err
	}

	return &QuicStream{stream: stream}, nil
}

func (c *QuicConnection) OpenStream(ctx context.Context) (*QuicStream, error) {
	stream, err := c.conn.OpenStreamSync(ctx)
	if err != nil {
		return nil, err
	}

	return &QuicStream{stream: stream}, nil
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
