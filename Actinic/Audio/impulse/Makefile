ARCH=`uname -m`
MAIN_BUILD_DIR=build
BUILD_DIR=$(MAIN_BUILD_DIR)/$(ARCH)
COPY_DEFAULTS=COPYING README.md

all: impulse test impulse-print impulse-spam post

post:
	if [ -f $(BUILD_DIR)/impulse/*.o ]; then rm $(BUILD_DIR)/impulse/*.o ; fi
	rmdir --ignore-fail-on-non-empty $(BUILD_DIR)/impulse/
	if [ -f $(BUILD_DIR)/test/*.o ]; then rm $(BUILD_DIR)/test/*.o ; fi
	rmdir --ignore-fail-on-non-empty $(BUILD_DIR)/test/
	if [ -f $(BUILD_DIR)/test-spam/*.o ]; then rm $(BUILD_DIR)/test-spam/*.o ; fi
	rmdir --ignore-fail-on-non-empty $(BUILD_DIR)/test-spam/
	if [ -f $(BUILD_DIR)/print/*.o ]; then rm $(BUILD_DIR)/print/*.o ; fi
	rmdir --ignore-fail-on-non-empty $(BUILD_DIR)/print/

init:
	mkdir -p $(BUILD_DIR)/impulse
	mkdir -p $(BUILD_DIR)/test
	mkdir -p $(BUILD_DIR)/test-spam
	mkdir -p $(BUILD_DIR)/print
	cp $(COPY_DEFAULTS) $(BUILD_DIR)/impulse

impulse: init module.o impulse.o
	gcc -pthread -shared -Wl,-O2 -Bsymbolic-functions\
		-L$(BUILD_DIR)/impulse/ $(BUILD_DIR)/impulse/module.o\
		$(BUILD_DIR)/impulse/impulse.o -o $(BUILD_DIR)/impulse/libimpulse.so\
		-lfftw3 -lpulse

test: impulse.o
	gcc -c src/test-impulse.c -o $(BUILD_DIR)/test/test-impulse.o
	gcc -pthread -L$(BUILD_DIR)/test/\
		$(BUILD_DIR)/impulse/impulse.o $(BUILD_DIR)/test/test-impulse.o\
		-o $(BUILD_DIR)/test/test-impulse -lm\
		-lfftw3 -lpulse

test-spam: impulse.o
	gcc -c src/test-impulse-spam.c -o $(BUILD_DIR)/test-spam/test-impulse-spam.o
	gcc -pthread -L$(BUILD_DIR)/test/\
		$(BUILD_DIR)/impulse/impulse.o $(BUILD_DIR)/test-spam/test-impulse-spam.o\
		-o $(BUILD_DIR)/test-spam/test-impulse-spam -lm\
		-lfftw3 -lpulse

impulse-lib: init impulse.o
	gcc -pthread -shared -Wl,-O2 -Bsymbolic-functions\
		$(BUILD_DIR)/impulse/impulse.o -o $(BUILD_DIR)/impulse/libimpulse.so\
		-lfftw3 -lpulse

impulse-print: impulse.o
	gcc -c src/impulse-print.c -o $(BUILD_DIR)/print/impulse-print.o
	gcc -pthread -L$(BUILD_DIR)/print/\
		$(BUILD_DIR)/impulse/impulse.o $(BUILD_DIR)/print/impulse-print.o\
		-o $(BUILD_DIR)/print/impulse-print -lm\
		-lfftw3 -lpulse

impulse.o:
	gcc -pthread -Wall -fPIC -c src/impulse.c -o $(BUILD_DIR)/impulse/impulse.o

module.o:
	gcc -pthread -fno-strict-aliasing -DNDEBUG -g -fwrapv -O2 -Wall\
		-Wstrict-prototypes -fPIC -I/usr/include/python2.7 \
		-c src/module.c -o $(BUILD_DIR)/impulse/module.o

clean:
	rm -rf $(MAIN_BUILD_DIR)
