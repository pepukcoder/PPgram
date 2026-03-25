package logging

import (
	"io"
	"log/slog"
	"os"
	"path/filepath"
)

var networkLogger = slog.New(slog.NewTextHandler(io.Discard, nil))

func SetNetworkLogger(logger *slog.Logger) {
	if logger == nil {
		return
	}
	networkLogger = logger
}

func NetworkLogger() *slog.Logger {
	return networkLogger
}

func NewNetworkLogger(path string) (*slog.Logger, func(), error) {
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		return nil, nil, err
	}

	file, err := os.OpenFile(path, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0o644)
	if err != nil {
		return nil, nil, err
	}

	logger := slog.New(slog.NewTextHandler(file, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	}))
	closeFn := func() {
		_ = file.Close()
	}
	return logger, closeFn, nil
}
