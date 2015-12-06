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

# Server directory
SERVER_DIR="/home/$G35_USB_SSH_USER/system/lights"
# Server directory via GVFS mount
# (Some time in the past, GVFS changed from user name to user ID)
SERVER_LOCAL_GVFS_DIR="/run/user/$(id -u $(whoami))/gvfs/sftp:host=$G35_USB_SSH_SERVER,port=$G35_USB_SSH_PORT,user=$G35_USB_SSH_USER${SERVER_DIR}"
# Local directory
LOCAL_DIR="$_LOCAL_DIR/../bin/Debug"

if ! [ -d "$SERVER_LOCAL_GVFS_DIR" ]; then
	if (whiptail --title "Update G35 USB" --backtitle "G35 USB SSH remote management" --yesno "'$G35_USB_SSH_SERVER' SFTP not connected.\nMount it now?" 10 60 --yes-button "Mount" --no-button "Cancel"); then
		echo "* Mounting directory..."
		gvfs-mount "sftp://$G35_USB_SSH_USER@$G35_USB_SSH_SERVER:$G35_USB_SSH_PORT${SERVER_DIR}"
		echo "> Waiting for mount..."
		sleep 10
	fi
fi

if [ -d "$SERVER_LOCAL_GVFS_DIR" ]; then
	echo "* Copying G35_USB Light manager..."
	cp "$LOCAL_DIR/G35_USB.exe" "$SERVER_LOCAL_GVFS_DIR/G35_USB.exe.new"
	echo "Update done!"
	# Optionally restart the server to apply the change
	source "$_LOCAL_DIR/remote-restart.sh"
else
	echo "You must connect to '$G35_USB_SSH_SERVER' ('$SERVER_DIR') via GNOME VFS SFTP first!"
	sleep 2
fi
