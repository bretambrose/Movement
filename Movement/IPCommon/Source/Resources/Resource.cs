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

namespace NIPCommon
{
	[Resource( EResourceType.Text )]
	public enum ECommonTextID
	{
		Invalid = -1,

		// slash command parsing
		Unable_To_Parse_Slash_Command,
		Unable_To_Parse_Parameter,
		Unknown_Command,
		Slash_Command_Missing_Parameter,
		Multiple_Commands_Matched,

		// misc help text
		Help,
		Help_Bad_Input,
		Help_No_Help_For_Command,
		Help_Usage_Command,
		Help_CommandGroup_Commands,
		Help_Optional,
		Undocumented_Parameter,

		// command groups
		CommandGroup_Debug,
		Help_CommandGroup_Debug,
		CommandGroup_Logging,
		Help_CommandGroup_Logging,
		CommandGroup_Test,
		Help_CommandGroup_Test,

		// Commands
		Command_Name_Help,
		Help_Command_Help,
		Command_Name_Crash,
		Help_Command_Crash,

		// Command param names (for building usage strings)
		Command_Help_Param_CommandGroupOrName,
		Command_SetLogLevel_Param_Level,

	}

	[Resource( EResourceType.Image )]
	public enum ECommonImageID
	{
		Invalid = -1
	}

	public class CCommonResource
	{
		// Public interface
		public static void Initialize_Common_Resources()
		{
			CResourceManager.Instance.Initialize_Assembly_Resources( "CRShared.Source.Resources.SharedResources", Assembly.GetExecutingAssembly() );
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
	}
}