/*

	SlashCommandManager.cs

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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace NIPCommon
{
	public class CSlashCommandParser
	{
		// Construction
		private CSlashCommandParser() {}
		
		static CSlashCommandParser() {}
		
		// Methods
		// Public interface
		public void Initialize_Assembly_Commands( Assembly commands_assembly )
		{
			foreach ( var type in commands_assembly.GetTypes() )
			{
				SlashCommandAttribute command_attr = Attribute.GetCustomAttribute( type, typeof( SlashCommandAttribute ) ) as SlashCommandAttribute;
				if ( command_attr == null )
				{
					continue;
				}
				
				string command_name_text_id_name = CSlashCommandTextBuilder.Build_Slash_Command_Text_Key( command_attr.TextIDSuffix );
				string command_shortcut_text_id_name = CSlashCommandTextBuilder.Build_Slash_Command_Shortcut_Text_Key( command_attr.TextIDSuffix );

				string command_name = CCommonResource.Get_Text( command_name_text_id_name );
				if ( command_name == null )
				{
					command_name = command_attr.TextIDSuffix;
				}

				string upper_command_name = command_name.ToUpper();
				string command_shortcut = CCommonResource.Get_Text( command_shortcut_text_id_name ).ToUpper();
							
				if ( upper_command_name.Length == 0 )
				{
					throw new CSlashCommandException( string.Format( "No command name specified for slash command class \"{0}\"", type.Name ) );
				}
				
				int numeric_name;
				if ( Int32.TryParse( upper_command_name, out numeric_name ) )
				{
					throw new CSlashCommandException( string.Format( "Numeric commands are not allowed: \"{0}\"", type.Name ) );
				}

				if ( upper_command_name.Equals( command_shortcut ) )
				{
					throw new CSlashCommandException( string.Format( "Command name and shortcut are the same (\"{0}\") for class \"{1}\"", command_name, type.Name ) );
				}
				
				Type existing_command_type = null;
				
				if ( Command_Exists( upper_command_name ) )
				{
					m_CommandsByName.TryGetValue( upper_command_name, out existing_command_type );
					throw new CSlashCommandException( string.Format( "Command name \"{0}\" for class \"{1}\" already exists as a command name for class \"{2}\"", command_name, type.Name, existing_command_type.Name ) );
				}
				
				if ( Shortcut_Exists( upper_command_name ) )
				{
					m_CommandsByShortcut.TryGetValue( upper_command_name, out existing_command_type );
					throw new CSlashCommandException( string.Format( "Command name \"{0}\" for class \"{1}\" already exists as a shortcut for class \"{2}\"", command_name, type.Name, existing_command_type.Name ) );
				}

				if ( command_shortcut.Length > 0 )
				{
					if ( Int32.TryParse( command_shortcut, out numeric_name ) )
					{
						throw new CSlashCommandException( string.Format( "Numeric shortcuts are not allowed: \"{0}\"", type.Name ) );
					}

					if ( Command_Exists( command_shortcut ) )
					{
						m_CommandsByName.TryGetValue( command_shortcut, out existing_command_type );
						throw new CSlashCommandException( string.Format( "Command shortcut \"{0}\" for class \"{1}\" already exists as a command name for class \"{2}\"", command_shortcut, type.Name, existing_command_type.Name ) );
					}
									
					if ( Shortcut_Exists( command_shortcut ) )
					{
						m_CommandsByShortcut.TryGetValue( command_shortcut, out existing_command_type );
						throw new CSlashCommandException( string.Format( "Command shortcut \"{0}\" for class \"{1}\" already exists as a shortcut for class \"{2}\"", command_shortcut, type.Name, existing_command_type.Name ) );
					}
				}

				CSlashCommandInfo command_info = new CSlashCommandInfo();
				if ( !command_info.Initialize( type, command_name, command_attr.TextIDSuffix ) )
				{
					throw new CSlashCommandException( string.Format( "Unable to initialize command info structure for slash command \"{0}\"", type.Name ) );
				}
				
				m_CommandsByName.Add( upper_command_name, type );
				m_CommandInfos.Add( type, command_info );				
				if ( command_shortcut.Length > 0 )
				{
					m_CommandsByShortcut.Add( command_shortcut, type );
				}

				// Register the command for help purposes
				CSlashCommand command_instance = Activator.CreateInstance( type ) as CSlashCommand;
				string command_group_name = command_attr.CommandGroupName;
				if ( command_group_name != null )
				{
					CSlashCommandGroupInfo command_group_info = null;
					m_CommandGroupInfos.TryGetValue( command_group_name, out command_group_info );
					if ( command_group_info == null )
					{
						string command_group_text_id = CSlashCommandTextBuilder.Build_Command_Group_Text_Key( command_group_name );
						string command_group_localized_name = CCommonResource.Get_Text( command_group_text_id );

						command_group_info = new CSlashCommandGroupInfo( command_group_name );
						m_CommandGroupInfos.Add( command_group_name, command_group_info );

						if ( m_GroupBuilder.Length != 0 )
						{
							m_GroupBuilder.Append( ", " );
						}

						m_GroupBuilder.Append( command_group_localized_name );
					}

					command_group_info.Add_Command( type );
				}
			}
		}

		public void Initialize_Groups()
		{
			if ( m_GeneralHelpString == null )
			{
				m_GeneralHelpString = String.Format( CCommonResource.Get_Text( ECommonTextID.Help ), m_GroupBuilder.ToString() );
			}

			foreach ( var group in m_CommandGroupInfos )
			{
				CSlashCommandGroupInfo group_info = group.Value;
				group_info.Initialize_Help_String( CCommonUtils.Build_String_List( group_info.Commands, n => m_CommandInfos[ n ].CommandName ) );
			}
		}

		public bool Try_Parse( CSlashCommandInstance command_request, out CSlashCommand command, out string error_string )
		{
			command = null;
			error_string = "";
			
			string command_name = command_request.Command;
			if ( command_name == null )
			{
				return false;
			}

			command_name = command_name.ToUpper();
			
			Type matched_type = Find_Matching_Type( command_name, ref error_string );
			if ( matched_type == null )
			{
				return false;
			}

			CSlashCommand command_instance = Activator.CreateInstance( matched_type ) as CSlashCommand;
			command_instance.On_Command_Name( command_name );
			
			if ( !Parse_Parameters( command_request, command_instance, ref error_string ) )
			{
				return false;
			}
			
			command = command_instance;
			return true;
		}

		public string Get_Help_String( string command_group_or_name )
		{
			if ( command_group_or_name != null && command_group_or_name.Length > 0 )
			{
				string upper_command_group_or_name = command_group_or_name.ToUpper();

				CSlashCommandGroupInfo command_group_info = null;
				if ( m_CommandGroupInfos.TryGetValue( upper_command_group_or_name, out command_group_info ) )
				{
					return command_group_info.HelpText;
				}

				// ok we tried to turn the string into a command group and failed, let's see if it's a command instead
				Type command_type = null;
				if ( m_CommandsByName.TryGetValue( upper_command_group_or_name, out command_type ) )
				{
					return m_CommandInfos[ command_type ].HelpText;
				}

				return CCommonResource.Get_Text( ECommonTextID.Help_Bad_Input, command_group_or_name, m_GeneralHelpString ); 
			}

			// No command group or name, list all command groups
			return m_GeneralHelpString;
		}
			
		// Private interface
		private bool Parse_Parameters( CSlashCommandInstance command_request, CSlashCommand command, ref string error_string )
		{
			CSlashCommandInfo command_info = Get_Command_Info( command.GetType() );
			if ( !command_request.Parse( command_info.CommandParser, command_info.RequiredParamCount, command_info.RequiredParamCount + command_info.OptionalParamCount ) )
			{
				error_string =	CCommonResource.Get_Text( ECommonTextID.Unable_To_Parse_Slash_Command );
				return false;
			}

			int current_parsed_param = 0;
			foreach ( var param_info in command_info.Params )
			{
				if ( current_parsed_param >= command_request.Count )
				{
					if ( current_parsed_param < command_info.RequiredParamCount )
					{
						error_string =	CCommonResource.Get_Text( ECommonTextID.Slash_Command_Missing_Parameter );
						return false;
					}

					break;
				}

				string param_string_value = command_request[ current_parsed_param ];
				PropertyInfo prop_info = param_info.ParamInfo;

				object param_value = Parse_Parameter( prop_info, param_string_value );			
				if ( param_value == null )
				{
					error_string = string.Format( CCommonResource.Get_Text( ECommonTextID.Unable_To_Parse_Parameter ),
															param_string_value, 
															prop_info.Name, 
															current_parsed_param, 
															prop_info.PropertyType.Name, 
															command.GetType().Name );
					return false;
				}

				prop_info.SetValue( command, param_value, null );
				current_parsed_param++;
			}
			
			return true;
		}
		
		private object Parse_Parameter( PropertyInfo prop, string prop_value )
		{
			Type property_type = prop.PropertyType;
			
			try
			{
				if ( property_type.IsEnum )
				{
					return Enum.Parse( property_type, prop_value, true );
				}
				if ( property_type == typeof( string ) )
				{
					return prop_value;
				}
				else if ( property_type == typeof( Int32 ) )
				{
					return Int32.Parse( prop_value );
				}
				else if ( property_type == typeof( UInt32 ) )
				{
					return UInt32.Parse( prop_value );
				}
				else if ( property_type == typeof( float ) )
				{
					return float.Parse( prop_value );
				}
				else if ( property_type == typeof( double ) )
				{
					return double.Parse( prop_value );
				}
				else if ( property_type == typeof( Int64 ) )
				{
					return Int64.Parse( prop_value );
				}
				else if ( property_type == typeof( UInt64 ) )
				{
					return UInt64.Parse( prop_value );
				}
				else if ( property_type == typeof( bool ) )
				{
					return Boolean.Parse( prop_value );
				}
			}
			catch
			{
				return null;
			}
			
			
			return null;
		}
		
		private Type Find_Matching_Type( string command_name, ref string error_string )
		{
			// search for an exact match in the shortcut list
			IEnumerable< Type > matching_shortcuts = m_CommandsByShortcut.Where( n => command_name.Equals( n.Key ) ).Select( n => n.Value );
			if ( matching_shortcuts.Count() == 1 )
			{
				return matching_shortcuts.First();
			}
			
			// search for a substring match of any command
			int command_length = command_name.Length;		
			IEnumerable< Type > matching_commands = m_CommandsByName.Where( n => n.Key.Length >= command_length && command_name.Equals( n.Key.Substring( 0, command_length ) ) )
																					  .Select( n => n.Value );

			int matches = matching_commands.Count();
			if ( matches == 1 )
			{
				return matching_commands.First();
			}
			else if ( matches == 0 )
			{
				// Maybe it's a numeric command
				int command_number;
				if ( Int32.TryParse( command_name, out command_number ) )
				{
					return m_CommandsByName.Where( n => n.Key == "#" ).Select( n => n.Value ).Single();
				}
				else
				{
					error_string = CCommonResource.Get_Text( ECommonTextID.Unknown_Command, command_name );
					return null;
				}
			}
			
			error_string = CCommonResource.Get_Text( ECommonTextID.Multiple_Commands_Matched, command_name );
			return null;		
		}
		
		private bool Command_Exists( string command_name )
		{
			return m_CommandsByName.ContainsKey( command_name );
		}
		
		private bool Shortcut_Exists( string shortcut_name )
		{
			return m_CommandsByShortcut.ContainsKey( shortcut_name );
		}
		
		public bool Has_Command( Type command_type )
		{
			return m_CommandInfos.ContainsKey( command_type );
		}

		private CSlashCommandInfo Get_Command_Info( Type command_type )
		{
			CSlashCommandInfo command_info = null;
			m_CommandInfos.TryGetValue( command_type, out command_info );

			return command_info;
		}

		private CSlashCommandGroupInfo Get_Command_Group_Info( string command_group_name )
		{
			CSlashCommandGroupInfo command_group_info = null;
			m_CommandGroupInfos.TryGetValue( command_group_name.ToUpper(), out command_group_info );

			return command_group_info;
		}

		// Properties
		public static CSlashCommandParser Instance { get { return m_Instance; } }
		
		// Fields
		private static CSlashCommandParser m_Instance = new CSlashCommandParser();
		
		private Dictionary< Type, CSlashCommandInfo > m_CommandInfos = new Dictionary< Type, CSlashCommandInfo >();
		private Dictionary< string, CSlashCommandGroupInfo > m_CommandGroupInfos = new Dictionary< string, CSlashCommandGroupInfo >();

		private Dictionary< string, Type > m_CommandsByName = new Dictionary< string, Type >();
		private Dictionary< string, Type > m_CommandsByShortcut = new Dictionary< string, Type >();

		private string m_GeneralHelpString = null;
		private StringBuilder m_GroupBuilder = new StringBuilder();
	}	
}
