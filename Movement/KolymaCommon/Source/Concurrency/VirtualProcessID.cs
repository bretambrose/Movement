/*

	ConncurentQueues.cs

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
using System.Globalization;

namespace NKolymaCommon
{
	public enum EProcessSubID : ushort
	{
		Invalid,

		First
	}

	public struct SVirtualProcessID< T > where T : struct, IConvertible 
	{
		public SVirtualProcessID() 
		{ 
			m_PID = 0; 
		}

		public SVirtualProcessID( T subject, EProcessSubID primary_id = EProcessSubID.First, EProcessSubID major_id = EProcessSubID.First, EProcessSubID minor_id = EProcessSubID.First )
		{
			if ( !typeof( T ).IsEnum )
			{
				throw new ArgumentException( "Generic parameter of SVirtualProcessID must be an enum" );
			}

			ulong subject_ul = subject.ToUInt16( CultureInfo.CurrentCulture );
			ulong primary_ul = (ulong) primary_id;
			primary_ul <<= 16;
			ulong major_ul = (ulong) major_id;
			major_ul <<= 32;
			ulong minor_ul = (ulong) minor_id;
			minor_ul <<= 48;

			m_PID = subject_ul | primary_ul | major_ul | minor_ul;
		}

		public T Get_Subject()
		{
			ulong subject = m_PID & 0xFFFF;
			return (T) ( Enum.ToObject( typeof( T ), (ushort) subject ) );
		}

		public EProcessSubID Get_Primary_ID()
		{
			return (EProcessSubID)( ( m_PID >> 16 ) & 0xFFFF );
		}

		public void Set_Primary_ID( EProcessSubID primary_id )
		{
			ulong primary_mask = ~( 0xFFFFUL << 16 );
			ulong new_primary_id = (ulong) primary_id;
			new_primary_id <<= 16;

			m_PID = ( m_PID & primary_mask ) | new_primary_id;
		}
		
		public EProcessSubID Get_Major_ID()
		{
			return (EProcessSubID)( ( m_PID >> 32 ) & 0xFFFF );
		}

		public void Set_Major_ID( EProcessSubID major_id )
		{
			ulong major_mask = ~( 0xFFFFUL << 32 );
			ulong new_major_id = (ulong) major_id;
			new_major_id <<= 32;

			m_PID = ( m_PID & major_mask ) | new_major_id;
		}

		public EProcessSubID Get_Minor_ID()
		{
			return (EProcessSubID)( ( m_PID >> 48 ) & 0xFFFF );
		}

		public void Set_Minor_ID( EProcessSubID minor_id )
		{
			ulong minor_mask = ~( 0xFFFFUL << 48 );
			ulong new_minor_id = (ulong) minor_id;
			new_minor_id <<= 48;

			m_PID = ( m_PID & minor_mask ) | new_minor_id;
		}

		public ulong Get_PID()
		{
			return m_PID;
		}

		public bool Is_Valid()
		{
			return Get_Primary_ID() != EProcessSubID.Invalid && Get_Major_ID() != EProcessSubID.Invalid && Get_Minor_ID() != EProcessSubID.Invalid;
		}

		public bool Needs_Sub_ID_Allocation()
		{
			return Get_Primary_ID() == EProcessSubID.Invalid || Get_Major_ID() == EProcessSubID.Invalid || Get_Minor_ID() == EProcessSubID.Invalid;
		}

		public ulong m_PID;
	}
}