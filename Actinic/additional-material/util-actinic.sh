#!/bin/bash
# See http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

# Indicates that Actinic utility module has been loaded
CONFIG_ACTINIC_LOADED="true"

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

# Load remote and local utility modules as needed
if [ "$SETTINGS_ACTINIC_REMOTE_ENABLED" = true ]; then
	#-------------------------------------------------------------
	if [ -z "${CONFIG_ACTINIC_REMOTE_LOADED:-}" ]; then
		source "$_LOCAL_DIR/util-actinic-remote.sh"
	fi
	#-------------------------------------------------------------
	# Check if session environment is prepared
	if [ -z "${CONFIG_ACTINIC_REMOTE_LOADED:-}" ]; then
		# Quit as nothing can happen
		echo "Actinic remote configuration module not loaded, does the file 'util-actinic-remote.sh' exist? (will now exit)"
		exit 1
	fi
	#-------------------------------------------------------------
fi

if [ "$SETTINGS_ACTINIC_LOCAL_ENABLED" = true ]; then
	#-------------------------------------------------------------
	if [ -z "${CONFIG_ACTINIC_LOCAL_LOADED:-}" ]; then
		source "$_LOCAL_DIR/util-actinic-local.sh"
	fi
	#-------------------------------------------------------------
	# Check if session environment is prepared
	if [ -z "${CONFIG_ACTINIC_LOCAL_LOADED:-}" ]; then
		# Quit as nothing can happen
		echo "Actinic local configuration module not loaded, does the file 'util-actinic-local.sh' exist? (will now exit)"
		exit 1
	fi
	#-------------------------------------------------------------
fi

# Run an Actinic command on the local or remote server
function run_actinic_cmd () {
	local EXPECTED_ARGS=1
	if [ $# -lt $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [run_actinic_cmd] {command to run}" >&2
		return 1
	fi

	# Run locally first if setting specified
	if [ "$SETTINGS_ACTINIC_LOCAL_ENABLED" = true ]; then
		if local_run_actinic_cmd "$*"; then
			# Command sent successfully, don't forward to remote system
			return 0
		fi
	fi

	# Run on remote device if setting specified
	if [ "$SETTINGS_ACTINIC_REMOTE_ENABLED" = true ]; then
		if remote_run_actinic_cmd "$*"; then
			# Command sent successfully
			return 0
		fi
	fi

	# Nothing configured, command not sent
	return 1
}

# Show a temporary notification on the remote server by invoking the remote script
function notification_show_temporary () {
	local EXPECTED_ARGS=7
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [notification_show_temporary] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness} {duration in seconds}" >&2
		return 1
	fi

	# Run locally first if setting specified
	if [ "$SETTINGS_ACTINIC_LOCAL_ENABLED" = true ]; then
		if local_notification_show_temporary "$1" "$2" "$3" "$4" "$5" "$6" "$7"; then
			# Command sent successfully, don't forward to remote system
			return 0
		fi
	fi

	# Run on remote device if setting specified
	if [ "$SETTINGS_ACTINIC_REMOTE_ENABLED" = true ]; then
		if remote_notification_show_temporary "$1" "$2" "$3" "$4" "$5" "$6" "$7"; then
			# Command sent successfully
			return 0
		fi
	fi

	# Nothing configured, command not sent
	return 1
}
