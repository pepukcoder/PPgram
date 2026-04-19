package auth

import (
	"context"
	"errors"
	"log/slog"
	"strings"
	"time"

	"github.com/ppgram/cache"
	db "github.com/ppgram/database"
	"github.com/ppgram/utils"
)

const AccountDeletionDelay = 30 * 24 * time.Hour
const DefaultDeletionWorkerInterval = 1 * time.Minute

var ErrAccountDeletionInProgress = errors.New("account deletion already started")

func ScheduleAccountDeletion(username, password string) (uint64, error) {
	database, _, err := defaultDeps()
	if err != nil {
		return 0, err
	}

	user, err := verifyDeleteCredentials(database, username, password)
	if err != nil {
		return 0, err
	}

	deleteAt := time.Now().UTC().Add(AccountDeletionDelay)
	deletion, err := database.PendingAccountDeletions.Upsert(user.ID, deleteAt)
	if err != nil {
		return 0, err
	}

	return uint64(deletion.DeleteAt.Unix()), nil
}

func CancelAccountDeletion(username, password string) error {
	database, _, err := defaultDeps()
	if err != nil {
		return err
	}

	user, err := verifyDeleteCredentials(database, username, password)
	if err != nil {
		return err
	}

	cancelled, inProgress, err := database.PendingAccountDeletions.CancelIfNotStarted(user.ID)
	if err != nil {
		return err
	}
	if inProgress {
		return ErrAccountDeletionInProgress
	}
	if cancelled {
		return nil
	}
	return nil
}

func StartDeletionWorker(ctx context.Context, interval time.Duration) {
	if interval <= 0 {
		interval = DefaultDeletionWorkerInterval
	}

	go func() {
		processDueAccountDeletions()

		ticker := time.NewTicker(interval)
		defer ticker.Stop()

		for {
			select {
			case <-ctx.Done():
				return
			case <-ticker.C:
				processDueAccountDeletions()
			}
		}
	}()
}

func processDueAccountDeletions() {
	database, redis, err := defaultDeps()
	if err != nil {
		slog.Error("account deletion worker: dependencies unavailable", "error", err)
		return
	}

	claimed, err := database.PendingAccountDeletions.ClaimDue(50)
	if err != nil {
		slog.Error("account deletion worker: claim due deletions failed", "error", err)
		return
	}

	for _, item := range claimed {
		username, err := database.Users.DeleteUserByID(item.UserID)
		if err != nil {
			if errors.Is(err, db.ErrUserNotFound) {
				_ = database.PendingAccountDeletions.DeleteByUserID(item.UserID)
				continue
			}
			slog.Error("account deletion worker: delete user failed", "user_id", item.UserID, "error", err)
			continue
		}

		removeUsernameFromCache(redis, username)
	}
}

func removeUsernameFromCache(redis *cache.PPCache, username string) {
	if redis == nil {
		return
	}
	normalized := utils.NormalizeUsername(username)
	if !utils.ValidUsername(normalized) {
		return
	}
	if err := redis.RemoveUsername(normalized); err != nil {
		slog.Warn("account deletion worker: remove username from cache failed", "username", normalized, "error", err)
	}
}

func verifyDeleteCredentials(database *db.PPDatabase, username, password string) (*db.User, error) {
	normalized := utils.NormalizeUsername(username)
	if !utils.ValidUsername(normalized) || strings.TrimSpace(password) == "" {
		return nil, ErrInvalidCredentials
	}

	user, err := database.Users.GetUserByUsername(normalized)
	if err != nil {
		if errors.Is(err, db.ErrUserNotFound) {
			return nil, ErrInvalidCredentials
		}
		return nil, err
	}

	ok, err := verifyPassword(password, user.PasswordHash)
	if err != nil || !ok {
		return nil, ErrInvalidCredentials
	}

	return user, nil
}
