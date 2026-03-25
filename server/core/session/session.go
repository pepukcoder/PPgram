package session

import (
	"sync"

	"github.com/ppgram/server/transport"
)

type Identity struct {
	UserID string
	Roles  []string
}

type Session struct {
	mu sync.RWMutex

	UserID   string
	Identity *Identity
	Claims   map[string]any
	Settings map[string]any
	DB       any

	connections map[*transport.QuicConnection]struct{}
}

func newSession() *Session {
	return &Session{
		Settings:    map[string]any{},
		connections: map[*transport.QuicConnection]struct{}{},
	}
}

func (s *Session) Authenticated() bool {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.Identity != nil && s.Identity.UserID != ""
}

func (s *Session) AddConnection(conn *transport.QuicConnection) {
	if conn == nil {
		return
	}
	s.mu.Lock()
	s.connections[conn] = struct{}{}
	s.mu.Unlock()
}

func (s *Session) RemoveConnection(conn *transport.QuicConnection) {
	if conn == nil {
		return
	}
	s.mu.Lock()
	delete(s.connections, conn)
	s.mu.Unlock()
}

func (s *Session) ConnectionCount() int {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return len(s.connections)
}

func (s *Session) SetSetting(key string, value any) {
	s.mu.Lock()
	s.Settings[key] = value
	s.mu.Unlock()
}

func (s *Session) Setting(key string) (any, bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	v, ok := s.Settings[key]
	return v, ok
}

func (s *Session) SetDB(db any) {
	s.mu.Lock()
	s.DB = db
	s.mu.Unlock()
}

func (s *Session) ApplyAuthentication(identity *Identity, claims map[string]any) {
	if identity == nil || identity.UserID == "" {
		return
	}

	s.mu.Lock()
	s.UserID = identity.UserID
	s.Identity = identity
	if claims != nil {
		s.Claims = claims
	}
	s.mu.Unlock()
}

type Registry struct {
	mu sync.RWMutex

	byConnection map[*transport.QuicConnection]*Session
	byUserID     map[string]*Session
}

func NewRegistry() *Registry {
	return &Registry{
		byConnection: map[*transport.QuicConnection]*Session{},
		byUserID:     map[string]*Session{},
	}
}

func (r *Registry) RegisterConnection(conn *transport.QuicConnection) *Session {
	if conn == nil {
		return nil
	}

	r.mu.Lock()
	defer r.mu.Unlock()

	if existing, ok := r.byConnection[conn]; ok {
		return existing
	}

	s := newSession()
	s.AddConnection(conn)
	r.byConnection[conn] = s
	return s
}

func (r *Registry) SessionByConnection(conn *transport.QuicConnection) (*Session, bool) {
	if conn == nil {
		return nil, false
	}

	r.mu.RLock()
	s, ok := r.byConnection[conn]
	r.mu.RUnlock()
	return s, ok
}

func (r *Registry) SessionByUserID(userID string) (*Session, bool) {
	if userID == "" {
		return nil, false
	}

	r.mu.RLock()
	s, ok := r.byUserID[userID]
	r.mu.RUnlock()
	return s, ok
}

func (r *Registry) UnregisterConnection(conn *transport.QuicConnection) {
	if conn == nil {
		return
	}

	r.mu.Lock()
	defer r.mu.Unlock()

	s, ok := r.byConnection[conn]
	if !ok {
		return
	}

	delete(r.byConnection, conn)
	s.RemoveConnection(conn)

	if s.ConnectionCount() == 0 && s.UserID != "" {
		delete(r.byUserID, s.UserID)
	}
}

func (r *Registry) ApplyAuthentication(conn *transport.QuicConnection, identity *Identity, claims map[string]any) *Session {
	if conn == nil {
		return nil
	}
	if identity == nil || identity.UserID == "" {
		s, _ := r.SessionByConnection(conn)
		return s
	}

	r.mu.Lock()
	defer r.mu.Unlock()

	current, ok := r.byConnection[conn]
	if !ok {
		current = newSession()
		current.AddConnection(conn)
		r.byConnection[conn] = current
	}

	target, ok := r.byUserID[identity.UserID]
	if !ok {
		target = current
		r.byUserID[identity.UserID] = target
	}

	if target != current {
		current.mu.RLock()
		for c := range current.connections {
			target.AddConnection(c)
			r.byConnection[c] = target
		}
		current.mu.RUnlock()
	}

	target.ApplyAuthentication(identity, claims)
	return target
}

var globalRegistry = NewRegistry()

func RegisterConnection(conn *transport.QuicConnection) *Session {
	return globalRegistry.RegisterConnection(conn)
}

func SessionByConnection(conn *transport.QuicConnection) (*Session, bool) {
	return globalRegistry.SessionByConnection(conn)
}

func SessionByUserID(userID string) (*Session, bool) {
	return globalRegistry.SessionByUserID(userID)
}

func UnregisterConnection(conn *transport.QuicConnection) {
	globalRegistry.UnregisterConnection(conn)
}

func ApplyAuthentication(conn *transport.QuicConnection, identity *Identity, claims map[string]any) *Session {
	return globalRegistry.ApplyAuthentication(conn, identity, claims)
}
