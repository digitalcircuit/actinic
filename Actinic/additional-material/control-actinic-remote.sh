#!/bin/bash

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of this file

#-------------------------------------------------------------
if [ -z "$CONFIG_ACTINIC_REMOTE_LOADED" ]; then
	source "$_LOCAL_DIR/util-actinic-remote.sh"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "$CONFIG_ACTINIC_REMOTE_LOADED" ]; then
	# Quit as nothing can happen
	echo "Actinic remote configuration module not loaded, does the file 'util-actinic-remote.sh' exist? (will now exit)"
	exit 1
fi
#-------------------------------------------------------------

EXPECTED_ARGS=1
if [ $# -ge $EXPECTED_ARGS ]; then
	case $1 in
		"notification_show_temporary" )
			EXPECTED_ARGS=8
			if [ $# -eq $EXPECTED_ARGS ]; then
				notification_show_temporary "$2" "$3" "$4" "$5" "$6" "$7" "$8"
			else
				echo "Usage: `basename "$0"` notification_show_temporary {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness} {duration in seconds}"
			fi
			;;
		* )
			echo "Usage: `basename "$0"` {command: notification_show_temporary}"
			;;
	esac
else
	echo "Usage: `basename "$0"` {command: notification_show_temporary}"
fi
