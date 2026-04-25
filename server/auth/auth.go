package auth

import (
	"crypto/rand"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/base64"
	"encoding/hex"
	"errors"
	"fmt"
	"strings"

	"github.com/ppgram/cache"
	db "github.com/ppgram/database"
	"github.com/ppgram/utils"
	"golang.org/x/crypto/argon2"
)

var (
	ErrInvalidUsername        = errors.New("invalid username")
	ErrInvalidCredentials     = errors.New("invalid credentials")
	ErrInvalidToken           = errors.New("invalid token")
	ErrAccountPendingDeletion = errors.New("account pending deletion")
	ErrPasswordRequired       = errors.New("password is required")
	ErrDeviceIDRequired       = errors.New("device_id is required")
	ErrSessionTokenNeeded     = errors.New("session token must be provided")
)

type AuthCredentials struct {
	Username    string
	DisplayName string
	Password    string
	DeviceID    string
	DeviceName  string
}

type AuthResult struct {
	UserID       string
	DeviceID     string
	DeviceName   string
	SessionToken string
}

func Register(creds AuthCredentials) (*AuthResult, error) {
	database, redis, err := defaultDeps()
	if err != nil {
		return nil, err
	}

	normalized := utils.NormalizeUsername(creds.Username)
	if !utils.ValidUsername(normalized) {
		return nil, ErrInvalidUsername
	}
	if strings.TrimSpace(creds.Password) == "" {
		return nil, ErrPasswordRequired
	}
	creds.DeviceID = strings.TrimSpace(creds.DeviceID)
	if creds.DeviceID == "" {
		return nil, ErrDeviceIDRequired
	}
	creds.DeviceName = strings.TrimSpace(creds.DeviceName)
	creds.DisplayName = strings.TrimSpace(creds.DisplayName)

	if redis != nil {
		exists, err := redis.UsernameExists(normalized)
		if err != nil {
			return nil, err
		}
		if exists {
			return nil, db.ErrUsernameTaken
		}
	}

	available, err := database.Users.IsUsernameAvailable(normalized)
	if err != nil {
		return nil, err
	}
	if !available {
		return nil, db.ErrUsernameTaken
	}

	passwordHash, err := hashPassword(creds.Password)
	if err != nil {
		return nil, err
	}

	user, err := database.Users.CreateUser(normalized, creds.DisplayName, passwordHash)
	if err != nil {
		return nil, err
	}

	if redis != nil {
		_ = redis.SetUsername(normalized, user.ID)
	}

	sessionToken, sessionTokenHash, err := generateSessionTokenPair()
	if err != nil {
		return nil, err
	}
	if err := database.UserTokens.UpsertActiveToken(user.ID, creds.DeviceID, creds.DeviceName, sessionTokenHash); err != nil {
		return nil, err
	}

	return &AuthResult{
		UserID:       user.ID,
		DeviceID:     creds.DeviceID,
		DeviceName:   creds.DeviceName,
		SessionToken: sessionToken,
	}, nil
}

func LoginByCredentials(creds AuthCredentials) (*AuthResult, error) {
	database, _, err := defaultDeps()
	if err != nil {
		return nil, err
	}

	normalized := utils.NormalizeUsername(creds.Username)
	if !utils.ValidUsername(normalized) {
		return nil, ErrInvalidCredentials
	}
	if strings.TrimSpace(creds.Password) == "" {
		return nil, ErrInvalidCredentials
	}
	creds.DeviceID = strings.TrimSpace(creds.DeviceID)
	if creds.DeviceID == "" {
		return nil, ErrDeviceIDRequired
	}
	creds.DeviceName = strings.TrimSpace(creds.DeviceName)

	user, err := database.Users.GetUserByUsername(normalized)
	if err != nil {
		if errors.Is(err, db.ErrUserNotFound) {
			return nil, ErrInvalidCredentials
		}
		return nil, err
	}

	ok, err := verifyPassword(creds.Password, user.PasswordHash)
	if err != nil || !ok {
		return nil, ErrInvalidCredentials
	}
	if err := rejectIfPendingDeletion(database, user.ID); err != nil {
		return nil, err
	}

	sessionToken, sessionTokenHash, err := generateSessionTokenPair()
	if err != nil {
		return nil, err
	}
	if err := database.UserTokens.UpsertActiveToken(user.ID, creds.DeviceID, creds.DeviceName, sessionTokenHash); err != nil {
		return nil, err
	}

	return &AuthResult{
		UserID:       user.ID,
		DeviceID:     creds.DeviceID,
		DeviceName:   creds.DeviceName,
		SessionToken: sessionToken,
	}, nil
}

