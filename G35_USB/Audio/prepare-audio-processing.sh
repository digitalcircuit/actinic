#!/bin/bash
# Builds and copies the necessary files to the output directory for audio processing

# Make Bash a little more strict with error-checking
# See: http://redsymbol.net/articles/unofficial-bash-strict-mode/
set -euo pipefail
IFS=$'\n\t'

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of file

SOURCE_DIR="$_LOCAL_DIR"

OPT_ALLOW_FAIL=false

FLAG_SKIP_AUDIO="skip-audio"

FILES_WARN_BUILD_FAIL="README-impulse-failed-to-build"
FILES_IMPULSE_MODULE="libimpulse.so"
#FILES_IMPULSE_PRINTOUT="impulse-print"

IMPULSE_DIR="$SOURCE_DIR/impulse"
IMPULSE_BUILD_DIR="$IMPULSE_DIR/build"
IMPULSE_SRC_DIR="$IMPULSE_DIR/src"

IMPULSE_MAKEFILE="$IMPULSE_DIR/Makefile"
IMPULSE_MAIN_SRC="$IMPULSE_SRC_DIR/impulse.c"
IMPULSE_MAIN_SRC_HEADER="$IMPULSE_SRC_DIR/impulse.h"
#IMPULSE_PRINTOUT_SRC="$IMPULSE_SRC_DIR/impulse-print.c"

print_build_warn ()
{
	echo "/!\ Could not build libimpulse.so
Run ./prepare-audio-processing.sh to see build output.
Do you have all the development headers installed?
> Needed packages on Ubuntu: libpulse-dev libfftw3-dev

Note: G35_USB still works, you just can't use audio processing, namely
the 'vu' commands.  To ignore this, create a file named 'skip-audio'
in this folder, or pass 'allow-fail' to prepare-audio-processing.sh."
}

build_and_copy_impulse ()
{
	# 2 arguments required
	if [ $# -ne 2 ]; then
		echo "Usage: `basename $0` [build_and_copy_impulse] {allow failure: true/false} {target directory}"
		return 1
	fi
	local ALLOW_FAIL="$1"
	local OUTPUT_DIR="$2"
	local CURRENT_DIR="$(pwd)"
	cd "$IMPULSE_DIR"

	# If make fails, capture the output value instead of immediately exiting
	set +e
	make clean init impulse-lib post
	local result=$?

	# Go back to fail by default
	set -e
	if [ $result -ne 0 ]; then
		if [ "$ALLOW_FAIL" = true ] ; then
			print_build_warn > "$OUTPUT_DIR/$FILES_WARN_BUILD_FAIL"
			# Failure is okay, don't exit out
			return 0
		else
			print_build_warn
			return 1
		fi
	elif [ -f "$OUTPUT_DIR/$FILES_WARN_BUILD_FAIL" ]; then
		# Build no longer failing; remove the warning
		rm "$OUTPUT_DIR/$FILES_WARN_BUILD_FAIL"
	fi
	# Copy the resulting files
	find "$IMPULSE_BUILD_DIR/" -path "*/impulse/*" -type f -name "$FILES_IMPULSE_MODULE" -exec cp '{}' "$OUTPUT_DIR/" ';'
	cd "$CURRENT_DIR"
}

check_impulse ()
{
	# 2 arguments required
	if [ $# -ne 2 ]; then
		echo "Usage: `basename $0` [check_impulse] {allow failure: true/false} {target directory}"
		return 1
	fi
	local ALLOW_FAIL="$1"
	local OUTPUT_DIR="$2"
	mkdir -p "$OUTPUT_DIR"
	if [ ! -f "$OUTPUT_DIR/$FLAG_SKIP_AUDIO" ]; then
		if [ "$IMPULSE_MAKEFILE" -nt "$OUTPUT_DIR/$FILES_IMPULSE_MODULE" ]\
			|| [ "$IMPULSE_MAIN_SRC" -nt "$OUTPUT_DIR/$FILES_IMPULSE_MODULE" ]\
			|| [ "$IMPULSE_MAIN_SRC_HEADER" -nt "$OUTPUT_DIR/$FILES_IMPULSE_MODULE" ]; then
			# Only build if the source file is newer than the output
			echo " * Setting up Impulse library in '$OUTPUT_DIR'..."
			build_and_copy_impulse "$1" "$OUTPUT_DIR"
			# Propagate the status down the chain
			return $?
		fi
	fi
}

print_usage ()
{
	echo "Usage: `basename $0` {build type: debug, release} {optional flag: allow-fail}"
}

# At least 1 argument provided
if [ $# -ge 1 ]; then
	case "${1,,}" in
		"debug" )
			TARGET_DIR="$SOURCE_DIR/../bin/Debug"
			;;
		"release" )
			TARGET_DIR="$SOURCE_DIR/../bin/Release"
			;;
		* )
			print_usage
			exit 1
			;;
	esac
	# At least 2 arguments provided
	if [ $# -ge 2 ]; then
		case "${2,,}" in
			"allow-fail" )
				OPT_ALLOW_FAIL=true
				;;
			* )
				print_usage
				exit 1
				;;
		esac
	# else - Nothing to do, it's an optional argument
	fi
else
	print_usage
	exit 1
fi

check_impulse "$OPT_ALLOW_FAIL" "$TARGET_DIR"
