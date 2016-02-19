PROJECT_DIR=G35_USB
SOLUTION_NAME=G35_USB.sln
MAIN_BUILD_DIR=$(PROJECT_DIR)/bin
MAIN_OBJ_DIR=$(PROJECT_DIR)/obj

LIBIMPULSE_BUILD=$(PROJECT_DIR)/Audio/prepare-audio-processing.sh

DEBUG ?= 0
ifeq ($(DEBUG), 1)
    BUILD_DIR=$(MAIN_BUILD_DIR)/Debug
    XBUILD_CONFIG=Debug
else
    BUILD_DIR=$(MAIN_BUILD_DIR)/Release
    XBUILD_CONFIG=Release
endif

all: lights audio clean-obj

init:
	mkdir -p $(BUILD_DIR)

lights: init
	xbuild /p:Configuration=$(XBUILD_CONFIG) $(SOLUTION_NAME)

audio: init
	$(LIBIMPULSE_BUILD) $(XBUILD_CONFIG)

clean: clean-build clean-obj

clean-build:
	if [ -d $(MAIN_BUILD_DIR) ]; then rm -r $(MAIN_BUILD_DIR) ; fi

clean-obj:
	if [ -d $(MAIN_OBJ_DIR) ]; then rm -r $(MAIN_OBJ_DIR) ; fi
