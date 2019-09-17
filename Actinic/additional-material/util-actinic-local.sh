#!/bin/bash
# See http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail

# Indicates that the local Actinic utility module has been loaded
CONFIG_ACTINIC_LOCAL_LOADED="true"

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

ACTINIC_CACHE_DIR="${XDG_CACHE_HOME:-$HOME/.cache}/actinic"
# Used for keeping track of ongoing notifications

function local_actinic_available () {
	# Check for tmux session
	if tmux has-session -t "=$ACTINIC_LOCAL_SESSION_NAME" >/dev/null 2>&1 ; then
		return 0
	else
		# Session not available
		return 1
	fi
}

function local_run_actinic_cmd () {
	local EXPECTED_ARGS=1
	if [ $# -lt $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [local_run_actinic_cmd] {command to run}" >&2
		return 1
	fi

	# Check for tmux session
	if local_actinic_available; then
		# Send to the specific pane of the configured session
		tmux send-keys -t "=$ACTINIC_LOCAL_SESSION_NAME:$ACTINIC_LOCAL_TARGET_PANE" "$1" C-m
	else
		# Session not available
		return 1
	fi
}

function local_notification_show_temporary {
	local EXPECTED_ARGS=7
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename $0` [local_notification_show_temporary] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness} {duration in seconds}"
		return 1
	fi

	if ! local_actinic_available; then
		return 1
	fi

	if [ ! -d "$ACTINIC_CACHE_DIR" ]; then
		mkdir -p "$ACTINIC_CACHE_DIR"
	fi

	local notification_name_hash="$(echo -n \"$1\" | md5sum | cut -d " " -f 1)"

	local notification_file="$ACTINIC_CACHE_DIR/currently-showing-$notification_name_hash"
	local notification_id="$RANDOM"
	# Generate a random identifier
	echo "$notification_id" > "$notification_file"
	# Store this into a temporary file

	# Run this in a sub-shell so the function won't block
	# This type of sub-shell will inherit all variables
	(
		if ! local_notification_show "$1" "$2" "$3" "$4" "$5" "$6"; then
			# Failed, cleanup
			rm "$notification_file"
			return 1
		fi
		# Show the notification
		sleep "$7"
		# Wait the requested time
		if [ -f "$notification_file" ]; then
			# File exists, check if it's been changed
			if grep -q "$notification_id" "$notification_file"; then
				# If nothing has overridden the file, it's safe to hide the notification without causing interference
				local_notification_hide "$1"
				# Hide the notification
				rm "$notification_file"
				# Remove the notification file as this is no longer showing
			else
				echo "Notification '$1' overridden, not hiding it"
			fi
		fi
	) &
}

function local_notification_show {
	local EXPECTED_ARGS=6
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename $0` [local_notification_show] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness}"
		return 1
	fi
	local_run_actinic_cmd "overlay $1-notif color $2 $3 $4 $5 0 && overlay $1-notif blending favor && overlay $1-notif brightness $2 $6" || return 1
}

function local_notification_show_protected {
	local EXPECTED_ARGS=7
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename $0` [local_notification_show_protected] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {LEDs to darken: 'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness}"
		return 1
	fi
	local_run_actinic_cmd "overlay $1-notif brightness $3 255 && overlay $1-notif color $2 $4 $5 $6 keep && overlay $1-notif blending favor && overlay $1-notif brightness $2 $7" || return 1
}

function local_notification_hide {
	local EXPECTED_ARGS=1
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename $0` [local_notification_hide] {name of notification}"
		return 1
	fi
	local_run_actinic_cmd "overlay $1-notif color all 0 0 0 0" || return 1
}
