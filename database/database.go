package db

import (
	"context"
	"errors"
	"fmt"
	"sync"

	"github.com/jackc/pgx/v5/pgxpool"
)

var (
	ErrDefaultDBNotInitialized = errors.New("default db is not initialized")
	ErrDatabaseURLRequired     = errors.New("database URL is required")
	ErrMissingRequiredFields   = errors.New("missing required fields")
)

type PPDatabase struct {
	ctx  context.Context
	pool *pgxpool.Pool

	Users                   *UsersRepository
	UserTokens              *UserTokensRepository
	PendingAccountDeletions *PendingAccountDeletionsRepository
}

var (
	defaultDBMu sync.RWMutex
	defaultDB   *PPDatabase
)

func NewPPDatabase(ctx context.Context, pool *pgxpool.Pool) (*PPDatabase, error) {
	usersRepo, err := NewUsersRepository(ctx, pool)
	if err != nil {
		return nil, err
	}
	userTokensRepo, err := NewUserTokensRepository(ctx, pool)
	if err != nil {
		return nil, err
	}
	pendingAccountDeletionsRepo, err := NewPendingAccountDeletionsRepository(ctx, pool)
	if err != nil {
		return nil, err
	}

	ppdb := &PPDatabase{
		ctx:  ctx,
		pool: pool,

		Users:                   usersRepo,
		UserTokens:              userTokensRepo,
		PendingAccountDeletions: pendingAccountDeletionsRepo,
	}
	return ppdb, nil
}

func (d *PPDatabase) Close() {
	if d == nil || d.pool == nil {
		return
	}
	d.pool.Close()
}

func InitDefaultDB(ctx context.Context, databaseURL string) error {
	if databaseURL == "" {
		return ErrDatabaseURLRequired
	}
	if ctx == nil {
		ctx = context.Background()
	}

	pool, err := pgxpool.New(ctx, databaseURL)
	if err != nil {
		return fmt.Errorf("create pgx pool: %w", err)
	}
	db, err := NewPPDatabase(ctx, pool)
	if err != nil {
		return err
	}

	defaultDBMu.Lock()
	prev := defaultDB
	defaultDB = db
	defaultDBMu.Unlock()

	if prev != nil {
		prev.Close()
	}

	return nil
}

func DefaultDB() (*PPDatabase, error) {
	defaultDBMu.RLock()
	db := defaultDB
	defaultDBMu.RUnlock()

	if db == nil {
		return nil, ErrDefaultDBNotInitialized
	}

	return db, nil
}
