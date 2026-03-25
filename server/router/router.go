package router

import (
	"context"
	"log/slog"
	"regexp"
	"strings"

	"github.com/ppgram/server/core"
	"github.com/ppgram/server/core/session"
	protomsg "github.com/ppgram/server/protomsg"
	"github.com/ppgram/server/transport"
	"google.golang.org/protobuf/proto"
)

var dotRoutePattern = regexp.MustCompile(`^[A-Za-z_]+(\.[A-Za-z_]+)*$`)

type StreamType string

const (
	StreamResponse       StreamType = "response"
	StreamServerStream   StreamType = "server_stream"
	StreamClientStream   StreamType = "client_stream"
	StreamBidirectional  StreamType = "bidirectional"
	StreamServerTransfer StreamType = "server_transfer"
	StreamClientTransfer StreamType = "client_transfer"
)

type ResponseHandler func(*ResponseContext) error
type ServerStreamHandler func(*ServerStreamContext) error
type ClientStreamHandler func(*ClientStreamContext) error
type BidirectionalHandler func(*BidirectionalContext) error
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
		return handler(&ResponseContext{BaseContext: base})
	})
}

func (r *Router) ServerStream(path string, handler ServerStreamHandler, middleware ...Middleware) {
	r.register(StreamServerStream, path, middleware, func(base *BaseContext) error {
		return handler(&ServerStreamContext{BaseContext: base})
	})
}

func (r *Router) ClientStream(path string, handler ClientStreamHandler, middleware ...Middleware) {
	r.register(StreamClientStream, path, middleware, func(base *BaseContext) error {
		return handler(&ClientStreamContext{BaseContext: base})
	})
}

func (r *Router) Bidirectional(path string, handler BidirectionalHandler, middleware ...Middleware) {
	r.register(StreamBidirectional, path, middleware, func(base *BaseContext) error {
		return handler(&BidirectionalContext{BaseContext: base})
	})
}

func (r *Router) ServerTransfer(path string, handler ServerTransferHandler, middleware ...Middleware) {
	r.register(StreamServerTransfer, path, middleware, func(base *BaseContext) error {
		return handler(&ServerTransferContext{BaseContext: base})
	})
}

func (r *Router) ClientTransfer(path string, handler ClientTransferHandler, middleware ...Middleware) {
	r.register(StreamClientTransfer, path, middleware, func(base *BaseContext) error {
		return handler(&ClientTransferContext{BaseContext: base})
	})
}

func (r *Router) Handle(ctx context.Context, userSession *session.Session, stream *transport.QuicStream) {
	_ = userSession

	frame, err := stream.ReadFrame()
	if err != nil {
		slog.Error("read request frame", "error", err)
		_ = stream.Close()
		return
	}

	envelope := &protomsg.Request{}
	if err := proto.Unmarshal(frame.Payload, envelope); err != nil {
		slog.Error("decode request envelope", "error", err)
		_ = sendErrorResponse(stream, uint32(core.ErrCodeBadRequest), "invalid request envelope")
		_ = stream.Close()
		return
	}

	op := strings.TrimSpace(envelope.GetOp())
	if op == "" {
		slog.Warn("empty route op")
		_ = sendErrorResponse(stream, uint32(core.ErrCodeBadRequest), "route op is required")
		_ = stream.Close()
		return
	}

	if !isDotRoute(op) {
		slog.Warn("invalid route", "op", op)
		_ = sendErrorResponse(stream, uint32(core.ErrCodeBadRequest), "invalid route")
		_ = stream.Close()
		return
	}
	path := normalizeRoute(op)
	root := r.getRoot()
	route, ok := root.routes[path]
	if !ok {
		slog.Warn("route not found", "route", path)
		_ = sendErrorResponse(stream, uint32(core.ErrCodeNotFound), "route not found")
		_ = stream.Close()
		return
	}

	request := &Request{
		Op:   path,
		Body: envelope.GetBody(),
	}

	base := newBaseContext(ctx, request, stream, route.middleware, route.handler)
	if err := base.Next(); err != nil {
		slog.Error("route handler failed", "route", route.path, "error", err)
		_ = sendErrorResponse(stream, uint32(core.ErrCodeInternal), "internal error")
		_ = stream.Close()
	}
}

func sendErrorResponse(stream *transport.QuicStream, statusCode uint32, message string) error {
	return writeResponseFrame(stream, &Response{
		StatusCode: statusCode,
		Message:    message,
	})
}

func (r *Router) ServeConnection(ctx context.Context, conn *transport.QuicConnection) error {
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
