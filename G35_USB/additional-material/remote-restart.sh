#!/bin/bash

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of this file

#-------------------------------------------------------------
if [ -z "$CONFIG_G35_USB_REMOTE_LOADED" ]; then
	source "$_LOCAL_DIR/util-g35-usb-remote.sh"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "$CONFIG_G35_USB_REMOTE_LOADED" ]; then
	# Quit as nothing can happen
	echo "G35 USB remote configuration module not loaded, does the file 'util-g35-usb-remote.sh' exist? (will now exit)"
	exit 1
fi
#-------------------------------------------------------------

if (whiptail --title "Restart G35 USB manager" --backtitle "G35 USB SSH remote management" --yesno "Restart G35 USB on the server?\nAny unsaved configuration or commands will be lost" 10 60 --yes-button "Restart" --no-button "Cancel" --defaultno); then
	echo "* Restarting server instance of G35 USB..."
	run_remote_cmd "~/system/lights/restart-g35_usb.sh" &
	echo "Restart done!"
fi
