package main

import (
	"log/slog"
	"os"

	"github.com/lmittmann/tint"
)

func main() {
	logger := slog.New(
		tint.NewHandler(os.Stdout, &tint.Options{
			Level:      slog.LevelInfo,
			TimeFormat: "2006-01-02 15:04:05.000",
		}),
	)
	slog.SetDefault(logger)

	slog.Info("hello ppgram")
}
