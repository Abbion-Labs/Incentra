#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Backend unit tests"
dotnet test tests/VariableCompensation.Application.Tests/VariableCompensation.Application.Tests.csproj --configuration Release
dotnet test tests/VariableCompensation.Architecture.Tests/VariableCompensation.Architecture.Tests.csproj --configuration Release
dotnet test tests/VariableCompensation.Domain.Tests/VariableCompensation.Domain.Tests.csproj --configuration Release

echo "==> Backend integration tests (requires Docker)"
dotnet test tests/VariableCompensation.Integration.Tests/VariableCompensation.Integration.Tests.csproj --configuration Release

echo "==> Frontend unit tests"
cd frontend
npm test
cd "$ROOT"

if [[ "${RUN_E2E:-0}" == "1" ]]; then
  echo "==> E2E tests"
  cd tests/e2e
  npm test
fi

echo "All tests passed."
