package core

type PPError uint32

const (
	ErrCodeOK PPError = 0

	// User (01XXX)
	ErrCodeBadRequest PPError = 11001
	ErrCodeNotFound   PPError = 11002

	// Auth (02XXX)
	ErrCodeUnauthorized PPError = 12001
	ErrCodeForbidden    PPError = 12002

	// Internal (05XXX)
	ErrCodeInternal PPError = 15001
)
