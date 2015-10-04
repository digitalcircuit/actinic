#!/usr/bin/python

#   Author:
#       Shane Synan <digitalcircuit36939@gmail.com>
#
#   Copyright (c) 2015
#
#	This program is free software: you can redistribute it and/or modify
#	it under the terms of the GNU General Public License as published by
#	the Free Software Foundation, either version 3 of the License, or
#	(at your option) any later version.
#
#	This program is distributed in the hope that it will be useful,
#	but WITHOUT ANY WARRANTY; without even the implied warranty of
#	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
#	GNU General Public License for more details.
#
#	You should have received a copy of the GNU General Public License
#	along with this program. If not, see http://www.gnu.org/licenses/

import time

import impulse
while True:
	print ("audio_data:" + str(impulse.getSnapshot( True )))
	time.sleep (0.0005)   # Change in timing:  0.01 -> 0.005 -> 0.0025
	# Before rewrite of VU volume queue (now multithreaded)
