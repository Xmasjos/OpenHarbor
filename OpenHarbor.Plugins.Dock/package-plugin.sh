#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FRONTEND_DIR="$SCRIPT_DIR/frontend"
PROJECT_FILE="$SCRIPT_DIR/OpenHarbor.Plugins.Dock.csproj"
PACKAGE_PATH="$SCRIPT_DIR/artifacts/OpenHarbor.Dock.zip"

for command_name in npm dotnet; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    printf 'Required command not found: %s\n' "$command_name" >&2
    exit 1
  fi
done

(
  cd "$FRONTEND_DIR"
  npm ci
  npm run build
)

dotnet build "$PROJECT_FILE" --configuration Release

if [[ ! -f "$PACKAGE_PATH" ]]; then
  printf 'Plugin package was not produced: %s\n' "$PACKAGE_PATH" >&2
  exit 1
fi

printf 'Created plugin package: %s\n' "$PACKAGE_PATH"