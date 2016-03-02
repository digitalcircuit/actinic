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

if (whiptail --title "Restart Actinic manager" --backtitle "Actinic SSH remote management" --yesno "Restart Actinic on the server?\nAny unsaved configuration or commands will be lost" 10 60 --yes-button "Restart" --no-button "Cancel" --defaultno); then
	echo "* Restarting server instance of Actinic..."
	run_remote_cmd "~/system/lights/restart-actinic.sh"
	echo "Restart done!"
fi
