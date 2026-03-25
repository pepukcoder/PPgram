package config

import (
	"crypto/tls"
	"fmt"
	"net"
	"os"
)

const (
	defaultQUICHost    = "0.0.0.0"
	defaultQUICPort    = "4433"
	defaultTLSCertFile = "certs/server.cert.pem"
	defaultTLSKeyFile  = "secrets/server.key.pem"
	defaultTLSALPN     = "ppproto/1.0"
	defaultLogFile     = "logs/network.log"
)

type Config struct {
	QUICHost    string
	QUICPort    string
	TLSCertFile string
	TLSKeyFile  string
	TLSALPN     string
	LogFile     string
}

func Load() (*Config, error) {
	cfg := &Config{
		QUICHost:    getenv("QUIC_HOST", defaultQUICHost),
		QUICPort:    getenv("QUIC_PORT", defaultQUICPort),
		TLSCertFile: getenv("TLS_CERT_FILE", defaultTLSCertFile),
		TLSKeyFile:  getenv("TLS_KEY_FILE", defaultTLSKeyFile),
		TLSALPN:     getenv("TLS_ALPN", defaultTLSALPN),
		LogFile:     getenv("NETWORK_LOG_FILE", defaultLogFile),
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
	if cfg.QUICHost == "" {
		return nil, fmt.Errorf("QUIC_HOST is empty")
	}
	if cfg.QUICPort == "" {
		return nil, fmt.Errorf("QUIC_PORT is empty")
	}
	if cfg.LogFile == "" {
		return nil, fmt.Errorf("NETWORK_LOG_FILE is empty")
	}

	return cfg, nil
}

func (c *Config) QUICAddr() string {
	return net.JoinHostPort(c.QUICHost, c.QUICPort)
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
