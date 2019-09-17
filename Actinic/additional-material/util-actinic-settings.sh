#!/bin/bash
# See http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

# Indicates that Actinic utility module has been loaded
CONFIG_ACTINIC_SETTINGS_LOADED="true"

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of this file

SETTINGS_ACTINIC_LOCAL_ENABLED="false"
SETTINGS_ACTINIC_REMOTE_ENABLED="false"

# Whether or not to try local/remote connections
ACTINIC_TRY_LOCAL="false"
ACTINIC_TRY_REMOTE="false"

# Check if a command exists
util_cmd_exists ()
{
	local EXPECTED_ARGS=1
	if [[ $# -ne $EXPECTED_ARGS ]]; then
		echo "Usage: `basename $0` [util_cmd_exists] {command name}" >&2
		return 1
	fi
	local CMD_NAME="$1"

	# Check if the command exists, return 0 (success) if found, else 1 (fail)
	if command -v "$CMD_NAME" >/dev/null 2>&1; then
		return 0
	else
		return 1
	fi
}

#______Setup______
# Path to remote Actinic configuration file
ACTINIC_SETTINGS_PATH="$_LOCAL_DIR/config-actinic.sh"

if [ ! -f "$ACTINIC_SETTINGS_PATH" ]; then
	# No configuration file exists; create one from scratch
	cat >"$ACTINIC_SETTINGS_PATH" <<EOL
#!/bin/bash
# Indicates that SSH configuration has been loaded
SETTINGS_ACTINIC_LOADED="true"

# [Local connection]
#
# If true, use the below information to try connecting to a local session
ACTINIC_TRY_LOCAL="false"
# Local tmux session name, e.g. "tmux"
ACTINIC_LOCAL_SESSION_NAME=""
# Local tmux target pane name, e.g. "Actinic"
ACTINIC_LOCAL_TARGET_PANE=""
#
# NOTE: If this local session is available, no remote connections will be made.
# This lets you automate a local instance of Actinic until a proper API exists.

# [Remote connection]
#
# If true, use the below information to try connecting to a remote session
ACTINIC_TRY_REMOTE="false"
# Remote SSH server, e.g. "remote-computer.local"
ACTINIC_SSH_SERVER=""
# Port for SSH, e.g. "22"
ACTINIC_SSH_PORT=""
# User for connection, e.g. "ubuntu"
ACTINIC_SSH_USER=""
EOL
	echo "New configuration written to '$ACTINIC_SETTINGS_PATH'."
	echo "Update this with your settings for the device running Actinic."
	exit 0
fi

#-------------------------------------------------------------
if [ -z "${SETTINGS_ACTINIC_LOADED:-}" ]; then
	source "$ACTINIC_SETTINGS_PATH"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "${SETTINGS_ACTINIC_LOADED:-}" ]; then
	# Quit as nothing can happen
	echo "Actinic configuration not loaded, does the file 'config-actinic.sh' exist? (will now exit)" >&2
	exit 1
fi
#-------------------------------------------------------------

# Check if tmux session is set and accessible
if [ "$ACTINIC_TRY_LOCAL" == true ]; then
	if [ -n "$ACTINIC_LOCAL_SESSION_NAME" ] && [ -n "$ACTINIC_LOCAL_TARGET_PANE" ]; then
		# Setting specified, allow trying local tmux sessions
		# Make sure tmux exists
		if util_cmd_exists "tmux"; then
			SETTINGS_ACTINIC_LOCAL_ENABLED="true"
		else
			echo "No local connection; tmux is not found in path." >&2
		fi
	else
		echo "No local connection; '$ACTINIC_SETTINGS_PATH' is missing tmux settings." >&2
		echo "Configure ACTINIC_LOCAL_* or set ACTINIC_TRY_LOCAL=false." >&2
		exit 1
	fi
fi

# Check if SSH parameters set
if [ "$ACTINIC_TRY_REMOTE" == true ]; then
	if [ -n "$ACTINIC_SSH_SERVER" ] && [ -n "$ACTINIC_SSH_PORT" ] && [ -n "$ACTINIC_SSH_USER" ]; then
		SETTINGS_ACTINIC_REMOTE_ENABLED="true"
	else
		echo "No remote connection; '$ACTINIC_SETTINGS_PATH' is missing SSH settings." >&2
		echo "Configure ACTINIC_SSH_* or set ACTINIC_TRY_REMOTE=false." >&2
		exit 1
	fi
fi

# Validate settings
if [ "$SETTINGS_ACTINIC_LOCAL_ENABLED" != true ] && [ "$SETTINGS_ACTINIC_REMOTE_ENABLED" != true ]; then
	# Quit as nothing can happen
	echo "You must configure a local and/or remote connection in '$ACTINIC_SETTINGS_PATH'." >&2
	echo "Set ACTINIC_TRY_LOCAL and/or ACTINIC_TRY_REMOTE to true." >&2
	exit 1
fi
#-------------------------------------------------------------

#_________________
