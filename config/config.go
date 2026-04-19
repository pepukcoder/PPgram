package config

import (
	"crypto/tls"
	"fmt"
	"net"
	"net/url"
	"os"
)

type Config struct {
	QUICHost         string
	QUICPort         string
	TLSCertFile      string
	TLSKeyFile       string
	TLSALPN          string
	LogFile          string
	PostgresHost     string
	PostgresPort     string
	PostgresUser     string
	PostgresPassword string
	PostgresDB       string
	PostgresSSLMode  string
	RedisAddr        string
}

func Load() (*Config, error) {
	cfg := &Config{
		QUICHost:         getenv("QUIC_HOST"),
		QUICPort:         getenv("QUIC_PORT"),
		TLSCertFile:      getenv("TLS_CERT_FILE"),
		TLSKeyFile:       getenv("TLS_KEY_FILE"),
		TLSALPN:          getenv("TLS_ALPN"),
		LogFile:          getenv("NETWORK_LOG_FILE"),
		PostgresHost:     getenv("POSTGRES_HOST"),
		PostgresPort:     getenv("POSTGRES_PORT"),
		PostgresUser:     getenv("POSTGRES_USER"),
		PostgresPassword: getenv("POSTGRES_PASSWORD"),
		PostgresDB:       getenv("POSTGRES_DB"),
		PostgresSSLMode:  getenv("POSTGRES_SSLMODE"),
		RedisAddr:        getenv("REDIS_ADDR"),
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
	if cfg.PostgresHost == "" {
		return nil, fmt.Errorf("POSTGRES_HOST is empty")
	}
	if cfg.PostgresPort == "" {
		return nil, fmt.Errorf("POSTGRES_PORT is empty")
	}
	if cfg.PostgresUser == "" {
		return nil, fmt.Errorf("POSTGRES_USER is empty")
	}
	if cfg.PostgresDB == "" {
		return nil, fmt.Errorf("POSTGRES_DB is empty")
	}
	if cfg.PostgresSSLMode == "" {
		return nil, fmt.Errorf("POSTGRES_SSLMODE is empty")
	}
	if cfg.RedisAddr == "" {
		return nil, fmt.Errorf("REDIS_ADDR is empty")
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

func (c *Config) GetPostgresURL() string {
	values := url.Values{}
	values.Set("sslmode", c.PostgresSSLMode)

	return (&url.URL{
		Scheme:   "postgres",
		User:     url.UserPassword(c.PostgresUser, c.PostgresPassword),
		Host:     net.JoinHostPort(c.PostgresHost, c.PostgresPort),
		Path:     c.PostgresDB,
		RawQuery: values.Encode(),
	}).String()
}

func getenv(key string) string {
	v, ok := os.LookupEnv(key)
	if !ok {
		return ""
	}
	return v
}
