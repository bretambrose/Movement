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

namespace NIPCommon
{
	public enum EResourceType
	{
		Invalid = -1,
		Text,
		Image
	}

	[AttributeUsage( AttributeTargets.Enum )]
	public class ResourceAttribute : Attribute
	{
		public ResourceAttribute( EResourceType resource_type )
		{
			ResourceType = resource_type;
		}

		public EResourceType ResourceType { get; set; }
	}

	public class CResourceException : Exception
	{
		public CResourceException( string message ) :
			base( message )
		{		
		}
	}

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
					m_ResourceTypePrefixes.Add( resource_type, entry_name.ToUpper() );
				}
			}
		}

		static CResourceManager() {}

		// Methods
		// Public interface
		public void Initialize_Assembly_Resources( string resource_prefix, Assembly resource_assembly )
		{
			ResourceManager rm = new ResourceManager( resource_prefix, resource_assembly );
			ResourceSet rs = rm.GetResourceSet( CultureInfo.CurrentUICulture, true, true );
			foreach ( DictionaryEntry entry in rs )
			{
				string key_name = entry.Key as string;
				EResourceType resource_type = Get_Resource_Type_From_Name( key_name );

				switch ( resource_type )
				{
					case EResourceType.Text:
					{
						// text substitutions to restore control characters like newline, tab
						string key_value = entry.Value as string;
						key_value = key_value.Replace( "\\n", "\n" );
						key_value = key_value.Replace( "\\r", "\r" );
						key_value = key_value.Replace( "\\t", "\t" );

						Create_And_Add_Resource( key_name, key_value );
						break;
					}

					default:
						throw new CResourceException( string.Format( "Unknown resource type with key: {0}", key_name ) );
				}
			}
		}

		public void Verify_Resource_Enums( Assembly assembly ) 
		{
			foreach ( var type in assembly.GetTypes() )
			{
				if ( !type.IsEnum )
				{
					continue;
				}

				object[] attribute_list = type.GetCustomAttributes( typeof ( ResourceAttribute ), true );
				if ( attribute_list.Length == 0 )
				{
					return;
				}

				ResourceAttribute resource_attribute = attribute_list[ 0 ] as ResourceAttribute;
				Verify_Resource_Enum( type, resource_attribute.ResourceType );
			}
		}

		public string Get_Text< T >( T text_id ) where T : IConvertible
		{
			string resource_key = Build_Resource_Key( EResourceType.Text, text_id );

			object resource_value = null;
			if ( !m_Resources.TryGetValue( resource_key, out resource_value ) )
			{
				return null;
			}

			return resource_value as string;
		}

		public string Get_Text( string text_key )
		{
			string resource_key = Build_Resource_Key( EResourceType.Text, text_key );

			object resource_value = null;
			if ( !m_Resources.TryGetValue( resource_key, out resource_value ) )
			{
				return null;
			}

			return resource_value as string;
		}

		// Private interface
		private void Create_And_Add_Resource( string resource_name, object resource_value )
		{			
			string resource_key = resource_name.ToUpper();
			
			if ( m_Resources.ContainsKey( resource_key ) )
			{
				throw new CResourceException( string.Format( "Duplicate resource key: {0}", resource_key ) );
			}

			m_Resources.Add( resource_key, resource_value );
		}

		private void Verify_Resource_Enum( Type enum_type, EResourceType resource_type ) 
		{
			foreach ( var enum_entry in Enum.GetNames( enum_type ) )
			{
				string upper_entry = enum_entry.ToUpper();
				if ( upper_entry == "INVALID" || upper_entry == "NONE" )
				{
					continue;
				}

				string resource_key = Build_Resource_Key( resource_type, enum_entry );
				if ( !m_Resources.ContainsKey( resource_key ) )
				{
					throw new CResourceException( string.Format( "{0} resource enum {1} contains an entry with no associated resource: {2}", 
																				Enum.GetName( typeof( EResourceType ), resource_type ), 
																				enum_type.Name, 
																				enum_entry ) );
				}
			}
		}

		private string Build_Resource_Key( EResourceType resource_type, string base_key_name )
		{
			return string.Format( "{0}_{1}", m_ResourceTypePrefixes[ resource_type ], base_key_name.ToUpper() );
		}

		private string Build_Resource_Key< T >( EResourceType resource_type, T key_entry ) where T : IConvertible
		{
			return Build_Resource_Key( resource_type, Enum.GetName( typeof( T ), key_entry ) );
		}

		private EResourceType Get_Resource_Type_From_Name( string resource_name )
		{
			foreach ( var prefix_pair in m_ResourceTypePrefixes )
			{
				string prefix = prefix_pair.Value;
				if ( resource_name.Substring( 0, prefix.Length ).Equals( prefix, StringComparison.CurrentCultureIgnoreCase ) )
				{
					return prefix_pair.Key;
				}
			}

			return EResourceType.Invalid;
		}

		// properties
		public static CResourceManager Instance { get { return m_Instance; } }

		// Fields
		static private CResourceManager m_Instance = new CResourceManager();

		private Dictionary< string, object > m_Resources = new Dictionary< string, object >();
		private Dictionary< EResourceType, string > m_ResourceTypePrefixes = new Dictionary< EResourceType, string >();

	}
}
