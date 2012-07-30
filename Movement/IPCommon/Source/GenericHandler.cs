/*

	GenericHandler.cs

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
using System.Reflection;

namespace NIPCommon
{
	public delegate void DGenericHandler< T >( T handleable );

	[AttributeUsage( AttributeTargets.Method )]
	public class GenericHandlerAttribute : Attribute
	{
		public GenericHandlerAttribute() :
			base()
		{
		}
	}

	public class CHandlerException : Exception
	{
		public CHandlerException( string message ) :
			base( message )
		{}
	}

	public class CGenericHandlerManager
	{
		// Construction
		private CGenericHandlerManager() {}
		static CGenericHandlerManager() {}

		// Methods
		// Public interface
		// must be called early on by all threads
		public static void Initialize_Thread_Instance()
		{
			m_Instance = new CGenericHandlerManager();
		}

		public void Find_Handlers< T >( Assembly handler_assembly )
		{
			BindingFlags binding_flags_all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			Type handleable_base_type = typeof( T );
			Type open_generic_handler_type = typeof( DGenericHandler<> );

			foreach ( var type in handler_assembly.GetTypes() )
			{
				MethodInfo[] methods = type.GetMethods( binding_flags_all );
				foreach ( var method_info in methods )
				{
					// has this method been marked as a slash command handler
					GenericHandlerAttribute handler_attribute = Attribute.GetCustomAttribute( method_info, typeof( GenericHandlerAttribute ) ) as GenericHandlerAttribute;
					if ( handler_attribute == null )
					{
						continue;
					}

					// slash command handlers only have 1 parameter, the slash command
					ParameterInfo[] parameters = method_info.GetParameters();
					if ( parameters.Length != 1 )
					{
						throw new CHandlerException( String.Format( "Handler function {0} in type {1} has the incorrect number of arguments (must be 1).", method_info.Name, type.Name ) );
					}

					// make sure the parameter type derives from CSlashCommand
					ParameterInfo parameter = parameters[ 0 ];
					Type parameter_type = parameter.ParameterType;
					if ( !parameter_type.IsSubclassOf( handleable_base_type ) )
					{
						continue;
					}

					// make sure we haven't already registered a handler for this slash command
					if ( Handler_Exists( parameter_type ) )
					{
						throw new CHandlerException( String.Format( "Duplicate slash command handler detected for command ({0})!", parameter_type.Name ) );
					}

					// Scarrrrrrryyyyy stuff
					//
					// Original inspiration for this solution found at: 
					//		http://social.msdn.microsoft.com/Forums/en/csharplanguage/thread/fe14d396-bc35-4f98-851d-ce3c8663cd79
					//
					// By using Cast_And_Call rather than Cast and composing/invoking delegates, together with the strong cast at the end, we avoid
					// using DynamicInvoke/Invoke completely, which is a huge performance gain
					Type derived_handler_type = open_generic_handler_type.MakeGenericType( parameter_type );
					MethodInfo cast_and_call_method_info = this.GetType().GetMethod( "Cast_And_Call", binding_flags_all ).MakeGenericMethod( parameter_type );
					Delegate derived_delegate = null;
	
					if ( method_info.IsStatic )
					{
						// static handlers are a little easier
						derived_delegate = Delegate.CreateDelegate( derived_handler_type, type, method_info.Name );						
					}
					else
					{
						// non static handlers require us to go through the Instance property in order to get the class instance; the class must be a singleton
						// implemented in the standard pattern
						PropertyInfo instance_property_info = type.GetProperty( "Instance" );
						if ( instance_property_info == null )
						{
							instance_property_info = type.GetProperty( "BaseInstance" );

							if ( instance_property_info == null )
							{
								throw new CHandlerException( String.Format( "Non-static handler {0} in type {1} for class {2} must belong to a singleton class with a static Instance or BaseInstance property getter.", method_info.Name, type.Name, parameter_type.Name ) );
							}
						}

						object singleton_instance = instance_property_info.GetValue( null, null );
						derived_delegate = Delegate.CreateDelegate( derived_handler_type, singleton_instance, method_info.Name );
					}

					// this final handler object ends up being a single composed method call without any reflection-on-invoke, type checking, or managed<->unmanaged transitions
					// which my original solution suffered from.
					//
					// It is the run-time, reflection-based equivalent to the line in the generic Register_Handler< T > function:
					//
					//		generic_handle = delegate( CSlashCommand command ) { handler( command as T ); };
					DGenericHandler< object > generic_handler = ( DGenericHandler< object > ) 
							Delegate.CreateDelegate( typeof( DGenericHandler< object > ), derived_delegate, cast_and_call_method_info );

					if ( generic_handler == null )
					{
						throw new CHandlerException( String.Format( "Unable to build handler for class {0}.", parameter_type.Name ) );
					}

					m_Handlers.Add( parameter_type, generic_handler );
				}
			}
		}

		public void Register_Handler< T >( DGenericHandler< T > handler ) where T : class
		{
			Type command_type = typeof( T );
			if ( Handler_Exists( command_type ) )
			{
				throw new CHandlerException( String.Format( "Duplicate handler detected for type ({0})!", command_type.Name ) );
			}

			DGenericHandler< object > generic_handler = delegate( object handleable ) { handler( handleable as T ); };
			m_Handlers.Add( command_type, generic_handler );
		}

		public bool Try_Handle( object handleable )
		{
			Type command_type = handleable.GetType();

			DGenericHandler< object > handler = null;
			if ( m_Handlers.TryGetValue( command_type, out handler ) )
			{
				handler( handleable );
				return true;
			}

			return false;
		}

		// Private interface
		private bool Handler_Exists( Type command_type )
		{
			return  m_Handlers.ContainsKey( command_type );
		}

		private static void Cast_And_Call< T >( DGenericHandler< T > handler, object o )
      {
         handler( (T) o );
      }

		// Properties
		public static CGenericHandlerManager Instance { get { return m_Instance; } }
		 
		// Fields
		[ThreadStatic]
		private static CGenericHandlerManager m_Instance = null;

		private Dictionary< Type, DGenericHandler< object > > m_Handlers = new Dictionary< Type, DGenericHandler< object > >();

	}
}
