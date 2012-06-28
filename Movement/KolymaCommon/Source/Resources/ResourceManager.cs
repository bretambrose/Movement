/*

	ResourceManager.cs

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
using System.Resources;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CRShared
{
	public class CResourceManager
	{
		// construction
		private CResourceManager() 
		{
			foreach ( var res_type in Enum.GetValues( typeof( EResourceType ) ) )
			{
				EResourceType resource_type = (EResourceType) res_type;

				if ( resource_type != EResourceType.Invalid )
				{
					string entry_name = resource_type.ToString();
					m_ResourceNamePatterns.Add( resource_type, new Regex( @"^" + entry_name + @"_(.+)" ) );
				}
			}
		}

		static CResourceManager() {}

		// Methods
		// Public interface
		public void Initialize_Assembly_Resources< T >( string resource_prefix, Assembly resource_assembly, EResourceType resource_type ) where T : IConvertible
		{
			Regex resource_matcher = null;
			resource_matcher = m_ResourceNamePatterns[ resource_type ];

			ResourceManager rm = new ResourceManager( resource_prefix, resource_assembly );
			ResourceSet rs = rm.GetResourceSet( CultureInfo.CurrentUICulture, true, true );
			foreach ( DictionaryEntry entry in rs )
			{
				string key_name = entry.Key as string;

				// text substitutions to restore control characters like newline, tab
				string key_value = entry.Value as string;
				key_value = key_value.Replace( "\\n", "\n" );
				key_value = key_value.Replace( "\\r", "\r" );
				key_value = key_value.Replace( "\\t", "\t" );

				Create_And_Add_Resource< T >( resource_matcher, key_name, key_value );
			}
		}

		public string Get_Text< T >( T text_id ) where T : IConvertible
		{
			Type text_enum_type = typeof( T );

			Dictionary< uint, object > resource_dictionary = null;
			if ( !m_Resources.TryGetValue( text_enum_type, out resource_dictionary ) )
			{
				return null;
			}

			object resource_value = null;
			if ( !resource_dictionary.TryGetValue( text_id.ToUInt32( CultureInfo.CurrentCulture ), out resource_value ) )
			{
				return null;
			}

			return resource_value as string;
		}

		// Private interface
		private void Create_And_Add_Resource< T >( Regex resource_type_matcher, string resource_name, object resource_value ) where T : IConvertible
		{
			uint resource_index;
			
			Match m = resource_type_matcher.Match( resource_name );
			if ( !m.Success )
			{
				return;
			}
			
			Type resource_key_type = typeof( T );

			// convert cast exceptions to generic app exception
			try
			{		
				T ri = (T) Enum.Parse( resource_key_type, m.Groups[ 1 ].Value, true );
				resource_index = ri.ToUInt32( CultureInfo.CurrentCulture );
			}
			catch ( OverflowException )
			{
				throw new CApplicationException( string.Format( "Overflow converting value ( {0} ) to {1}", resource_name, resource_key_type ) );
			}
			catch ( ArgumentException )
			{
				throw new CApplicationException( string.Format( "Unable to cast value ( {0} ) as {1}", resource_name, resource_key_type ) );
			}
			catch ( Exception e )
			{
				throw new CApplicationException( "Unknown exception in enum parsing: " + e.ToString() );
			}

			Dictionary< uint, object > resource_dictionary = null;
			if ( !m_Resources.TryGetValue( resource_key_type, out resource_dictionary ) )
			{
				resource_dictionary = new Dictionary< uint, object >();
				m_Resources.Add( resource_key_type, resource_dictionary );
			}

			if ( resource_dictionary.ContainsKey( resource_index ) )
			{
				throw new CApplicationException( string.Format( "Duplicate resource key: {0} in resource enum {1}", resource_name, resource_key_type ) );
			}

			resource_dictionary.Add( resource_index, resource_value );
		}

		// properties
		public static CResourceManager Instance { get { return m_Instance; } }

		// Fields
		static private CResourceManager m_Instance = new CResourceManager();

		private Dictionary< Type, Dictionary< uint, object > > m_Resources = new Dictionary< Type, Dictionary< uint, object > >();
		private Dictionary< EResourceType, Regex > m_ResourceNamePatterns = new Dictionary< EResourceType, Regex >();

	}
}
