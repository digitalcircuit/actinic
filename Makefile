PROJECT_DIR=Actinic
SOLUTION_NAME=Actinic.sln
MAIN_BUILD_DIR=$(PROJECT_DIR)/bin
MAIN_OBJ_DIR=$(PROJECT_DIR)/obj
TESTS_BUILD_DIR=$(PROJECT_DIR).Tests/bin
TESTS_OBJ_DIR=$(PROJECT_DIR).Tests/obj


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
	msbuild -m /p:Configuration=$(XBUILD_CONFIG) $(SOLUTION_NAME)

audio: init
	$(LIBIMPULSE_BUILD) $(XBUILD_CONFIG)

test: lights
	mono packages/NUnit.ConsoleRunner.*/tools/nunit*console.exe ./$(PROJECT_DIR).Tests/bin/$(XBUILD_CONFIG)/$(PROJECT_DIR).Tests.dll --noresult

clean: clean-build clean-obj

clean-build:
	if [ -d $(MAIN_BUILD_DIR) ]; then rm -r $(MAIN_BUILD_DIR) ; fi
	if [ -d $(TESTS_BUILD_DIR) ]; then rm -r $(TESTS_BUILD_DIR) ; fi

clean-obj:
	if [ -d $(MAIN_OBJ_DIR) ]; then rm -r $(MAIN_OBJ_DIR) ; fi
	if [ -d $(TESTS_OBJ_DIR) ]; then rm -r $(TESTS_OBJ_DIR) ; fi
