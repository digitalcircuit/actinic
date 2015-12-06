#!/bin/bash
# Indicates that G35_USB utility module has been loaded
CONFIG_G35_USB_REMOTE_LOADED="true"

#______Setup______
# Path to remote SSH configuration file
SSH_G35_USB_PATH="$_LOCAL_DIR/config-ssh-g35-usb.sh"

if [ ! -f "$SSH_G35_USB_PATH" ]; then
	# No configuration file exists; create one from scratch
	cat >"$SSH_G35_USB_PATH" <<EOL
#!/bin/bash
# Indicates that SSH configuration has been loaded
CONFIG_SSH_G35_USB_LOADED="true"

# Remote SSH server, e.g. "remote-computer.local"
G35_USB_SSH_SERVER=""
# Port for SSH, e.g. "22"
G35_USB_SSH_PORT=""
# User for connection, e.g. "ubuntu"
G35_USB_SSH_USER=""
EOL
	echo "New configuration written to '$SSH_G35_USB_PATH'."
	echo "Update this with your settings for the remote machine running G35 USB."
	exit 0
fi


#-------------------------------------------------------------
if [ -z "$CONFIG_SSH_G35_USB_LOADED" ]; then
	source "$SSH_G35_USB_PATH"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "$CONFIG_SSH_G35_USB_LOADED" ]; then
	# Quit as nothing can happen
	echo "G35 USB SSH configuration not loaded, does the file 'config-ssh-g35-usb.sh' exist? (will now exit)" >&2
	exit 1
fi
#-------------------------------------------------------------


# Check if any variables undefined or left empty
if [ ! -n "$G35_USB_SSH_SERVER" ] || [ ! -n "$G35_USB_SSH_PORT" ] || [ ! -n "$G35_USB_SSH_USER" ]; then
	echo "Configuration in '$SSH_G35_USB_PATH' is not valid." >&2
	echo "Be sure to define a valid SSH_SERVER, SSH_PORT, and SSH_USER." >&2
	exit 1
fi
#_________________

# Run a command on the remote server
function run_remote_cmd () {
	local EXPECTED_ARGS=1
	if [ $# -lt $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [run_remote_cmd] {command to run}"
		return
	fi
	
	ssh "$G35_USB_SSH_USER"@"$G35_USB_SSH_SERVER" -p "$G35_USB_SSH_PORT" "$*"
}

# Show a temporary notification on the remote server by invoking the remote script
function notification_show_temporary () {
	local EXPECTED_ARGS=7
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [notification_show_temporary] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness} {duration in seconds}"
		return
	fi
	run_remote_cmd "~/system/lights/control-g35_usb-notify.sh 'notification_show_temporary' '$1' '$2' '$3' '$4' '$5' '$6' '$7'" &
	# Use a sub-shell to not force a wait
}
