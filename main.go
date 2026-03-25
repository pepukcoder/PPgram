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
	"github.com/ppgram/server/logging"
	"github.com/ppgram/server/router"
	"github.com/ppgram/server/transport"
	quic "github.com/quic-go/quic-go"
)

func main() {
	consoleLogger := slog.New(
		tint.NewHandler(os.Stdout, &tint.Options{
			Level:      slog.LevelInfo,
			TimeFormat: "2006-01-02 15:04:05.000",
		}),
	)
	slog.SetDefault(consoleLogger)

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
		"log_file", cfg.LogFile,
	)

	networkLogger, closeNetworkLogger, err := logging.NewNetworkLogger(cfg.LogFile)
	if err != nil {
		slog.Error("init network logger", "error", err)
		os.Exit(1)
	}
	defer closeNetworkLogger()
	logging.SetNetworkLogger(networkLogger)

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

	app.Response("ping", handlers.PingHandler)

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
				connID := conn.ID()
				if isExpectedConnectionClose(err) {
					logging.NetworkLogger().Info("connection closed", "conn_id", connID, "remote_addr", conn.RemoteAddr().String(), "message", connectionCloseMessage(err))
					return
				}
				slog.Error("serve quic connection", "error", err)
				logging.NetworkLogger().Error("connection failed", "conn_id", connID, "remote_addr", conn.RemoteAddr().String(), "error", err)
			}
		}(conn)
	}
}

func isExpectedConnectionClose(err error) bool {
	var appErr *quic.ApplicationError
	if errors.As(err, &appErr) && appErr.ErrorCode == 0 {
		return true
	}
	return false
}

func connectionCloseMessage(err error) string {
	if appErr, ok := errors.AsType[*quic.ApplicationError](err); ok {
		return appErr.ErrorMessage
	}
	return err.Error()
}
