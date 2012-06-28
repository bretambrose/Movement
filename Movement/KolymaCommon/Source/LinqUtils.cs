/*

	LinqUtils.cs

	(c) Copyright 2010-2011, Bret Ambrose (mailto:bretambrose@gmail.com).

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace NKolymaCommon
{
	public static class CCommonUtils
	{
		public static string Build_String_List< T >( IEnumerable< T > t_list ) 
		{
			StringBuilder list_builder = new StringBuilder();
			bool is_first = true;

			foreach ( var element in t_list )
			{
				if ( is_first )
				{
					is_first = false;
				}
				else
				{
					list_builder.Append( ", " );
				}

				list_builder.Append( element.ToString() );
			}

			return list_builder.ToString();
		}

		public delegate string DListElementTransform< T >( T list_element );

		public static string Build_String_List< T >( IEnumerable< T > t_list, DListElementTransform< T > transformer )
		{
			return Build_String_List( t_list, transformer, null );
		}

		public static string Build_String_List< T >( IEnumerable< T > t_list, DListElementTransform< T > transformer, DListElementTransform< T > annotater )
		{
			StringBuilder list_builder = new StringBuilder();
			bool is_first = true;

			foreach ( var element in t_list )
			{
				if ( is_first )
				{
					is_first = false;
				}
				else
				{
					list_builder.Append( ", " );
				}

				if ( transformer == null )
				{
					list_builder.Append( element.ToString() );
				}
				else
				{
					list_builder.Append( transformer( element ) );
				}

				if ( annotater != null )
				{
					string annotation = annotater( element );
					if ( annotation != null )
					{
						list_builder.Append( annotation );
					}
				}
			}

			return list_builder.ToString();
		}

		public static void Apply< T >( this IEnumerable< T > collection, Action< T > action )
		{
			foreach ( T element in collection )
			{
				action( element );
			}
		}

		public static void ShallowCopy< T >( this IEnumerable< T > collection, ICollection< T > destination )
		{
			collection.Apply( n => destination.Add( n ) );
		}

		public static void ShallowCopy< T1, T2 >( this IEnumerable< KeyValuePair< T1, T2 > > collection, Dictionary< T1, T2 > destination )
		{
			collection.Apply( n => destination.Add( n.Key, n.Value ) );
		}

		public static List< T > Duplicate_As_List< T >( this IEnumerable< T > collection )
		{
			List< T > new_list = new List< T >();
			collection.Apply( n => new_list.Add( n ) );
			return new_list;
		}

		public static Dictionary< T1, T2 > Duplicate< T1, T2 >( this Dictionary< T1, T2 > collection )
		{
			Dictionary< T1, T2 > new_dict = new Dictionary< T1, T2 >();
			collection.Apply( n => new_dict.Add( n.Key, n.Value ) );
			return new_dict;
		}
	}
}
