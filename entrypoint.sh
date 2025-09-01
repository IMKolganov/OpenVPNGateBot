#!/bin/sh

echo "[entrypoint] Current UID: $(id -u), GID: $(id -g)"

# ✅ Ensure resources/certs directory exists
echo "[entrypoint] Ensuring /app/resources/certs directory exists..."
mkdir -p /app/resources/certs
chown -R app:app /app/resources/certs || echo "[entrypoint] chown failed on certs"

echo "[entrypoint] Starting application..."
exec dotnet DataGateVPNBot.dll