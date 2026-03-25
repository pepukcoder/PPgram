package router

import "errors"

var (
	ErrResponseBodyRequired = errors.New("response body is required")
	ErrUpdateBodyRequired   = errors.New("update body is required")
)
