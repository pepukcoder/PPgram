package transport

import (
	"context"
	"crypto/tls"
	"net"

	quic "github.com/quic-go/quic-go"
)

type QuicListener struct {
	listener *quic.Listener
}

func Start(addr string, tlsConfig *tls.Config, config *quic.Config) (*QuicListener, error) {
	l, err := quic.ListenAddr(addr, tlsConfig, config)
	if err != nil {
		return nil, err
	}

	return &QuicListener{listener: l}, nil
}

func (l *QuicListener) Accept(ctx context.Context) (*QuicConnection, error) {
	conn, err := l.listener.Accept(ctx)
	if err != nil {
		return nil, err
	}

	return &QuicConnection{conn: conn}, nil
}

func (l *QuicListener) Close() error {
	return l.listener.Close()
}

func (l *QuicListener) Addr() net.Addr {
	return l.listener.Addr()
}
