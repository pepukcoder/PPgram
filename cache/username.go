package cache

import (
	"fmt"
	"strings"
)

func (c *PPCache) UsernameExists(username string) (bool, error) {
	if c == nil || c.client == nil {
		return false, ErrDefaultRedisNotInitialized
	}

	username = strings.TrimSpace(username)
	if username == "" {
		return false, fmt.Errorf("%w: username", ErrMissingRequiredFields)
	}

	key := usernameKey(username)

	exists, err := c.client.Exists(c.ctx, key).Result()
	if err != nil {
		return false, fmt.Errorf("check username existence: %w", err)
	}

	return exists > 0, nil
}

func (c *PPCache) SetUsername(username, userID string) error {
	if c == nil || c.client == nil {
		return ErrDefaultRedisNotInitialized
	}

	username = strings.TrimSpace(username)
	if username == "" {
		return fmt.Errorf("%w: username", ErrMissingRequiredFields)
	}

	userID = strings.TrimSpace(userID)
	if userID == "" {
		return fmt.Errorf("%w: user_id", ErrMissingRequiredFields)
	}

	key := usernameKey(username)
	if err := c.client.Set(c.ctx, key, userID, 0).Err(); err != nil {
		return fmt.Errorf("set username key: %w", err)
	}

	return nil
}

func (c *PPCache) RemoveUsername(username string) error {
	if c == nil || c.client == nil {
		return ErrDefaultRedisNotInitialized
	}

	username = strings.TrimSpace(username)
	if username == "" {
		return fmt.Errorf("%w: username", ErrMissingRequiredFields)
	}

	key := usernameKey(username)
	if err := c.client.Del(c.ctx, key).Err(); err != nil {
		return fmt.Errorf("remove username key: %w", err)
	}

	return nil
}

func usernameKey(username string) string {
	return fmt.Sprintf("username:%s", username)
}
