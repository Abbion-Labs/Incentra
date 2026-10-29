#!/bin/sh
set -eu

if [ -n "${SUPABASE_DB_CA_CERT_BASE64:-}" ]; then
    cert_path="/tmp/supabase-db-ca.crt"
    printf '%s' "$SUPABASE_DB_CA_CERT_BASE64" | base64 -d > "$cert_path"
    export PGSSLROOTCERT="$cert_path"
fi

# Migrate and seed once per Vercel deployment instead of on every cold start.
# Project environment variables with the same names take precedence.
export DbInit__Policy="${DbInit__Policy:-OnDeploy}"
export Deployment__IdentityProvider="${Deployment__IdentityProvider:-Vercel}"
export Deployment__IdentityStore="${Deployment__IdentityStore:-Postgres}"

exec dotnet VariableCompensation.Host.dll --urls "http://0.0.0.0:${PORT:-80}"
