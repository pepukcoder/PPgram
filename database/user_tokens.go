package db

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

var (
	ErrUserTokenNotFound = errors.New("user token not found")
)

type UserToken struct {
	TokenID    int64
	UserID     string
	DeviceID   string
	DeviceName string
	IssuedAt   time.Time
	LastUsedAt time.Time
	ExpiresAt  *time.Time
	RevokedAt  *time.Time
}

type UserTokensRepository struct {
	ctx  context.Context
	pool *pgxpool.Pool
}

func NewUserTokensRepository(ctx context.Context, pool *pgxpool.Pool) (*UserTokensRepository, error) {
	repo := &UserTokensRepository{ctx: ctx, pool: pool}
	err := repo.CreateTable()
	if err != nil {
		return nil, err
	}
	return repo, nil
}

func (r *UserTokensRepository) CreateTable() error {
	query := `
		CREATE TABLE IF NOT EXISTS user_tokens (
			token_id BIGSERIAL PRIMARY KEY,
			user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
			token_hash BYTEA NOT NULL UNIQUE,
			device_id TEXT NOT NULL UNIQUE,
			device_name TEXT,
			issued_at TIMESTAMPTZ NOT NULL DEFAULT now(),
			last_used_at TIMESTAMPTZ NOT NULL DEFAULT now(),
			expires_at TIMESTAMPTZ,
			revoked_at TIMESTAMPTZ
		);
	`

	if _, err := r.pool.Exec(r.ctx, query); err != nil {
		return fmt.Errorf("create user_tokens table: %w", err)
	}

	return nil
}

func (r *UserTokensRepository) UpsertActiveToken(userID, deviceID, deviceName string, tokenHash []byte) error {
	userID = strings.TrimSpace(userID)
	deviceID = strings.TrimSpace(deviceID)
	deviceName = strings.TrimSpace(deviceName)
	if userID == "" {
		return fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}
	if deviceID == "" {
		return fmt.Errorf("%w: device_id", ErrMissingRequiredFields)
	}
	if len(tokenHash) == 0 {
		return fmt.Errorf("%w: token_hash", ErrMissingRequiredFields)
	}

	query := `
		INSERT INTO user_tokens (user_id, token_hash, device_id, device_name)
		VALUES ($1, $2, $3, $4)
		ON CONFLICT (device_id) DO UPDATE SET
			user_id = EXCLUDED.user_id,
			token_hash = EXCLUDED.token_hash,
			device_name = EXCLUDED.device_name,
			last_used_at = now(),
			issued_at = now(),
			revoked_at = NULL
	`
	if _, err := r.pool.Exec(r.ctx, query, userID, tokenHash, deviceID, deviceName); err != nil {
		return fmt.Errorf("upsert active token: %w", err)
	}
	return nil
}

func (r *UserTokensRepository) GetActiveByTokenHash(tokenHash []byte) (*UserToken, error) {
	if len(tokenHash) == 0 {
		return nil, fmt.Errorf("%w: token_hash", ErrMissingRequiredFields)
	}

	query := `
		SELECT token_id, user_id, device_id, COALESCE(device_name, ''), issued_at, last_used_at, expires_at, revoked_at
		FROM user_tokens
		WHERE token_hash = $1
			AND revoked_at IS NULL
			AND (expires_at IS NULL OR expires_at > now())
	`

	token := &UserToken{}
	err := r.pool.QueryRow(r.ctx, query, tokenHash).Scan(
		&token.TokenID,
		&token.UserID,
		&token.DeviceID,
		&token.DeviceName,
		&token.IssuedAt,
		&token.LastUsedAt,
		&token.ExpiresAt,
		&token.RevokedAt,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrUserTokenNotFound
		}
		return nil, fmt.Errorf("get active token by hash: %w", err)
	}

	return token, nil
}

func (r *UserTokensRepository) RotateToken(tokenID int64, newTokenHash []byte) error {
	if tokenID == 0 {
		return fmt.Errorf("%w: token_id", ErrMissingRequiredFields)
	}
	if len(newTokenHash) == 0 {
		return fmt.Errorf("%w: token_hash", ErrMissingRequiredFields)
	}

	query := `
		UPDATE user_tokens
		SET token_hash = $2, last_used_at = now()
		WHERE token_id = $1
	`
	tag, err := r.pool.Exec(r.ctx, query, tokenID, newTokenHash)
	if err != nil {
		return fmt.Errorf("rotate token: %w", err)
	}
	if tag.RowsAffected() == 0 {
		return ErrUserTokenNotFound
	}

	return nil
}
