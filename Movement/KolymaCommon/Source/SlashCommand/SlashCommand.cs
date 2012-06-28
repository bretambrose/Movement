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

namespace NKolymaCommon
{
	public class SlashCommandException : Exception
	{
		public SlashCommandException( string exception_info )
		{
			ExceptionInfo = exception_info;
		}
		
		public string ExceptionInfo { get; private set; }
	}
	
	public enum ESlashCommandGroup
	{
		None,

		Chat,
		Network,
		Debug,
		Lobby,
		Social,
		Browse,
		ProcessControl,
		Logging,
		Test,
		Match,
		MatchUI,
		Quickmatch
	}

	public class CSlashCommandGroupInfo
	{
		// constructors
		public CSlashCommandGroupInfo( ESlashCommandGroup command_group )
		{
			CommandGroup = command_group;
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
			string command_group_name = Enum.GetName( typeof( ESlashCommandGroup ), CommandGroup );
			ESharedTextID command_group_help_id = (ESharedTextID) Enum.Parse( typeof( ESharedTextID ), "Shared_Help_CommandGroup_" + command_group_name, true );

			string group_help = CSharedResource.Get_Text< ESharedTextID >( command_group_help_id );
			string command_list = CSharedResource.Get_Text< ESharedTextID >( ESharedTextID.Shared_Help_CommandGroup_Commands );
						
			HelpText = group_help + String.Format( command_list, command_list_string );		
		}

		// properties
		public ESlashCommandGroup CommandGroup { get; private set; }
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
		public void Initialize_Text< T >( string text_id_prefix, string command_text_name ) where T : IConvertible
		{
			try
			{
				string param_name_text_id_string = text_id_prefix + "_Command_" + command_text_name + "_Param_" + ParamInfo.Name;
				T text_id = (T) Enum.Parse( typeof( T ), param_name_text_id_string, true );
				ParamNameText = CSharedResource.Get_Text< T >( text_id );
			}
			catch ( Exception )
			{
				ParamNameText = CSharedResource.Get_Text< ESharedTextID >( ESharedTextID.Shared_Undocumented_Parameter );
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
		public bool Initialize< T >( Type command_type, string command_name, string command_text_id_suffix, string text_id_prefix ) where T : IConvertible
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
					throw new SlashCommandException( "ERROR: Cannot have additional params after a consume remaining param, in command: " + command_type.Name );
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
				param_info.Initialize_Text< T >( text_id_prefix, command_text_id_suffix );

				if ( is_required )
				{
					if ( OptionalParamCount > 0 )
					{
						throw new SlashCommandException( "ERROR: required param specified after optional param in slash command: " + command_type.Name );
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
					usage_string_builder.Append( CSharedResource.Get_Text< ESharedTextID >( ESharedTextID.Shared_Help_Optional ) );
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
				
				string text_id_string = text_id_prefix + "_Help_Command_" + command_attr.TextIDSuffix;			
				T help_text_id = (T) Enum.Parse( typeof( T ), text_id_string, true );

				string command_help = CSharedResource.Get_Text< T >( help_text_id );
				string command_usage = CSharedResource.Get_Text< ESharedTextID >( ESharedTextID.Shared_Help_Usage_Command, 
																										usage_string_builder.ToString() );
				HelpText = command_help + "\n\n" + command_usage;			
			}
			catch ( Exception )
			{
				HelpText = CSharedResource.Get_Text< ESharedTextID >( ESharedTextID.Shared_Help_No_Help_For_Command );
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
		
		// Properties
		public virtual ESlashCommandGroup CommandGroup { get { return ESlashCommandGroup.None; } }
	}
	
	[AttributeUsage( AttributeTargets.Class )]
	public class SlashCommandAttribute : Attribute
	{
		// Construction
		public SlashCommandAttribute( string text_id_suffix ) :
			base()
		{
			TextIDSuffix = text_id_suffix;
			AllowSymbols = false;
		}
		
		// Properties
		public string TextIDSuffix { get; private set; }
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

}