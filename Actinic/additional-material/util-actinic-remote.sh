#!/bin/bash
# Indicates that Actinic utility module has been loaded
CONFIG_ACTINIC_REMOTE_LOADED="true"

#______Setup______
# Path to remote SSH configuration file
SSH_ACTINIC_PATH="$_LOCAL_DIR/config-ssh-actinic.sh"

if [ ! -f "$SSH_ACTINIC_PATH" ]; then
	# No configuration file exists; create one from scratch
	cat >"$SSH_ACTINIC_PATH" <<EOL
#!/bin/bash
# Indicates that SSH configuration has been loaded
CONFIG_SSH_ACTINIC_LOADED="true"

# Remote SSH server, e.g. "remote-computer.local"
ACTINIC_SSH_SERVER=""
# Port for SSH, e.g. "22"
ACTINIC_SSH_PORT=""
# User for connection, e.g. "ubuntu"
ACTINIC_SSH_USER=""
EOL
	echo "New configuration written to '$SSH_ACTINIC_PATH'."
	echo "Update this with your settings for the remote machine running Actinic."
	exit 0
fi


#-------------------------------------------------------------
if [ -z "$CONFIG_SSH_ACTINIC_LOADED" ]; then
	source "$SSH_ACTINIC_PATH"
fi
#-------------------------------------------------------------
# Check if session environment is prepared
if [ -z "$CONFIG_SSH_ACTINIC_LOADED" ]; then
	# Quit as nothing can happen
	echo "Actinic SSH configuration not loaded, does the file 'config-ssh-actinic.sh' exist? (will now exit)" >&2
	exit 1
fi
#-------------------------------------------------------------


# Check if any variables undefined or left empty
if [ ! -n "$ACTINIC_SSH_SERVER" ] || [ ! -n "$ACTINIC_SSH_PORT" ] || [ ! -n "$ACTINIC_SSH_USER" ]; then
	echo "Configuration in '$SSH_ACTINIC_PATH' is not valid." >&2
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

	ssh "$ACTINIC_SSH_USER"@"$ACTINIC_SSH_SERVER" -p "$ACTINIC_SSH_PORT" "$*"
}

# Show a temporary notification on the remote server by invoking the remote script
function notification_show_temporary () {
	local EXPECTED_ARGS=7
	if [ $# -ne $EXPECTED_ARGS ]; then
		echo "Usage: `basename "$0"` [notification_show_temporary] {name of notification} {'all', hyphen-separated range of LEDs, or a single LED} {R} {G} {B} {brightness} {duration in seconds}"
		return
	fi
	run_remote_cmd "~/system/lights/control-actinic-notify.sh 'notification_show_temporary' '$1' '$2' '$3' '$4' '$5' '$6' '$7'" &
	# Use a sub-shell to not force a wait
}
