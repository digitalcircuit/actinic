#!/bin/bash
# Builds and copies the necessary files to the output directory for audio processing

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of file

SOURCE_DIR="$_LOCAL_DIR"
TARGET_DIR="$SOURCE_DIR/../bin/Debug"

FLAG_SKIP_AUDIO="$TARGET_DIR/skip-audio"

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

build_and_copy_impulse ()
{
	local CURRENT_DIR="$(pwd)"
	cd "$IMPULSE_DIR"
	make clean init impulse-lib post
	local result=$?
	if [ $result -ne 0 ]; then
		echo " /!\ Could not build libimpulse.so
Do you have all the development headers installed?
> Needed packages: libpulse-dev libfftw3-dev
(Manually run ./prepare-audio-processing.sh to see build output)
Note: G35_USB still works, you just can't use the audio processing functionality.
To ignore this, create a file named 'skip-audio' in the bin/Debug directory." > "$TARGET_DIR/$FILES_WARN_BUILD_FAIL"
		return 1
	elif [ -f "$TARGET_DIR/$FILES_WARN_BUILD_FAIL" ]; then
		# Build no longer failing; remove the warning
		rm "$TARGET_DIR/$FILES_WARN_BUILD_FAIL"
	fi
	# Copy the resulting files
	find "$IMPULSE_BUILD_DIR/" -path "*/impulse/*" -type f -name "$FILES_IMPULSE_MODULE" -exec cp '{}' "$TARGET_DIR/" ';'
	cd "$CURRENT_DIR"
}

if [ ! -f "$FLAG_SKIP_AUDIO" ]; then
	if [ "$IMPULSE_MAKEFILE" -nt "$TARGET_DIR/$FILES_IMPULSE_MODULE" ]\
		|| [ "$IMPULSE_MAIN_SRC" -nt "$TARGET_DIR/$FILES_IMPULSE_MODULE" ]\
		|| [ "$IMPULSE_MAIN_SRC_HEADER" -nt "$TARGET_DIR/$FILES_IMPULSE_MODULE" ]; then
		# Only build if the source file is newer than the output
		echo " * Setting up Impulse library..."
		build_and_copy_impulse
	fi
fi
