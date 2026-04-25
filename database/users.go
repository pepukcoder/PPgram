package db

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgconn"
	"github.com/jackc/pgx/v5/pgxpool"
)

var (
	ErrUserNotFound  = errors.New("user not found")
	ErrUsernameTaken = errors.New("username is already taken")
)

type User struct {
	ID           string
	Username     string
	DisplayName  string
	PasswordHash string
	CreatedAt    time.Time
}

type UsersRepository struct {
	ctx  context.Context
	pool *pgxpool.Pool
}

func NewUsersRepository(ctx context.Context, pool *pgxpool.Pool) (*UsersRepository, error) {
	repo := &UsersRepository{ctx: ctx, pool: pool}
	err := repo.CreateTable()
	if err != nil {
		return nil, err
	}
	return repo, nil
}

func (r *UsersRepository) CreateTable() error {
	query := `
		CREATE EXTENSION IF NOT EXISTS pgcrypto;
		CREATE TABLE IF NOT EXISTS users (
			user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
			username TEXT NOT NULL UNIQUE,
			display_name TEXT NOT NULL DEFAULT '',
			password_hash TEXT NOT NULL,
			created_at TIMESTAMPTZ NOT NULL DEFAULT now()
		);
		ALTER TABLE users ADD COLUMN IF NOT EXISTS display_name TEXT NOT NULL DEFAULT '';
	`

	_, err := r.pool.Exec(r.ctx, query)
	if err != nil {
		return fmt.Errorf("create users table: %w", err)
	}

	return nil
}

func (r *UsersRepository) CreateUser(username, displayName, passwordHash string) (*User, error) {

	username = strings.TrimSpace(username)
	if username == "" {
		return nil, fmt.Errorf("%w: username", ErrMissingRequiredFields)
	}
	displayName = strings.TrimSpace(displayName)
	if passwordHash == "" {
		return nil, fmt.Errorf("%w: password_hash", ErrMissingRequiredFields)
	}

	user := &User{}
	query := `INSERT INTO users (username, display_name, password_hash) VALUES ($1, $2, $3) RETURNING user_id, username, display_name, password_hash, created_at`
	err := r.pool.QueryRow(
		r.ctx,
		query,
		username,
		displayName,
		passwordHash,
	).Scan(&user.ID, &user.Username, &user.DisplayName, &user.PasswordHash, &user.CreatedAt)
	if err != nil {
		var pgErr *pgconn.PgError
		if errors.As(err, &pgErr) && pgErr.Code == "23505" {
			return nil, ErrUsernameTaken
		}
		return nil, fmt.Errorf("create user: %w", err)
	}

	return user, nil
}

func (r *UsersRepository) GetUserByID(userID string) (*User, error) {
	if userID == "" {
		return nil, ErrUserNotFound
	}

	query := `SELECT user_id, username, display_name, password_hash, created_at FROM users WHERE user_id = $1`

	user := &User{}
	err := r.pool.QueryRow(
		r.ctx,
		query,
		userID,
	).Scan(&user.ID, &user.Username, &user.DisplayName, &user.PasswordHash, &user.CreatedAt)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrUserNotFound
		}
		return nil, fmt.Errorf("get user by id: %w", err)
	}

	return user, nil
}

func (r *UsersRepository) DeleteUserByID(userID string) (string, error) {
	userID = strings.TrimSpace(userID)
	if userID == "" {
		return "", fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}

	var username string
	query := `DELETE FROM users WHERE user_id = $1 RETURNING username`
	err := r.pool.QueryRow(r.ctx, query, userID).Scan(&username)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return "", ErrUserNotFound
		}
		return "", fmt.Errorf("delete user by id: %w", err)
	}

	return username, nil
}

func (r *UsersRepository) GetUserByUsername(username string) (*User, error) {
	username = strings.TrimSpace(username)
	if username == "" {
		return nil, fmt.Errorf("%w: username", ErrMissingRequiredFields)
	}

	query := `SELECT user_id, username, display_name, password_hash, created_at FROM users WHERE username = $1`

	user := &User{}
	err := r.pool.QueryRow(
		r.ctx,
		query,
		username,
	).Scan(&user.ID, &user.Username, &user.DisplayName, &user.PasswordHash, &user.CreatedAt)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrUserNotFound
		}
		return nil, fmt.Errorf("get user by username: %w", err)
	}

	return user, nil
}

func (r *UsersRepository) IsUsernameAvailable(username string) (bool, error) {
	username = strings.TrimSpace(username)
	if username == "" {
		return false, fmt.Errorf("%w: username", ErrMissingRequiredFields)
	}

	query := `SELECT EXISTS (SELECT 1 FROM users WHERE username = $1)`

	var exists bool
	err := r.pool.QueryRow(
		r.ctx,
		query,
		username,
	).Scan(&exists)
	if err != nil {
		return false, fmt.Errorf("check username availability: %w", err)
	}

	return !exists, nil
}
