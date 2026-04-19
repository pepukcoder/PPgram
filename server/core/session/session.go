package session

import (
	"errors"
	"strings"
	"sync"

	cmap "github.com/orcaman/concurrent-map/v2"
	db "github.com/ppgram/database"
	"github.com/ppgram/server/transport"
	quic "github.com/quic-go/quic-go"
)

type UserState struct {
	mu sync.Mutex

	userID string

	devices map[string]*DeviceSession
}

var userStates cmap.ConcurrentMap[string, *UserState] = cmap.New[*UserState]()
var userStatesInitMu sync.Mutex
var ErrSessionRevoked = errors.New("session revoked")

type DeviceSession struct {
	mu sync.RWMutex

	userID     string
	deviceID   string
	deviceName string
	conn       *transport.QuicConnection
	revoked    bool
	revokedCh  chan struct{}
}

func NewDeviceSession(connection *transport.QuicConnection) *DeviceSession {
	return &DeviceSession{
		conn:      connection,
		revokedCh: make(chan struct{}),
	}
}

func (s *DeviceSession) Authenticate(userID, deviceID, deviceName string) {
	if s == nil {
		return
	}
	userID = strings.TrimSpace(userID)
	deviceID = strings.TrimSpace(deviceID)
	deviceName = strings.TrimSpace(deviceName)
	if userID == "" || deviceID == "" {
		return
	}

	s.mu.Lock()
	if s.revoked {
		s.revoked = false
		s.revokedCh = make(chan struct{})
	}
	s.userID = userID
	s.deviceID = deviceID
	s.deviceName = deviceName
	s.mu.Unlock()

	state, ok := userStates.Get(userID)
	if !ok {
		userStatesInitMu.Lock()
		state, ok = userStates.Get(userID)
		if !ok {
			defaultDB, err := db.DefaultDB()
			if err != nil {
				userStatesInitMu.Unlock()
				return
			}
			_, err = defaultDB.Users.GetUserByID(userID) // no useful fields yet
			if err != nil {
				userStatesInitMu.Unlock()
				return
			}
			state = &UserState{
				userID:  userID,
				devices: make(map[string]*DeviceSession),
			}
			userStates.Set(userID, state)
		}
		userStatesInitMu.Unlock()
	}

	state.mu.Lock()
	if state.devices == nil {
		state.devices = make(map[string]*DeviceSession)
	}
	state.devices[deviceID] = s
	state.mu.Unlock()
}

func (s *DeviceSession) IsAuthenticated() bool {
	if s == nil {
		return false
	}
	s.mu.RLock()
	defer s.mu.RUnlock()

	authenticated := s.userID != "" && s.deviceID != ""
	return authenticated
}

func (s *DeviceSession) UserID() string {
	if s == nil {
		return ""
	}
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.userID
}

func (s *DeviceSession) IsRevoked() bool {
	if s == nil {
		return true
	}
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.revoked
}

func (s *DeviceSession) Revoked() <-chan struct{} {
	if s == nil {
		closed := make(chan struct{})
		close(closed)
		return closed
	}
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.revokedCh
}

func (s *DeviceSession) Revoke() {
	if s == nil {
		return
	}

	s.mu.Lock()
	if s.revoked {
		s.mu.Unlock()
		return
	}
	s.revoked = true
	s.userID = ""
	s.deviceID = ""
	s.deviceName = ""
	if s.revokedCh != nil {
		close(s.revokedCh)
	}
	s.mu.Unlock()
}

func CloseUserSessions(userID string, code quic.ApplicationErrorCode, reason string) {
	sessions := detachUserSessions(userID)
	for _, session := range sessions {
		if session == nil {
			continue
		}

		session.Revoke()
		session.mu.RLock()
		conn := session.conn
		session.mu.RUnlock()

		if conn != nil {
			_ = conn.CloseWithError(code, reason)
		}
	}
}

func InvalidateUserSessions(userID string) {
	sessions := detachUserSessions(userID)
	for _, session := range sessions {
		if session == nil {
			continue
		}

		session.Revoke()
	}
}

func detachUserSessions(userID string) []*DeviceSession {
	userID = strings.TrimSpace(userID)
	if userID == "" {
		return nil
	}

	state, ok := userStates.Get(userID)
	if !ok {
		return nil
	}

	state.mu.Lock()
	sessions := make([]*DeviceSession, 0, len(state.devices))
	for deviceID, session := range state.devices {
		sessions = append(sessions, session)
		delete(state.devices, deviceID)
	}
	state.mu.Unlock()

	userStates.Remove(userID)
	return sessions
}
