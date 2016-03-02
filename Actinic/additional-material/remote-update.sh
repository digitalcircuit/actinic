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

# Server directory
SERVER_DIR="/home/$ACTINIC_SSH_USER/system/lights"
# Server directory via GVFS mount
# (Some time in the past, GVFS changed from user name to user ID)
SERVER_LOCAL_GVFS_DIR="/run/user/$(id -u $(whoami))/gvfs/sftp:host=$ACTINIC_SSH_SERVER,port=$ACTINIC_SSH_PORT,user=$ACTINIC_SSH_USER${SERVER_DIR}"
# Local directory
LOCAL_DIR="$_LOCAL_DIR/../bin/Debug"

if ! [ -d "$SERVER_LOCAL_GVFS_DIR" ]; then
	if (whiptail --title "Update Actinic" --backtitle "Actinic SSH remote management" --yesno "'$ACTINIC_SSH_SERVER' SFTP not connected.\nMount it now?" 10 60 --yes-button "Mount" --no-button "Cancel"); then
		echo "* Mounting directory..."
		gvfs-mount "sftp://$ACTINIC_SSH_USER@$ACTINIC_SSH_SERVER:$ACTINIC_SSH_PORT${SERVER_DIR}"
		echo "> Waiting for mount..."
		sleep 10
	fi
fi

if [ -d "$SERVER_LOCAL_GVFS_DIR" ]; then
	echo "* Copying Actinic Light manager..."
	cp "$LOCAL_DIR/Actinic.exe" "$SERVER_LOCAL_GVFS_DIR/Actinic.exe.new"
	echo "Update done!"
	# Optionally restart the server to apply the change
	source "$_LOCAL_DIR/remote-restart.sh"
else
	echo "You must connect to '$ACTINIC_SSH_SERVER' ('$SERVER_DIR') via GNOME VFS SFTP first!"
	sleep 2
fi
