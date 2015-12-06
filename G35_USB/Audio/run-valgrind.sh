#!/bin/bash

_LOCAL_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Get directory of file

SOURCE_DIR="$_LOCAL_DIR"

IMPULSE_DIR="$SOURCE_DIR/impulse"

CURRENT_DIR="$(pwd)"
cd "$IMPULSE_DIR"
make clean init test-spam post
cd "$SOURCE_DIR"
valgrind --tool=memcheck --leak-check=full --show-leak-kinds=all "./impulse/build/$(uname -m)/test-spam/test-impulse-spam"
cd "$CURRENT_DIR"
