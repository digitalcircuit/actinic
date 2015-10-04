#!/usr/bin/env python
#
#+   Copyright (c) 2009 Ian Halpern
#@   http://impulse.ian-halpern.com
#
#    This file is part of Impulse.
#
#    Impulse is free software: you can redistribute it and/or modify
#    it under the terms of the GNU General Public License as published by
#    the Free Software Foundation, either version 3 of the License, or
#    (at your option) any later version.
#
#    Impulse is distributed in the hope that it will be useful,
#    but WITHOUT ANY WARRANTY; without even the implied warranty of
#    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#    GNU General Public License for more details.
#
#    You should have received a copy of the GNU General Public License
#    along with Impulse.  If not, see <http://www.gnu.org/licenses/>.


import screenlets, sys, gobject, os, gtk
from screenlets.options import IntOption

class ImpulseScreenlet ( screenlets.Screenlet) :
	"""A PulseAudio graphical spectrum analyzer."""

	# default meta-info for Screenlets (should be removed and put into metainfo)
	__name__	= 'ImpulseScreenlet'
	__version__	= '0.5'
	__author__	= 'Ian Halpern'
	__desc__	= __doc__	# set description to docstring of class

	theme_module = None

	audio_source_index = 0

	def __init__ ( self, **keyword_args ):
		#call super (width/height MUST match the size of graphics in the theme)
		screenlets.Screenlet.__init__(self, width=200, height=200,
			uses_theme=True, **keyword_args)

		self.add_options_group( 'Impulse', 'Change the look of Impulse.\
			\n\nIf you just changed the theme\
			\nplease close and reopen this window to edit the theme.' )

		self.add_options_group( 'Audio Source', 'Change the Audio Source of Impulse.' )
		self.addAudioSourceOptions()

		os.chdir( self.get_screenlet_dir( ) )
		sys.path.append( "themes" )

		import impulse

		sys.modules[ __name__ ].impulse = impulse
		self.setAudioSource()

		# set theme
		self.theme_name = "default"

		self.timer = gobject.timeout_add( 33, self.update )

		self.add_menuitem( 'audio_source', 'Change Audio Source', callback=self.openAudioSourceDialog )

	def openAudioSourceDialog( self, widget, id ):
		asd = AudioSourceDialog()
		asd.run()
		asd.destroy()

	def update (self):

		self.redraw_canvas()
		return True # keep running this event

	def on_init ( self ):
		self.add_default_menuitems( )

	def on_load_theme (self):
		"""Called when the theme is reloaded (after loading, before redraw)."""

		if not self.theme_module or self.theme_name != self.theme_module.__name__:

			for o in list( self.__options_groups__[ "Impulse" ][ 'options' ] ):
				self.__options__.remove( o )
				self.__options_groups__[ "Impulse" ][ 'options' ].remove( o )

			self.theme_module = __import__( self.theme_name )
			self.theme_module.load_theme( self )

	def addAudioSourceOptions( self ):
		self.add_option( IntOption('Audio Source','audio_source_index', self.audio_source_index,
			'Audio Source Index List', 'List of audio source indices', min=0, max=10 ), callback=self.setAudioSource )

	def setAudioSource( self, *args, **kwargs ):
		impulse.setSourceIndex( self.audio_source_index )

	def resize ( self, w, h ):
		self.width = w
		self.height = h
		self.window.resize( int( w * self.scale ), int( h * self.scale ) )

	def on_draw ( self, cr ):
		"""In here we draw"""

		cr.scale( self.scale, self.scale )

		if not self.theme_module: return

		fft = False

		if hasattr( self.theme_module, "fft" ) and self.theme_module.fft:
			fft = True

		audio_sample_array = impulse.getSnapshot( fft )

		self.theme_module.on_draw( audio_sample_array, cr, self )

	def on_draw_shape ( self, ctx ):
		self.on_draw(ctx)

	def on_after_set_atribute(self,name, value):
		"""Called after setting screenlet atributes"""
		if self.theme_module and hasattr( self.theme_module, name ):
			self.theme_module.on_after_set_attribute( self.theme_module, name, value, self )

class AudioSourceDialog( gtk.Dialog ):
	def __init__( self ):
		super( AudioSourceDialog, self ).__init__( 'AudioSource',
			flags=gtk.DIALOG_DESTROY_WITH_PARENT | gtk.DIALOG_NO_SEPARATOR,
 			buttons = ( gtk.STOCK_CLOSE, gtk.RESPONSE_OK ) )

		# set size
		self.resize(300, 300)
		self.set_keep_above(True)

# If the program is run directly or passed as an argument to the python
# interpreter then create a Screenlet instance and show it
if __name__ == "__main__":

	try:
		import ctypes
		libc = ctypes.CDLL('libc.so.6')
		libc.prctl(15, os.path.split( sys.argv[ 0 ] )[ 1 ], 0, 0, 0)
	except Exception:
		pass

	# create new session
	import screenlets.session
	screenlets.session.create_session( ImpulseScreenlet )

