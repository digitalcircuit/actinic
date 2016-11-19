//
//  IAnimationOneshot.cs
//
//  Author:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;

namespace Actinic.Animations
{
	public interface IAnimationOneshot
	{
		/// <summary>
		/// Gets a value indicating whether the animation has finished and may be cleaned up.
		/// </summary>
		/// <value><c>true</c> if animation has finished; otherwise, <c>false</c>.</value>
		bool AnimationFinished {
			get;
		}
	}
}

