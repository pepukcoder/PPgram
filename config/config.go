package config

import (
	"crypto/tls"
	"fmt"
	"os"
)

const (
	defaultQUICAddr    = "0.0.0.0:4433"
	defaultTLSCertFile = "certs/server.cert.pem"
	defaultTLSKeyFile  = "secrets/server.key.pem"
	defaultTLSALPN     = "ppproto/1.0"
)

type Config struct {
	QUICAddr    string
	TLSCertFile string
	TLSKeyFile  string
	TLSALPN     string
}

func Load() (*Config, error) {
	cfg := &Config{
		QUICAddr:    getenv("QUIC_ADDR", defaultQUICAddr),
		TLSCertFile: getenv("TLS_CERT_FILE", defaultTLSCertFile),
		TLSKeyFile:  getenv("TLS_KEY_FILE", defaultTLSKeyFile),
		TLSALPN:     getenv("TLS_ALPN", defaultTLSALPN),
	}

	if cfg.TLSCertFile == "" {
		return nil, fmt.Errorf("TLS_CERT_FILE is empty")
	}
	if cfg.TLSKeyFile == "" {
		return nil, fmt.Errorf("TLS_KEY_FILE is empty")
	}
	if cfg.TLSALPN == "" {
		return nil, fmt.Errorf("TLS_ALPN is empty")
	}
	if cfg.QUICAddr == "" {
		return nil, fmt.Errorf("QUIC_ADDR is empty")
	}

	return cfg, nil
}

func (c *Config) TLSConfig() (*tls.Config, error) {
	cert, err := tls.LoadX509KeyPair(c.TLSCertFile, c.TLSKeyFile)
	if err != nil {
		return nil, err
	}

	return &tls.Config{
		Certificates: []tls.Certificate{cert},
		NextProtos:   []string{c.TLSALPN},
	}, nil
}

func getenv(key, fallback string) string {
	v, ok := os.LookupEnv(key)
	if !ok {
		return fallback
	}
	return v
}
