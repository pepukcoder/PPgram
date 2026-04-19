package core

type PPStatusCode uint32

const (
	ErrCodeOK PPStatusCode = 0

	// Client (1XXX)
	ErrCodeBadRequest        PPStatusCode = 1001
	ErrCodeNotFound          PPStatusCode = 1002
	ErrCodeInvalidParameters PPStatusCode = 1003

	// Auth (2XXX)
	ErrCodeUnauthorized       PPStatusCode = 2001
	ErrCodeForbidden          PPStatusCode = 2002
	ErrCodeInvalidCredentials PPStatusCode = 2003
	ErrCodeInvalidToken       PPStatusCode = 2004
	ErrCodeUsernameTaken      PPStatusCode = 2005

	// Internal (5XXX)
	ErrCodeInternal PPStatusCode = 5001
)
