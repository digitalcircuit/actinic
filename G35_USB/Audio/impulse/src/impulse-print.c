/*
 *
 *+  Copyright (c) 2009 Ian Halpern
 *@  http://impulse.ian-halpern.com
 *
 *   Modified by Shane Synan to print all numbers in a suitable format
 *   for G35_USB.
 *
 *   This file is part of Impulse.
 *
 *   Impulse is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Impulse is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with Impulse.  If not, see <http://www.gnu.org/licenses/>.
 */


#include "impulse.h"
#include <stdio.h>

int main( ) {

	int i = 0;
	im_start( );

	while ( 1 ) {
		// usleep( 1000000 / 30 );
		// Go without any delay - more CPU usage, but also more accurate
		double *array = im_getSnapshot( IM_FFT );
		printf( "audio_data:");
		for ( i = 0; i < 256; i++ )
			printf( "%.4f,", array[ i ] );
			// Only 4 digits of precision is fine for what we're doing
		printf( "\n" );
		fflush( stdout );
	}
	im_stop( );

	return 0;
}
