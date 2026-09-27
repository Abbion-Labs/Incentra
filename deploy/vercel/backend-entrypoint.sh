#!/bin/sh
set -eu

entrypoint_start_ms="$(date +%s%3N)"
entrypoint_start_utc="$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")"
echo "[StartupProfile] Container entrypoint started at $entrypoint_start_utc (epoch_ms=$entrypoint_start_ms)"

if [ -f /container-image-profile.txt ]; then
    cat /container-image-profile.txt
fi

if [ -n "${SUPABASE_DB_CA_CERT_BASE64:-}" ]; then
    cert_start_ms="$(date +%s%3N)"
    cert_path="/tmp/supabase-db-ca.crt"
    printf '%s' "$SUPABASE_DB_CA_CERT_BASE64" | base64 -d > "$cert_path"
    export PGSSLROOTCERT="$cert_path"
    cert_end_ms="$(date +%s%3N)"
    echo "[StartupProfile] CA certificate setup took $((cert_end_ms - cert_start_ms)) ms"
fi

before_dotnet_ms="$(date +%s%3N)"
before_dotnet_utc="$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")"
echo "[StartupProfile] Executing dotnet at $before_dotnet_utc; entrypoint work took $((before_dotnet_ms - entrypoint_start_ms)) ms"

exec dotnet VariableCompensation.Host.dll --urls "http://0.0.0.0:${PORT:-80}"