func LoginByToken(sessionToken string) (*AuthResult, error) {
	database, _, err := defaultDeps()
	if err != nil {
		return nil, err
	}

	sessionToken = strings.TrimSpace(sessionToken)
	if sessionToken == "" {
		return nil, ErrSessionTokenNeeded
	}

	currentTokenHash := hashToken(sessionToken)
	storedToken, err := database.UserTokens.GetActiveByTokenHash(currentTokenHash)
	if err != nil {
		if errors.Is(err, db.ErrUserTokenNotFound) {
			return nil, ErrInvalidToken
		}
		return nil, err
	}
	if err := rejectIfPendingDeletion(database, storedToken.UserID); err != nil {
		return nil, err
	}

	newToken, newTokenHash, err := generateSessionTokenPair()
	if err != nil {
		return nil, err
	}

	if err := database.UserTokens.RotateToken(storedToken.TokenID, newTokenHash); err != nil {
		if errors.Is(err, db.ErrUserTokenNotFound) {
			return nil, ErrInvalidToken
		}
		return nil, err
	}

	return &AuthResult{
		UserID:       storedToken.UserID,
		DeviceID:     storedToken.DeviceID,
		DeviceName:   storedToken.DeviceName,
		SessionToken: newToken,
	}, nil
}

func defaultDeps() (*db.PPDatabase, *cache.PPCache, error) {
	database, err := db.DefaultDB()
	if err != nil {
		return nil, nil, err
	}

	redis, err := cache.DefaultCache()
	if err != nil {
		redis = nil
	}

	return database, redis, nil
}

func rejectIfPendingDeletion(database *db.PPDatabase, userID string) error {
	pending, err := database.PendingAccountDeletions.ExistsActiveByUserID(userID)
	if err != nil {
		return err
	}
	if pending {
		return ErrAccountPendingDeletion
	}
	return nil
}

func hashPassword(password string) (string, error) {
	const (
		memory      uint32 = 64 * 1024
		iterations  uint32 = 3
		parallelism uint8  = 2
		saltLen            = 16
		keyLen      uint32 = 32
	)

	salt := make([]byte, saltLen)
	if _, err := rand.Read(salt); err != nil {
		return "", err
	}

	hash := argon2.IDKey([]byte(password), salt, iterations, memory, parallelism, keyLen)

	encoder := base64.RawStdEncoding
	encodedSalt := encoder.EncodeToString(salt)
	encodedHash := encoder.EncodeToString(hash)
	return fmt.Sprintf("$argon2id$v=19$m=%d,t=%d,p=%d$%s$%s", memory, iterations, parallelism, encodedSalt, encodedHash), nil
}

func verifyPassword(password, encoded string) (bool, error) {
	parts := strings.Split(encoded, "$")
	if len(parts) != 6 || parts[1] != "argon2id" {
		return false, fmt.Errorf("invalid argon2id hash format")
	}
	if parts[2] != "v=19" {
		return false, fmt.Errorf("unsupported argon2 version")
	}

	var (
		memory      uint32
		iterations  uint32
		parallelism uint8
	)
	if _, err := fmt.Sscanf(parts[3], "m=%d,t=%d,p=%d", &memory, &iterations, &parallelism); err != nil {
		return false, fmt.Errorf("invalid argon2 params: %w", err)
	}

	decoder := base64.RawStdEncoding
	salt, err := decoder.DecodeString(parts[4])
	if err != nil {
		return false, fmt.Errorf("invalid argon2 salt: %w", err)
	}
	expectedHash, err := decoder.DecodeString(parts[5])
	if err != nil {
		return false, fmt.Errorf("invalid argon2 hash: %w", err)
	}

	if len(expectedHash) == 0 {
		return false, fmt.Errorf("empty argon2 hash")
	}

	computed := argon2.IDKey([]byte(password), salt, iterations, memory, parallelism, uint32(len(expectedHash)))
	return subtle.ConstantTimeCompare(computed, expectedHash) == 1, nil
}

func generateSessionTokenPair() (string, []byte, error) {
	buf := make([]byte, 32)
	if _, err := rand.Read(buf); err != nil {
		return "", nil, err
	}

	token := hex.EncodeToString(buf)
	return token, hashToken(token), nil
}

func hashToken(token string) []byte {
	sum := sha256.Sum256([]byte(token))
	return sum[:]
}
