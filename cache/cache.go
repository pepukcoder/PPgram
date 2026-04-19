package cache

import (
	"context"
	"errors"
	"fmt"
	"os"
	"sync"

	"github.com/redis/go-redis/v9"
)

var (
	ErrDefaultRedisNotInitialized = errors.New("default redis is not initialized")
	ErrRedisAddrRequired          = errors.New("REDIS_ADDR is required")
	ErrMissingRequiredFields      = errors.New("missing required fields")
)

type PPCache struct {
	ctx    context.Context
	client *redis.Client
}

var (
	defaultCacheMu sync.RWMutex
	defaultCache   *PPCache
)

func NewPPCache(ctx context.Context, client *redis.Client) *PPCache {
	return &PPCache{ctx: ctx, client: client}
}

func (c *PPCache) Client() *redis.Client {
	if c == nil {
		return nil
	}
	return c.client
}

func (c *PPCache) Close() error {
	if c == nil || c.client == nil {
		return nil
	}
	return c.client.Close()
}

func InitDefaultCache(ctx context.Context, redisAddr string) error {
	if redisAddr == "" {
		redisAddr = os.Getenv("REDIS_ADDR")
	}
	if redisAddr == "" {
		return ErrRedisAddrRequired
	}
	if ctx == nil {
		ctx = context.Background()
	}

	client := redis.NewClient(&redis.Options{
		Addr: redisAddr,
	})
	if err := client.Ping(ctx).Err(); err != nil {
		_ = client.Close()
		return fmt.Errorf("ping redis: %w", err)
	}

	cache := NewPPCache(ctx, client)

	defaultCacheMu.Lock()
	prev := defaultCache
	defaultCache = cache
	defaultCacheMu.Unlock()

	if prev != nil {
		_ = prev.Close()
	}

	return nil
}

func DefaultCache() (*PPCache, error) {
	defaultCacheMu.RLock()
	cache := defaultCache
	defaultCacheMu.RUnlock()

	if cache == nil {
		return nil, ErrDefaultRedisNotInitialized
	}

	return cache, nil
}
