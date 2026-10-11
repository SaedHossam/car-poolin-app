#!/bin/bash
# Prepares a Claude Code cloud session so the backend and frontend can be
# built, linted, and tested. Local machines are left untouched.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

ROOT="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "$0")/../.." && pwd)}"
DOTNET_ROOT="$HOME/.dotnet"
NODE_MAJOR=24

export DOTNET_ROOT DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

# .NET 10 SDK. global.json allows any 10.0 feature band from 10.0.102 up.
if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.0\.'; then
  installer="$(mktemp)"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$installer"
  bash "$installer" --channel 10.0 --install-dir "$DOTNET_ROOT"
  rm -f "$installer"
fi

# Node 24, as required by src/frontend/package.json engines.
export NVM_DIR="${NVM_DIR:-/opt/nvm}"
# shellcheck disable=SC1091
. "$NVM_DIR/nvm.sh"
nvm install "$NODE_MAJOR" >/dev/null
NODE_BIN="$(dirname "$(nvm which "$NODE_MAJOR")")"
export PATH="$NODE_BIN:$PATH"

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export DOTNET_ROOT=\"$DOTNET_ROOT\""
    echo "export DOTNET_NOLOGO=1"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export NEXT_TELEMETRY_DISABLED=1"
    echo "export PATH=\"$NODE_BIN:$DOTNET_ROOT:$DOTNET_ROOT/tools:\$PATH\""
  } >> "$CLAUDE_ENV_FILE"
fi

cd "$ROOT"
dotnet tool restore
dotnet restore Carpool.slnx --locked-mode

cd "$ROOT/src/frontend"
npm install --no-audit --no-fund
if [ ! -f .env.local ]; then
  cp .env.example .env.local
fi
