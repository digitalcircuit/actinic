#!/bin/bash
# See http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

# Run a command in a terminal
# Sadly, x-terminal-emulator does not provide a generic way to run commands

if command -v konsole >/dev/null; then
	konsole -e "$*"
elif command -v gnome-terminal >/dev/null; then
	gnome-terminal --command="$*"
else
	x-terminal-emulator "$*"
fi
