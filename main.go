package main

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"os"
	"os/signal"
	"strings"
	"syscall"

	"github.com/lmittmann/tint"
	"github.com/ppgram/cache"
	"github.com/ppgram/config"
	db "github.com/ppgram/database"
	authsvc "github.com/ppgram/server/auth"
	"github.com/ppgram/server/handlers"
	"github.com/ppgram/server/logging"
	"github.com/ppgram/server/middleware"
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

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	cfg, err := config.Load()
	if err != nil {
		slog.Error("load config", "error", err)
		os.Exit(1)
	}
	tlsConfig, err := cfg.TLSConfig()
	if err != nil {
		slog.Error("build tls config", "error", err)
		os.Exit(1)
	}

	networkLogger, closeNetworkLogger, err := logging.NewNetworkLogger(cfg.LogFile)
	if err != nil {
		slog.Error("init network logger", "error", err)
		os.Exit(1)
	}
	defer closeNetworkLogger()
	logging.SetNetworkLogger(networkLogger)

	// init infrastructure
	if err := db.InitDefaultDB(ctx, cfg.GetPostgresURL()); err != nil {
		slog.Error("init database", "error", err)
		os.Exit(1)
	}
	defer func() {
		defaultDB, err := db.DefaultDB()
		if err == nil {
			defaultDB.Close()
		}
	}()

	if err := cache.InitDefaultCache(ctx, cfg.RedisAddr); err != nil {
		slog.Error("init redis", "error", err)
		os.Exit(1)
	}
	defer func() {
		defaultCache, err := cache.DefaultCache()
		if err == nil {
			_ = defaultCache.Close()
		}
	}()

	// setup routes
	quicRouter := router.New()

	quicRouter.Response("ping", handlers.PingHandler)

	quicRouter.Response("auth.register", handlers.HandleUserRegister)
	quicRouter.Response("auth.login.credentials", handlers.HandleUserLoginByCredentials)
	quicRouter.Response("auth.login.token", handlers.HandleUserLoginByToken)

	quicRouter.Response("account.delete", handlers.HandleAccountDelete, middleware.AuthMiddleware)
	quicRouter.Response("account.delete.cancel", handlers.HandleAccountDeleteCancel)

	printStartupPanel(cfg, quicRouter.RouteCount())

	// start workers
	authsvc.StartDeletionWorker(ctx, authsvc.DefaultDeletionWorkerInterval)

	// start serving
	listener, err := transport.Start(cfg.QUICAddr(), tlsConfig, &quic.Config{})
	if err != nil {
		slog.Error("start quic listener", "error", err)
		os.Exit(1)
	}
	defer listener.Close()
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
			if err := quicRouter.ServeConnection(ctx, conn); err != nil && !errors.Is(ctx.Err(), context.Canceled) {
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

func printStartupPanel(cfg *config.Config, routeCount int) {
	rows := []string{
		"PPgram QUIC Server",
		"Host    : " + cfg.QUICHost,
		"Port    : " + cfg.QUICPort,
		"ALPN    : " + cfg.TLSALPN,
		fmt.Sprintf("Handlers: %d", routeCount),
	}

	width := 0
	for _, row := range rows {
		if len(row) > width {
			width = len(row)
		}
	}

	border := "┌" + strings.Repeat("─", width+2) + "┐"
	fmt.Println(border)
	for _, row := range rows {
		padding := strings.Repeat(" ", width-len(row))
		fmt.Printf("│ %s%s │\n", row, padding)
	}
	fmt.Println("└" + strings.Repeat("─", width+2) + "┘")
}
