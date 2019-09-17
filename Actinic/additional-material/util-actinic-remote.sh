#!/bin/bash
# See http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

# Indicates that the remote Actinic utility module has been loaded
CONFIG_ACTINIC_REMOTE_LOADED="true"

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of this file

#-------------------------------------------------------------
if [ -z "${CONFIG_ACTINIC_SETTINGS_LOADED:-}" ]; then
	source "$_LOCAL_DIR/util-actinic-settings.sh"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "${CONFIG_ACTINIC_SETTINGS_LOADED:-}" ]; then
	# Quit as nothing can happen
	echo "Actinic configuration module not loaded, does the file 'util-actinic-settings.sh' exist? (will now exit)"
	exit 1
fi
#-------------------------------------------------------------

# Run a command on the remote server
function remote_run_cmd () {
	local EXPECTED_ARGS=1
	if [ $# -lt $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [remote_run_cmd] {command to run}" >&2
		return
	fi

	ssh "$ACTINIC_SSH_USER"@"$ACTINIC_SSH_SERVER" -p "$ACTINIC_SSH_PORT" "$*"
}

# Run an Actinic command on the remote server
function remote_run_actinic_cmd () {
	local EXPECTED_ARGS=1
	if [ $# -lt $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [remote_run_actinic_cmd] {command to run}" >&2
		return 1
	fi

	# Run on the remote system
	remote_run_cmd "~/system/lights/control-actinic.sh '$*'"
}

# Show a temporary notification on the remote server by invoking the remote script
function remote_notification_show_temporary () {
	local EXPECTED_ARGS=7
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [remote_notification_show_temporary] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness} {duration in seconds}" >&2
		return 1
	fi

	remote_run_cmd "~/system/lights/control-actinic-notify.sh 'notification_show_temporary' '$1' '$2' '$3' '$4' '$5' '$6' '$7'" &
	# Use a sub-shell to not force a wait
}
