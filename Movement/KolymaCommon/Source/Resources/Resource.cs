/*

	Resource.cs

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

namespace NKolyaCommon
{
	public enum EResourceType
	{
		Invalid = -1,
		Text,
		Image
	}

	public enum ESharedTextID
	{
		Invalid = -1,

		Shared_Unknown_Player,

		// slash command parsing
		Shared_Unable_To_Parse_Slash_Command,
		Shared_Unable_To_Parse_Parameter,
		Shared_Unknown_Command,
		Shared_Slash_Command_Missing_Parameter,
		Shared_Multiple_Commands_Matched,

		// misc help text
		Shared_Help,
		Shared_Help_Bad_Input,
		Shared_Help_No_Help_For_Command,
		Shared_Help_Usage_Command,
		Shared_Help_CommandGroup_Commands,
		Shared_Help_Optional,
		Shared_Undocumented_Parameter,

		// command groups
		Shared_CommandGroup_Chat,
		Shared_Help_CommandGroup_Chat,
		Shared_CommandGroup_Network,
		Shared_Help_CommandGroup_Network,
		Shared_CommandGroup_Debug,
		Shared_Help_CommandGroup_Debug,
		Shared_CommandGroup_Lobby,
		Shared_Help_CommandGroup_Lobby,
		Shared_CommandGroup_Social,
		Shared_Help_CommandGroup_Social,
		Shared_CommandGroup_Browse,
		Shared_Help_CommandGroup_Browse,
		Shared_CommandGroup_ProcessControl,
		Shared_Help_CommandGroup_ProcessControl,
		Shared_CommandGroup_Logging,
		Shared_Help_CommandGroup_Logging,
		Shared_CommandGroup_Test,
		Shared_Help_CommandGroup_Test,
		Shared_CommandGroup_Match,
		Shared_Help_CommandGroup_Match,
		Shared_CommandGroup_MatchUI,
		Shared_Help_CommandGroup_MatchUI,
		Shared_CommandGroup_Quickmatch,
		Shared_Help_CommandGroup_Quickmatch,

		// Commands
		Shared_Command_Name_Help,
		Shared_Help_Command_Help,
		Shared_Command_Name_Crash,
		Shared_Help_Command_Crash,

		// Command param names (for building usage strings)
		Shared_Command_Help_Param_CommandGroupOrName,
		Shared_Command_SetLogLevel_Param_Level,

		// Misc
		Shared_Begin_Internal_Tests,
		Shared_End_Internal_Tests
	}

	public enum ESharedImageID
	{
		Invalid = -1
	}

	public class CSharedResource
	{
		// Public interface
		public static void Initialize_Shared_Resources()
		{
			CResourceManager.Instance.Initialize_Assembly_Resources< ESharedTextID >( "CRShared.Source.Resources.SharedResources", Assembly.GetExecutingAssembly(), EResourceType.Text );
			CResourceManager.Instance.Initialize_Assembly_Resources< ESharedImageID >( "CRShared.Source.Resources.SharedResources", Assembly.GetExecutingAssembly(), EResourceType.Image );
		}

		public static string Get_Text< T >( T text_id ) where T : IConvertible
		{
			return CResourceManager.Instance.Get_Text< T >( text_id );
		}	

		public static string Get_Text< T >( T text_id, params object[] objects ) where T : IConvertible
		{
			string text_string = CResourceManager.Instance.Get_Text< T >( text_id );
			return String.Format( text_string, objects );
		}	

		public static void Output_Text( string text_output )
		{
			CLogicalThreadBase.BaseInstance.Add_UI_Notification( new CUITextOutputNotification( text_output ) );
		}

		public static void Output_Text< T >( T text_id ) where T : IConvertible
		{
			CLogicalThreadBase.BaseInstance.Add_UI_Notification( new CUITextOutputNotification( Get_Text( text_id ) ) );
		}

		public static void Output_Text< T >( T text_id, params object[] objects ) where T : IConvertible
		{
			string text_string = Get_Text( text_id );

			CLogicalThreadBase.BaseInstance.Add_UI_Notification( new CUITextOutputNotification( String.Format( text_string, objects ) ) );
		}

		public static void Output_Text_By_Category( ETextOutputCategory category, string text_output )
		{
			CLogicalThreadBase.BaseInstance.Add_UI_Notification( new CUITextOutputNotification( category, text_output ) );
		}
	}
}