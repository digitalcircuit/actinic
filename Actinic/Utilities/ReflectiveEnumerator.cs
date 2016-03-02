//
//  ReflectiveEnumerator.cs
//
//  Author:
//       Repo Man <https://stackoverflow.com/users/140126/repo-man>
//       (From <https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class>)
//  Edited by:
//       Shane Synan <digitalcircuit36939@gmail.com>
//
//  Copyright (c) 2015 - 2016
//
//  (I'm not sure what license the original code is under, but in good faith,
//   I'll assume it is GPL-compatible)
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Actinic
{
	public static class ReflectiveEnumerator
	{
		static ReflectiveEnumerator() { }

		/// <summary>
		/// Gets an enumerator to and instantiates all objects of a certain type.
		/// </summary>
		/// <returns>The enumerable of all subclasses matching given type.</returns>
		/// <param name="constructorArgs">Constructor arguments.</param>
		/// <typeparam name="T">The type of classes to search.</typeparam>
		public static IEnumerable<T> GetEnumerableOfType<T>(params object[] constructorArgs) where T : class, IComparable<T>
		{
			List<T> objects = new List<T>();
			foreach (Type type in
			         Assembly.GetAssembly(typeof(T)).GetTypes()
			         .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
			{
				objects.Add((T)Activator.CreateInstance(type, constructorArgs));
			}
			objects.Sort();
			return objects;
		}

		/// <summary>
		/// Gets an enumerator to and instantiates all objects of a certain type, filtering by another type.
		/// </summary>
		/// <returns>The enumerable of all subclasses matching given type.</returns>
		/// <param name="InclusiveSearch">If set to <c>true</c> perform an inclusive search, otherwise exclusive.</param>
		/// <param name="constructorArgs">Constructor arguments.</param>
		/// <typeparam name="T">The type of classes to search.</typeparam>
		public static IEnumerable<T> GetFilteredEnumerableOfType<T, U>(bool InclusiveSearch, params object[] constructorArgs)
			where T : class, IComparable<T>
			where U : class
		{
			List<T> objects = new List<T>();
			foreach (Type type in
			         Assembly.GetAssembly(typeof(T)).GetTypes()
			         .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)) &&
			       ((InclusiveSearch && typeof (U).IsAssignableFrom (myType)) || (!InclusiveSearch && !(typeof (U).IsAssignableFrom (myType)))) ))
			{
				objects.Add((T)Activator.CreateInstance(type, constructorArgs));
			}
			objects.Sort();
			return objects;
		}

	}
}

