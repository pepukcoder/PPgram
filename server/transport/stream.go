package transport

import (
	"encoding/binary"
	"errors"
	"io"

	"github.com/quic-go/quic-go"
)

type FrameType uint8

const (
	FrameTypeInvalid FrameType = iota
	FrameTypePing
	FrameTypePong
	FrameTypeOpen
	FrameTypeClose
	FrameTypeData
)

const (
	frameHeaderSize  = 5
	DefaultChunkSize = 64 * 1024
)

type Frame struct {
	FrameType FrameType
	Length    uint32
	Payload   []byte
}

var (
	ErrNilQuicStream = errors.New("nil QuicStream")
	ErrNilReader     = errors.New("nil reader")
	ErrNilWriter     = errors.New("nil writer")
	ErrFrameTooLarge = errors.New("frame too large")
)

type QuicStream struct {
	stream *quic.Stream
}

func (s *QuicStream) Close() error {
	return s.stream.Close()
}

func (s *QuicStream) WriteFrame(frameType FrameType, payload []byte) error {
	if s == nil || s.stream == nil {
		return ErrNilQuicStream
	}
	if len(payload) > int(^uint32(0)) {
		return ErrFrameTooLarge
	}

	header := [frameHeaderSize]byte{}
	header[0] = byte(frameType)
	binary.BigEndian.PutUint32(header[1:], uint32(len(payload)))

	if _, err := s.stream.Write(header[:]); err != nil {
		return err
	}
	if len(payload) == 0 {
		return nil
	}

	written := 0
	for written < len(payload) {
		n, err := s.stream.Write(payload[written:])
		written += n
		if err != nil {
			return err
		}
		if n == 0 {
			return io.ErrShortWrite
		}
	}
	return nil
}

func (s *QuicStream) ReadFrame() (*Frame, error) {
	if s == nil || s.stream == nil {
		return nil, ErrNilQuicStream
	}

	header := [frameHeaderSize]byte{}
	if _, err := io.ReadFull(s.stream, header[:]); err != nil {
		return nil, err
	}

	length := binary.BigEndian.Uint32(header[1:])
	payload := make([]byte, length)
	if length > 0 {
		if _, err := io.ReadFull(s.stream, payload); err != nil {
			return nil, err
		}
	}

	return &Frame{FrameType: FrameType(header[0]), Length: length, Payload: payload}, nil
}

func (s *QuicStream) WriteFrom(reader io.Reader, chunkSize int) (int64, error) {
	if s == nil || s.stream == nil {
		return 0, ErrNilQuicStream
	}
	if reader == nil {
		return 0, ErrNilReader
	}
	if chunkSize <= 0 {
		chunkSize = DefaultChunkSize
	}

	buf := make([]byte, chunkSize)
	var total int64

	for {
		n, rErr := reader.Read(buf)
		if n > 0 {
			written := 0
			for written < n {
				m, wErr := s.stream.Write(buf[written:n])
				total += int64(m)
				written += m
				if wErr != nil {
					return total, wErr
				}
				if m == 0 {
					return total, io.ErrShortWrite
				}
			}
		}
		if errors.Is(rErr, io.EOF) {
			return total, nil
		}
		if rErr != nil {
			return total, rErr
		}
	}
}

func (s *QuicStream) ReadTo(writer io.Writer) (int64, error) {
	if s == nil || s.stream == nil {
		return 0, ErrNilQuicStream
	}
	if writer == nil {
		return 0, ErrNilWriter
	}

	return io.CopyBuffer(writer, s.stream, make([]byte, DefaultChunkSize))
}
