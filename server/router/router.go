package router

import (
	"context"
	"log/slog"
	"regexp"
	"strings"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/core/session"
	"github.com/ppgram/server/logging"
	protomsg "github.com/ppgram/server/protomsg"
	"github.com/ppgram/server/transport"
	"github.com/ppgram/server/utils"
	"google.golang.org/protobuf/proto"
)

var dotRoutePattern = regexp.MustCompile(`^[A-Za-z_]+(\.[A-Za-z_]+)*$`)

type StreamType string

const (
	StreamResponse       StreamType = "response"
	StreamServerUpdates  StreamType = "server_stream"
	StreamClientUpdates  StreamType = "client_stream"
	StreamServerTransfer StreamType = "server_transfer"
	StreamClientTransfer StreamType = "client_transfer"
)

type ResponseHandler func(*ResponseContext) error
type ServerUpdatesHandler func(*ServerUpdatesContext) error
type ClientUpdatesHandler func(*ClientUpdatesContext) error
type ServerTransferHandler func(*ServerTransferContext) error
type ClientTransferHandler func(*ClientTransferContext) error

type route struct {
	streamType StreamType
	path       string
	middleware []Middleware
	handler    Handler
}

type Router struct {
	root       *Router
	prefix     string
	middleware []Middleware
	routes     map[string]route
}

func New() *Router {
	r := &Router{
		routes: make(map[string]route),
	}
	r.root = r
	return r
}

func (r *Router) Group(prefix string, middleware ...Middleware) *Router {
	if !isDotRoute(prefix) {
		panic("invalid route syntax")
	}

	return &Router{
		root:       r.getRoot(),
		prefix:     joinRoute(r.prefix, prefix),
		middleware: append(append([]Middleware{}, r.middleware...), middleware...),
	}
}

func (r *Router) Use(middleware ...Middleware) {
	r.middleware = append(r.middleware, middleware...)
}

func (r *Router) Response(path string, handler ResponseHandler, middleware ...Middleware) {
	r.register(StreamResponse, path, middleware, func(base *BaseContext) error {
		ctx, err := NewResponseContext(base)
		if err != nil {
			return err
		}
		return handler(ctx)
	})
}

func (r *Router) ServerUpdates(path string, handler ServerUpdatesHandler, middleware ...Middleware) {
	r.register(StreamServerUpdates, path, middleware, func(base *BaseContext) error {
		ctx, err := NewServerUpdatesContext(base)
		if err != nil {
			return err
		}
		return handler(ctx)
	})
}

func (r *Router) ClientUpdates(path string, handler ClientUpdatesHandler, middleware ...Middleware) {
	r.register(StreamClientUpdates, path, middleware, func(base *BaseContext) error {
		ctx, err := NewClientUpdatesContext(base)
		if err != nil {
			return err
		}
		return handler(ctx)
	})
}

func (r *Router) ServerTransfer(path string, handler ServerTransferHandler, middleware ...Middleware) {
	r.register(StreamServerTransfer, path, middleware, func(base *BaseContext) error {
		ctx, err := NewServerTransferContext(base)
		if err != nil {
			return err
		}
		return handler(ctx)
	})
}

func (r *Router) ClientTransfer(path string, handler ClientTransferHandler, middleware ...Middleware) {
	r.register(StreamClientTransfer, path, middleware, func(base *BaseContext) error {
		ctx, err := NewClientTransferContext(base)
		if err != nil {
			return err
		}
		return handler(ctx)
	})
}

