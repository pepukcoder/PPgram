package utils

import (
	"strings"
)

func NormalizeUsername(username string) string {
	return strings.ToLower(strings.TrimSpace(username))
}

func ValidUsername(username string) bool {
	if username == "" {
		return false
	}

	if username[0] == '_' || username[len(username)-1] == '_' {
		return false
	}

	for i := 0; i < len(username); i++ {
		ch := username[i]
		isLowerASCII := ch >= 'a' && ch <= 'z'
		isDigit := ch >= '0' && ch <= '9'
		if isLowerASCII || isDigit || ch == '_' {
			continue
		}
		return false
	}

	return true
}
