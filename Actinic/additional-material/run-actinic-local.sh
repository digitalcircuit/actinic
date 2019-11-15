#!/bin/bash
# See http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of this file

# Actinic program directory
# From: "[...]/Actinic/Actinic/additional-material"
# To:   "[...]/Actinic/Actinic/bin/Debug/"
ACTINIC_APP_DIR="$_LOCAL_DIR/../bin/Debug"

#-------------------------------------------------------------
if [ -z "${CONFIG_ACTINIC_LOCAL_LOADED:-}" ]; then
	source "$_LOCAL_DIR/util-actinic-local.sh"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "${CONFIG_ACTINIC_LOCAL_LOADED:-}" ]; then
	# Quit as nothing can happen
	echo "Actinic local configuration module not loaded, does the file 'util-actinic-local.sh' exist? (will now exit)" >&2
	exit 1
fi

if [ "$SETTINGS_ACTINIC_LOCAL_ENABLED" != true ]; then
	# Quit as nothing can happen
	echo "You must configure a local connection in '$ACTINIC_SETTINGS_PATH'." >&2
	echo "Remember to set ACTINIC_TRY_LOCAL=true." >&2
	exit 1
fi

if tmux has-session -t "$ACTINIC_LOCAL_SESSION_NAME" >/dev/null 2>&1 ; then
	# Connect to existing tmux
	tmux attach-session -t "$ACTINIC_LOCAL_SESSION_NAME"
else
	# Launch tmux
	# To run a command on launch, add it like so:
	# (sleep 1 && local_run_actinic_cmd "anim play simple interval time") &
	tmux new-session -s "$ACTINIC_LOCAL_SESSION_NAME" -c "$ACTINIC_APP_DIR" -n "$ACTINIC_LOCAL_TARGET_PANE" "mono Actinic.exe ; echo '(Press any key to exit)' ; read -n ; exit"
fi
