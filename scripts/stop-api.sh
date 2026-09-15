#!/usr/bin/env bash
# Zaustavi VariableCompensation.Host ako je još uvek pokrenut u pozadini.

echo "Stopping VariableCompensation.Host..."

if command -v taskkill >/dev/null 2>&1; then
  taskkill //F //IM VariableCompensation.Host.exe >/dev/null 2>&1 || true
fi

if command -v powershell >/dev/null 2>&1; then
  powershell -Command "Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id \$_.OwningProcess -Force -ErrorAction SilentlyContinue }" >/dev/null 2>&1 || true
fi

echo "Done. You can run: cd src/VariableCompensation.Host && dotnet run"
