#!/bin/sh
set -eu

if [ -n "${SUPABASE_DB_CA_CERT_BASE64:-}" ]; then
    cert_path="/tmp/supabase-db-ca.crt"
    printf '%s' "$SUPABASE_DB_CA_CERT_BASE64" | base64 -d > "$cert_path"
    export PGSSLROOTCERT="$cert_path"
fi

if [ "${1:-}" = "--migrate" ]; then
    exec dotnet VariableCompensation.Host.dll --migrate
fi

exec dotnet VariableCompensation.Host.dll --urls "http://0.0.0.0:${PORT:-80}"
