package db

import (
	"context"
	"fmt"
	"strings"
	"time"

	"github.com/jackc/pgx/v5/pgxpool"
)

type PendingAccountDeletion struct {
	UserID      string
	ScheduledAt time.Time
	DeleteAt    time.Time
	StartedAt   *time.Time
}

type PendingAccountDeletionsRepository struct {
	ctx  context.Context
	pool *pgxpool.Pool
}

func NewPendingAccountDeletionsRepository(ctx context.Context, pool *pgxpool.Pool) (*PendingAccountDeletionsRepository, error) {
	repo := &PendingAccountDeletionsRepository{ctx: ctx, pool: pool}
	if err := repo.CreateTable(); err != nil {
		return nil, err
	}
	return repo, nil
}

func (r *PendingAccountDeletionsRepository) CreateTable() error {
	query := `
		CREATE TABLE IF NOT EXISTS pending_account_deletions (
			user_id UUID PRIMARY KEY REFERENCES users(user_id) ON DELETE CASCADE,
			scheduled_at TIMESTAMPTZ NOT NULL DEFAULT now(),
			delete_at TIMESTAMPTZ NOT NULL,
			started_at TIMESTAMPTZ
		);
	`

	if _, err := r.pool.Exec(r.ctx, query); err != nil {
		return fmt.Errorf("create pending_account_deletions table: %w", err)
	}
	return nil
}

func (r *PendingAccountDeletionsRepository) Upsert(userID string, deleteAt time.Time) (*PendingAccountDeletion, error) {
	userID = strings.TrimSpace(userID)
	if userID == "" {
		return nil, fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}
	if deleteAt.IsZero() {
		return nil, fmt.Errorf("%w: delete_at", ErrMissingRequiredFields)
	}

	query := `
		INSERT INTO pending_account_deletions (user_id, delete_at)
		VALUES ($1, $2)
		ON CONFLICT (user_id) DO UPDATE SET
			delete_at = EXCLUDED.delete_at,
			scheduled_at = now(),
			started_at = NULL
		RETURNING user_id, scheduled_at, delete_at, started_at
	`

	deletion := &PendingAccountDeletion{}
	err := r.pool.QueryRow(r.ctx, query, userID, deleteAt).Scan(
		&deletion.UserID,
		&deletion.ScheduledAt,
		&deletion.DeleteAt,
		&deletion.StartedAt,
	)
	if err != nil {
		return nil, fmt.Errorf("upsert pending account deletion: %w", err)
	}

	return deletion, nil
}

func (r *PendingAccountDeletionsRepository) DeleteByUserID(userID string) error {
	userID = strings.TrimSpace(userID)
	if userID == "" {
		return fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}

	query := `DELETE FROM pending_account_deletions WHERE user_id = $1`
	if _, err := r.pool.Exec(r.ctx, query, userID); err != nil {
		return fmt.Errorf("delete pending account deletion: %w", err)
	}
	return nil
}

func (r *PendingAccountDeletionsRepository) ExistsActiveByUserID(userID string) (bool, error) {
	userID = strings.TrimSpace(userID)
	if userID == "" {
		return false, fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}

	query := `
		SELECT EXISTS (
			SELECT 1
			FROM pending_account_deletions
			WHERE user_id = $1
		)
	`

	var exists bool
	if err := r.pool.QueryRow(r.ctx, query, userID).Scan(&exists); err != nil {
		return false, fmt.Errorf("check pending account deletion existence: %w", err)
	}
	return exists, nil
}

func (r *PendingAccountDeletionsRepository) CancelIfNotStarted(userID string) (cancelled bool, inProgress bool, err error) {
	userID = strings.TrimSpace(userID)
	if userID == "" {
		return false, false, fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}

	query := `DELETE FROM pending_account_deletions WHERE user_id = $1 AND started_at IS NULL`
	tag, err := r.pool.Exec(r.ctx, query, userID)
	if err != nil {
		return false, false, fmt.Errorf("cancel pending account deletion: %w", err)
	}
	if tag.RowsAffected() > 0 {
		return true, false, nil
	}

	var startedExists bool
	checkQuery := `SELECT EXISTS (SELECT 1 FROM pending_account_deletions WHERE user_id = $1 AND started_at IS NOT NULL)`
	if err := r.pool.QueryRow(r.ctx, checkQuery, userID).Scan(&startedExists); err != nil {
		return false, false, fmt.Errorf("check started pending account deletion: %w", err)
	}
	return false, startedExists, nil
}

func (r *PendingAccountDeletionsRepository) ClaimDue(limit int) ([]PendingAccountDeletion, error) {
	if limit <= 0 {
		limit = 50
	}

	query := `
		WITH due AS (
			SELECT user_id
			FROM pending_account_deletions
			WHERE delete_at <= now() AND started_at IS NULL
			ORDER BY delete_at ASC
			LIMIT $1
			FOR UPDATE SKIP LOCKED
		)
		UPDATE pending_account_deletions p
		SET started_at = now()
		FROM due
		WHERE p.user_id = due.user_id
		RETURNING p.user_id, p.scheduled_at, p.delete_at, p.started_at
	`

	rows, err := r.pool.Query(r.ctx, query, limit)
	if err != nil {
		return nil, fmt.Errorf("claim due pending account deletions: %w", err)
	}
	defer rows.Close()

	out := make([]PendingAccountDeletion, 0)
	for rows.Next() {
		var item PendingAccountDeletion
		if err := rows.Scan(&item.UserID, &item.ScheduledAt, &item.DeleteAt, &item.StartedAt); err != nil {
			return nil, fmt.Errorf("scan claimed pending account deletion: %w", err)
		}
		out = append(out, item)
	}
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate claimed pending account deletions: %w", err)
	}

	return out, nil
}
