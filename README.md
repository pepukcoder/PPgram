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

## Startup (currently Podman is used)

Compose maps QUIC UDP port:
- host `4433/udp` -> container `4433/udp`

Start all services from `compose.yml`:

```bash
podman compose up -d --build
```

Rebuild server

```bash
podman compose up -d --build --no-deps app
```
