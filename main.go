package main

import (
	"context"
	"errors"
	"log/slog"
	"os"
	"os/signal"
	"syscall"

	"github.com/lmittmann/tint"
	"github.com/ppgram/config"
	"github.com/ppgram/server/handlers"
	"github.com/ppgram/server/router"
	"github.com/ppgram/server/transport"
	quic "github.com/quic-go/quic-go"
)

func main() {
	logger := slog.New(
		tint.NewHandler(os.Stdout, &tint.Options{
			Level:      slog.LevelInfo,
			TimeFormat: "2006-01-02 15:04:05.000",
		}),
	)
	slog.SetDefault(logger)

	cfg, err := config.Load()
	if err != nil {
		slog.Error("load config", "error", err)
		os.Exit(1)
	}

	slog.Info("config loaded",
		"quic_addr", cfg.QUICAddr,
		"tls_cert_file", cfg.TLSCertFile,
		"tls_key_file", cfg.TLSKeyFile,
		"tls_alpn", cfg.TLSALPN,
	)

	tlsConfig, err := cfg.TLSConfig()
	if err != nil {
		slog.Error("build tls config", "error", err)
		os.Exit(1)
	}

	listener, err := transport.Start(cfg.QUICAddr, tlsConfig, &quic.Config{})
	if err != nil {
		slog.Error("start quic listener", "error", err)
		os.Exit(1)
	}
	defer listener.Close()

	app := router.New()
	app.Response("echo", handlers.Echo)

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	slog.Info("quic listener started", "addr", listener.Addr().String())

	for {
		conn, err := listener.Accept(ctx)
		if err != nil {
			if errors.Is(ctx.Err(), context.Canceled) {
				slog.Info("shutdown complete")
				return
			}
			slog.Error("accept quic connection", "error", err)
			continue
		}

		go func(conn *transport.QuicConnection) {
			if err := app.ServeConnection(ctx, conn); err != nil && !errors.Is(ctx.Err(), context.Canceled) {
				slog.Error("serve quic connection", "error", err)
			}
		}(conn)
	}
}