func (r *Router) Handle(ctx context.Context, userSession *session.Session, stream *transport.QuicStream) {
	_ = userSession
	connID := stream.ConnectionID()
	streamID := stream.ID()

	frame, err := stream.ReadFrame()
	if err != nil {
		logging.NetworkLogger().Error("read request frame failed", "conn_id", connID, "stream_id", streamID, "error", err)
		_ = stream.Close()
		return
	}

	envelope := &protomsg.RequestEnvelope{}
	if err := proto.Unmarshal(frame.Payload, envelope); err != nil {
		logging.NetworkLogger().Warn("request rejected", "conn_id", connID, "stream_id", streamID, "reason", "invalid request envelope", "status_code", uint32(core.ErrCodeBadRequest))
		if writeErr := sendErrorResponse(stream, core.ErrCodeBadRequest, "invalid request envelope"); writeErr != nil {
			slog.Error("send error response failed", "error", writeErr)
		}
		_ = stream.Close()
		return
	}

	op := strings.TrimSpace(envelope.GetOp())
	if op == "" {
		logging.NetworkLogger().Info("request rejected", "conn_id", connID, "stream_id", streamID, "reason", "route op is required", "status_code", uint32(core.ErrCodeBadRequest))
		if writeErr := sendErrorResponse(stream, core.ErrCodeBadRequest, "route op is required"); writeErr != nil {
			slog.Error("send error response failed", "error", writeErr)
		}
		_ = stream.Close()
		return
	}

	if !isDotRoute(op) {
		logging.NetworkLogger().Info("request rejected", "conn_id", connID, "stream_id", streamID, "op", op, "reason", "invalid route", "status_code", uint32(core.ErrCodeBadRequest))
		if writeErr := sendErrorResponse(stream, core.ErrCodeBadRequest, "invalid route"); writeErr != nil {
			slog.Error("send error response failed", "error", writeErr)
		}
		_ = stream.Close()
		return
	}
	path := normalizeRoute(op)
	root := r.getRoot()
	route, ok := root.routes[path]
	if !ok {
		logging.NetworkLogger().Info("request rejected", "conn_id", connID, "stream_id", streamID, "op", path, "reason", "route not found", "status_code", uint32(core.ErrCodeNotFound))
		if writeErr := sendErrorResponse(stream, core.ErrCodeNotFound, "route not found"); writeErr != nil {
			slog.Error("send error response failed", "error", writeErr)
		}
		_ = stream.Close()
		return
	}

	base := newBaseContext(ctx, stream, route.middleware, route.handler)
	base.Op = route
	base.Bytes = envelope.Request
	if base.Bytes == nil {
		base.Bytes = []byte{}
	}
	logging.NetworkLogger().Info("request accepted", "conn_id", connID, "stream_id", streamID, "op", path, "stream_type", route.streamType)
	if err := base.Next(); err != nil {
		slog.Error("route handler failed", "route", route.path, "error", err)
		logging.NetworkLogger().Error("request failed", "conn_id", connID, "stream_id", streamID, "op", route.path, "status_code", uint32(core.ErrCodeInternal), "error", err)
		if writeErr := sendErrorResponse(stream, core.ErrCodeInternal, "internal error"); writeErr != nil {
			slog.Error("send error response failed", "error", writeErr)
		}
		_ = stream.Close()
		return
	}
	logging.NetworkLogger().Info("request completed", "conn_id", connID, "stream_id", streamID, "op", path)
}

func (r *Router) ServeConnection(ctx context.Context, conn *transport.QuicConnection) error {
	if conn.ID() == "" {
		conn.SetID(utils.NewUID("c"))
	}
	logging.NetworkLogger().Info("connection opened", "conn_id", conn.ID(), "remote_addr", conn.RemoteAddr().String(), "local_addr", conn.LocalAddr().String())

	userSession := session.RegisterConnection(conn)
	defer session.UnregisterConnection(conn)

	for {
		stream, err := conn.AcceptStream(ctx)
		if err != nil {
			return err
		}

		go r.Handle(ctx, userSession, stream)
	}
}

func (r *Router) register(streamType StreamType, path string, routeMiddleware []Middleware, handler Handler) {
	if !isDotRoute(path) {
		panic("invalid route syntax")
	}

	fullPath := joinRoute(r.prefix, path)
	root := r.getRoot()
	if existingRoute, exists := root.routes[fullPath]; exists {
		panic("duplicate route registration: " + fullPath + " (existing stream type: " + string(existingRoute.streamType) + ")")
	}

	root.routes[fullPath] = route{
		streamType: streamType,
		path:       fullPath,
		middleware: append(append([]Middleware{}, r.middleware...), routeMiddleware...),
		handler:    handler,
	}
}

func (r *Router) getRoot() *Router {
	if r.root == nil {
		return r
	}
	return r.root
}

func normalizeRoute(route string) string {
	route = strings.TrimSpace(route)
	route = strings.Trim(route, ".")
	if route == "" {
		return "root"
	}

	parts := strings.Split(route, ".")
	clean := make([]string, 0, len(parts))
	for _, part := range parts {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		clean = append(clean, part)
	}
	if len(clean) == 0 {
		return "root"
	}
	return strings.Join(clean, ".")
}

func joinRoute(prefix, route string) string {
	p := normalizeRoute(prefix)
	s := normalizeRoute(route)

	if p == "root" {
		return s
	}
	if s == "root" {
		return p
	}
	return p + "." + s
}

func isDotRoute(route string) bool {
	if route == "" {
		return true
	}
	return dotRoutePattern.MatchString(route)
}

func sendErrorResponse(stream *transport.QuicStream, statusCode core.PPStatusCode, message string) error {
	payload, err := proto.Marshal(&protomsg.ResponseEnvelope{
		StatusCode: uint32(statusCode),
		Message:    message,
	})
	if err != nil {
		return err
	}

	return stream.WriteFrame(transport.Frame{
		FrameType: transport.FrameTypeData,
		Payload:   payload,
	})
}
