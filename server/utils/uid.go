package utils

import (
	"crypto/rand"
	"encoding/hex"
	"fmt"
	"sync/atomic"
	"time"
)

var uidFallbackSeq uint64

func NewUID(prefix string) string {
	buf := make([]byte, 12)
	if _, err := rand.Read(buf); err == nil {
		return fmt.Sprintf("%s_%s", prefix, hex.EncodeToString(buf))
	}

	seq := atomic.AddUint64(&uidFallbackSeq, 1)
	return fmt.Sprintf("%s_%x%x", prefix, time.Now().UnixNano(), seq)
}
