#!/bin/bash
# Builds and copies the necessary files to the output directory for audio processing

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of file

SOURCE_DIR="$_LOCAL_DIR"
TARGET_DIR="$SOURCE_DIR/../bin/Debug"

FILES_SKIP_AUDIO="$TARGET_DIR/skip-audio"

FILES_PYTHON_BRIDGE="print_volume.py"

IMPULSE_PREBUILT_DIR="$SOURCE_DIR/impulse-prebuilt"
IMPULSE_PREBUILT_32BIT_DIR="$IMPULSE_PREBUILT_DIR/i686"
IMPULSE_PREBUILT_64BIT_DIR="$IMPULSE_PREBUILT_DIR/x86_64"

FILES_IMPULSE_LIBIMPULSE="libimpulse.so"
FILES_IMPULSE_IMPULSE="impulse.so"

IMPULSE_DIR="$SOURCE_DIR/Impulse"
IMPULSE_BUILD_DIR="$IMPULSE_DIR/build"

copy_prebuilt_impulse ()
{
	local SOURCE_PATH=""
	if [ "$(getconf LONG_BIT)" = "64" ]; then
		echo " * Copying 64-bit libraries..."
		SOURCE_PATH="$IMPULSE_PREBUILT_64BIT_DIR"
	else
		echo " * Copying 32-bit libraries..."
		SOURCE_PATH="$IMPULSE_PREBUILT_32BIT_DIR"
	fi
	cp --update "$SOURCE_PATH/$FILES_IMPULSE_IMPULSE" "$TARGET_DIR/$FILES_IMPULSE_IMPULSE"
	cp --update "$SOURCE_PATH/$FILES_IMPULSE_LIBIMPULSE" "$TARGET_DIR/$FILES_IMPULSE_LIBIMPULSE"
}

build_and_copy_impulse ()
{
	# FIXME:  Can't figure out how to build this - fix it..?
	local CURRENT_DIR="$(pwd)"
	cd "$IMPULSE_DIR"
	make python-impulse
	local result=$?
	if [ $result -ne 0 ]; then
		echo "----------------------------------------------------"
		echo " /!\ Could not build impulse.so and/or libimpulse.so"
		echo " Do you have all the needed development headers installed?"
		echo "  Needed packages: libpulse-dev libfftw3-dev python-dev"
		echo " Note: G35_USB will still work, you just can't use the audio"
		echo " processing functionality.  To ignore this, create a file"
		echo " named 'skip-audio' in the Bin/Debug directory"
		echo "----------------------------------------------------"
		echo " [press Enter to continue]"
		read
		return 1
	fi
	cd "$CURRENT_DIR"
	find "$IMPULSE_BUILD_DIR/" -path "*/python-impulse/*" -type f -name '*.so' -exec cp '{}' "$TARGET_DIR/" ';'
}

echo " * Updating Python audio bridge..."
cp --update "$SOURCE_DIR/$FILES_PYTHON_BRIDGE" "$TARGET_DIR/$FILES_PYTHON_BRIDGE"

if [ ! -f "$FILES_SKIP_AUDIO" ] && [ ! -f "$TARGET_DIR/$FILES_IMPULSE_LIBIMPULSE" ] || [ ! -f "$TARGET_DIR/$FILES_IMPULSE_IMPULSE" ]; then
	echo " * Setting up Impulse library..."
	copy_prebuilt_impulse
	#build_and_copy_impulse
fi
