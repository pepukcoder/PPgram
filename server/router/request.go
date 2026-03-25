package router

type Request struct {
	Op   string
	Body []byte
}

type Response struct {
	StatusCode uint32
	Message    string
	Body       []byte
}
