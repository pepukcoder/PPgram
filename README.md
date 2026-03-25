# PPgram

## TLS Cert Generation

Generate a local server certificate and private key:

```bash
./scripts/cert-gen
```

Default output locations:
- certificate: `certs/server.cert.pem`
- private key: `secrets/server.key.pem`

Optional overrides:

```bash
DAYS=30 COMMON_NAME=ppgram.local ./scripts/cert-gen
./scripts/cert-gen /path/to/server.key.pem /path/to/server.cert.pem
```

## Environment Config

Copy and edit env values:

```bash
cp .env.example .env
```

Relevant TLS/QUIC variables:

```env
QUIC_ADDR=0.0.0.0:4242
TLS_CERT_FILE=certs/server.cert.pem
TLS_KEY_FILE=secrets/server.key.pem
TLS_ALPN=ppproto/1.0
```

## Startup

### Podman Compose (recommended)

Start all services from `compose.yml`:

```bash
podman compose up -d --build
```

Rebuild server

```bash
podman compose up -d --build --no-deps app
```

Useful commands:

Compose maps QUIC UDP port:
- host `4433/udp` -> container `4433/udp`
