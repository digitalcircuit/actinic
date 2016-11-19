Actinic [![Build Status](https://travis-ci.org/digitalcircuit/actinic.svg?branch=master)](https://travis-ci.org/digitalcircuit/actinic)
===============

Actinic interfaces with strands of LED lights to control them in various flashy or useful ways according to music, time of day, and multiple types of animations.  Currently supports an [Arduino-based LED controller firmware][arduino-firmware], but support can be easily added for other systems.

## Building

Tools needed:
* MonoDevelop (preferably 4.0 or higher)
* Mono runtime, including System.Drawing and System.Core
* Development headers for [FFTW library version 3](https://fftw.org) and [PulseAudio](https://wiki.freedesktop.org/www/Software/PulseAudio/)
* Make, a C++ compiler, and friends

For an Ubuntu system, apt-get install the following packages
```sh
libmono-system-core4.0-ci		# Mono runtime, System.Linq interface
libmono-system-drawing4.0-ci	# Bitmap parsing
make libpulse-dev libfftw3-dev	# Tools to compile Impulse audio processing library
```

The project will automatically attempt to build the Impulse library upon compiling the solution in MonoDevelop.  Should anything go wrong, a file named `README-impulse-failed-to-build` will be put in the `bin/Debug` build directory.

To manually compile, run [`prepare-audio-processing.sh`](https://github.com/digitalcircuit/actinic/blob/master/Actinic/Audio/prepare-audio-processing.sh) included in the `Actinic/Audio` folder.

*If using the [Arduino LED controller][arduino-firmware], don't forget to change permissions on the USB device to allow access without root.*

## Usage

* Connect and power on your lighting controller and lights (*for the [Arduino LED controller][arduino-firmware], plug it into power, then into a USB port*)
* In the output directory, run `mono Actinic.exe`
* *To be done - expand this list?*

To list commands, type `help`.  For help on a particular command, type it without providing any parameters, e.g. `vu run`.

## Credits

* [Impulse Python module](https://launchpad.net/impulse.bzr) for audio processing; some information on [GNOME Look](http://gnome-look.org/content/show.php/Impulse+-+PulseAudio+visualizer?content=99383)
* [LinearColorInterpolator.cs](https://stackoverflow.com/questions/2307726/how-to-calculate-color-based-on-a-range-of-values-in-c) by [Mark Byers](https://stackoverflow.com/users/61974/mark-byers)
* [ReflectiveEnumerator.cs](https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class) by [Repo Man](https://stackoverflow.com/users/140126/repo-man)
* *If you're missing, let me know, and I'll fix it as soon as I can!*

[arduino-firmware]: https://github.com/digitalcircuit/ActinicArduino_Controller
