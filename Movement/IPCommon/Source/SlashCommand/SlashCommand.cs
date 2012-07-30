/*

	SlashCommand.cs

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
using System.Text.RegularExpressions;
using System.Text;

namespace NIPCommon
{
	public class CSlashCommandException : Exception
	{
		public CSlashCommandException( string exception_info ) :
			base( exception_info )
		{
		}
	}

	[AttributeUsage( AttributeTargets.Enum )]
	public class SlashCommandGroupAttribute : Attribute
	{
		public SlashCommandGroupAttribute()
		{
		}
	}

	[AttributeUsage( AttributeTargets.Class )]
	public class SlashCommandAttribute : Attribute 
	{
		// Construction
		public SlashCommandAttribute( string text_id_suffix, string command_group_name ) :
			base()
		{
			TextIDSuffix = text_id_suffix;
			CommandGroupName = command_group_name.Length > 0 ? command_group_name.ToUpper() : null;
			AllowSymbols = false;
		}
		
		// Properties
		public string TextIDSuffix { get; private set; }
		public string CommandGroupName { get; private set; }
		public bool AllowSymbols { get; set; }
	}
	
	public enum ESlashCommandParameterType
	{
		Required,
		Optional,
		Ignore
	}
	
	[AttributeUsage( AttributeTargets.Property )]
	public class SlashParamAttribute : Attribute
	{
		// Construction
		public SlashParamAttribute( ESlashCommandParameterType param_type ) :
			base()
		{
			ParamType = param_type;
			ConsumeAll = false;
			AllowQuoted = false;
		}
		
		// Properties
		public ESlashCommandParameterType ParamType { get; private set; }
		public bool ConsumeAll { get; set; }
		public bool AllowQuoted { get; set; }
	}
	
	static public class CSlashCommandTextBuilder
	{
		static public string Build_Slash_Command_Group_Help_Text_Key( string command_group_name )
		{
			return "Help_CommandGroup_" + command_group_name;
		}

		static public string Build_Slash_Command_Param_Help_Text_Key( string command_name, string param_name )
		{
			return "Command_" + command_name + "_Param_" + param_name;
		}

		static public string Build_Slash_Command_Help_Text_Key( string command_name )
		{
			return "Help_Command_" + command_name;
		}

		static public string Build_Slash_Command_Text_Key( string command_name )
		{
			return "Command_Name_" + command_name;
		}

		static public string Build_Slash_Command_Shortcut_Text_Key( string command_name )
		{
			return "Command_Shortcut_" + command_name;
		}

		static public string Build_Command_Group_Text_Key( string command_group_name )
		{
			return 	"CommandGroup_" + command_group_name;
		}
	}
		
	public class CSlashCommandGroupInfo
	{
		// constructors
		public CSlashCommandGroupInfo( string command_group )
		{
			CommandGroupName = command_group;
			Commands = new List< Type >();
		}

		// Methods
		// Public interface
		public void Add_Command( Type command_type )
		{
			Commands.Add( command_type );
		}

		public void Initialize_Help_String( string command_list_string )
		{
			string group_help = CCommonResource.Get_Text( CSlashCommandTextBuilder.Build_Slash_Command_Group_Help_Text_Key( CommandGroupName ) );
			string command_list = CCommonResource.Get_Text( ECommonTextID.Help_CommandGroup_Commands );
						
			HelpText = group_help + String.Format( command_list, command_list_string );		
		}

		// properties
		public string CommandGroupName { get; private set; }
		public List< Type > Commands { get; private set; }
		public string HelpText { get; private set; }
	}

	public class CSlashCommandParamInfo
	{
		// Construction
		public CSlashCommandParamInfo( PropertyInfo param_info ) 
		{
			ParamInfo = param_info;
		}

		// Methods
		// Public interface
		public void Initialize_Text( string command_text_name ) 
		{
			ParamNameText = CCommonResource.Get_Text( CSlashCommandTextBuilder.Build_Slash_Command_Param_Help_Text_Key( command_text_name, ParamInfo.Name ) );
			if ( ParamNameText == null )
			{
				ParamNameText = CCommonResource.Get_Text( ECommonTextID.Undocumented_Parameter );
			}
		}

		// Properties
		public PropertyInfo ParamInfo { get; private set; }
		public string ParamNameText { get; private set; }
	}

	public class CSlashCommandInfo
	{
		// Construction
		public CSlashCommandInfo()
		{
			CommandType = null;
			CommandParser = null;
			OptionalParamCount = 0;
			CommandType = null;
		}

		// Methods
		// Public interface
		public bool Initialize( Type command_type, string command_name, string command_text_id_suffix )
		{
			bool remaining_consumed = false;

			StringBuilder parse_string = new StringBuilder();
			parse_string.Append( @"^/(?'command'\w+)" );
			int current_param = 1;

			StringBuilder usage_string_builder = new StringBuilder();
			usage_string_builder.Append( command_name );

			SlashCommandAttribute command_attr = Attribute.GetCustomAttribute( command_type, typeof( SlashCommandAttribute ) ) as SlashCommandAttribute;
			string valid_param_characters = null;
			if ( command_attr.AllowSymbols )
			{
				valid_param_characters = @"(?:\w|\p{S}|#)";
			}
			else
			{
				valid_param_characters = @"\w";
			}

			foreach ( var prop in command_type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
			{
				SlashParamAttribute param_attr = Attribute.GetCustomAttribute( prop, typeof( SlashParamAttribute ) ) as SlashParamAttribute;
				if ( param_attr == null )
				{
					continue;
				}
			
				if ( remaining_consumed )
				{
					throw new CSlashCommandException( "ERROR: Cannot have additional params after a consume remaining param, in command: " + command_type.Name );
				}
					
				string param_group = "param" + current_param.ToString();
				ESlashCommandParameterType param_type = param_attr.ParamType;
				if ( param_type == ESlashCommandParameterType.Ignore )
				{
					continue;
				}

				bool is_required = param_type == ESlashCommandParameterType.Required;
				bool consume_remaining = param_attr.ConsumeAll;
				if ( consume_remaining )
				{
					remaining_consumed = true;
				}

				CSlashCommandParamInfo param_info = new CSlashCommandParamInfo( prop );
				param_info.Initialize_Text( command_text_id_suffix );

				if ( is_required )
				{
					if ( OptionalParamCount > 0 )
					{
						throw new CSlashCommandException( "ERROR: required param specified after optional param in slash command: " + command_type.Name );
					}

					if ( consume_remaining )
					{
						parse_string.Append( @"(?'" + param_group + @"'[^\r\n]*)" );
					}
					else
					{
						parse_string.Append( @"(?:\s+(?:(?'" + param_group + @"'" + valid_param_characters + @"+)|(?:""(?'" + param_group + @"'.*?)"")))" );
					}
				}
				else
				{
					parse_string.Append( @"(?:\s+(?:(?'" + param_group + @"'" + valid_param_characters + @"+)|(?:""(?'" + param_group + @"'.*?)"")))?" );
					OptionalParamCount++;
				}

				usage_string_builder.Append( " [" );
				usage_string_builder.Append( param_info.ParamNameText );
				if ( !is_required )
				{
					usage_string_builder.Append( CCommonResource.Get_Text( ECommonTextID.Help_Optional ) );
				}

				// If the parameter is an enum, then let's add a list of the valid values to the help string
				Type property_type = prop.PropertyType;
				if ( property_type.IsEnum )
				{
					usage_string_builder.Append( " ( " );
					string[] enum_values = Enum.GetNames( property_type );
					int value_count = enum_values.Length;
					for( int i = 0; i < value_count; i++ )
					{
						usage_string_builder.Append( enum_values[ i ] );
						if ( i < value_count - 1 )
						{
							usage_string_builder.Append( ", " );
						}
					}
					usage_string_builder.Append( " )" );
				}

				usage_string_builder.Append( "]" );

				m_Params.Add( param_info );
				current_param++;
			}
			
			CommandParser = new Regex( parse_string.ToString() );
			CommandType = command_type;
			CommandName = command_name;

			try
			{
				CSlashCommand type_instance = Activator.CreateInstance( command_type ) as CSlashCommand;
				
				string help_text_id_string = CSlashCommandTextBuilder.Build_Slash_Command_Help_Text_Key( command_attr.TextIDSuffix );			

				string command_help = CCommonResource.Get_Text( help_text_id_string );
				string command_usage = CCommonResource.Get_Text( ECommonTextID.Help_Usage_Command, usage_string_builder.ToString() );
				HelpText = command_help + "\n\n" + command_usage;			
			}
			catch ( Exception )
			{
				HelpText = CCommonResource.Get_Text( ECommonTextID.Help_No_Help_For_Command );
			}

			return true;
		}

		// Properties
		public Type CommandType { get; private set; }
		public string CommandName { get; private set; }
		public IEnumerable< CSlashCommandParamInfo > Params { get { return m_Params; } }
		public Regex CommandParser { get; private set; }
		public int RequiredParamCount { get { return m_Params.Count - OptionalParamCount; } }
		public int OptionalParamCount { get; private set; }
		public string HelpText { get; private set; }

		// Fields
		private List< CSlashCommandParamInfo > m_Params = new List< CSlashCommandParamInfo >();
	}
	
	[Serializable]
	public class CSlashCommand
	{
		// Construction
		public CSlashCommand()
		{
		}

		// Methods
		public virtual void On_Command_Name( string command_name ) {}
	}
	
	public class CSlashCommandInstance
	{
		// Construction
		public CSlashCommandInstance( string input_string )
		{
			m_OriginalText = input_string;
		}
		
		// Methods
		public bool Parse( Regex command_parser, int required_params, int max_params )
		{
			Match m = command_parser.Match( m_OriginalText );
			if ( !m.Success )
			{
				return false;
			}

			for ( int i = 1; i <= max_params; i++ )
			{
				string group_name = "param" + i.ToString();
				Group group = m.Groups[ group_name ];
				if ( group == null || !group.Success )
				{
					return i > required_params;
				}

				m_Parameters.Add( group.ToString() );
			}

			return true;
		}

		// Properties
		public string Command
		{
			get
			{
				Match m = Regex.Match( m_OriginalText, @"/(\w+).*" );
				if ( !m.Success )
				{
					return null;
				}
				else
				{
					return m.Groups[ 1 ].ToString();
				}
			}
		}

		public IEnumerable< string > Parameters { get { return m_Parameters; } }
		public int Count { get { return m_Parameters.Count; } }
		public string this[ int index ] { get { return m_Parameters[ index ]; } }

		// Fields
		private string m_OriginalText;
		private List< string > m_Parameters = new List< string >();
	}

}