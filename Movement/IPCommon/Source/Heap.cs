/*

	Heap.cs

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
using System.Linq;

namespace NIPCommon
{
	public interface IIndexableHeapElement
	{
		void Set_Heap_Index( int index );
		int Get_Heap_Index();
	}

	public sealed class CEmptyHeapException : Exception
	{
		public CEmptyHeapException() :
			base( "Heap is empty.  Cannot query elements in it." )
		{}
	}
	
	public sealed class CIncomparableHeapTypeParameterException< T > : Exception
	{
		public CIncomparableHeapTypeParameterException() :
			base( string.Format( "Heap generic parameter type class {0} does not implement IComparable and no comparison delegate was supplied", typeof( T ).Name ) )
		{}
	}

	public sealed class CUnindexableHeapTypeParameterException< T > : Exception
	{
		public CUnindexableHeapTypeParameterException() :
			base( string.Format( "Heap generic parameter type class {0} does not implement IIndexableHeapElement and so Remove is not a valid operation", typeof( T ).Name ) )
		{}
	}

	public sealed class CCorruptedHeapRemoveException : Exception
	{
		public CCorruptedHeapRemoveException() :
			base( "Attempted to remove an element from a heap that does not contain that element." )
		{}
	}

	public delegate int DHeapElementComparison< T >( T lhs, T rhs );
	public delegate void DSwapHeapElements( int index1, int index2 );
	public delegate void DSetElementIndex( int element_index, int index );

	public sealed class THeap< T > where T : new()
	{
		// Construction
		private THeap( DHeapElementComparison< T > comparator )
		{
			m_Comparator = comparator;

			TracksIndices = typeof( T ).GetInterfaces().Any( t => t == typeof( IIndexableHeapElement ) );

			if ( TracksIndices )
			{
				m_Swapper = Swap_Indexed_Elements;
				m_IndexSetter = Set_Indexed_Index;
			}
			else
			{
				m_Swapper = Swap_Unindexed_Elements;
				m_IndexSetter = Set_Unindexed_Index;
			}

			m_HeapVector = new List< T >();
			m_HeapVector.Add( new T() );	// dummy element in the 0th index
			m_IndexSetter( 0, 0 );
		}

		public static THeap< U > Create< U >() where U : IComparable< U >, new()
		{
			return new THeap< U >( DefaultHeapComparison< U > );
		}

		public static THeap< T > Create( DHeapElementComparison< T > comparator )
		{
			return new THeap< T >( comparator );
		}

		// Methods
		// Public interface		
		public T Peek_Top()
		{
			if ( Empty )
			{
				throw new CEmptyHeapException();
			}
			
			return m_HeapVector[ 1 ];
		}

		public void Pop()
		{
			if ( Empty )
			{
				throw new CEmptyHeapException();
			}

			m_Swapper( 1, Size );
			m_IndexSetter( Size, 0 );
			m_HeapVector.RemoveAt( Size );

			if ( Size > 1 )
			{
				Heapify_Down( 1 );
			}
		}
		
		public void Add( T element )
		{
			m_HeapVector.Add( element );
			m_IndexSetter( Size, Size );
			Heapify_Up( Size );
		}

		void Remove( T element )
		{
			if ( !TracksIndices )
			{
				throw new CUnindexableHeapTypeParameterException< T >();
			}

			IIndexableHeapElement indexed_element = element as IIndexableHeapElement;
			int index = indexed_element.Get_Heap_Index();
			if ( m_Comparator( m_HeapVector[ index ], element ) != 0 )
			{
				throw new CCorruptedHeapRemoveException();
			}

			int size = Size;

			// Swap the last into the removal spot, then push down or up as needed
			m_Swapper( index, size );
			m_IndexSetter( size, 0 );
			m_HeapVector.RemoveAt( size );

			Heapify_Up( index );
			Heapify_Down( index );
		}

		public void Clear()
		{
			int size = Size;
			for ( int i = 1; i < size; i++ )
			{
				m_IndexSetter( i, 0 );
			}

			m_HeapVector.Clear();
			m_HeapVector.Add( new T() );
			m_IndexSetter( 0, 0 );
		}
		
		// Public testing interface
		public bool Verify_Index_Tracking()
		{
			if ( !TracksIndices )
			{
				return true;
			}

			int size = Size;
			for ( int i = 0; i < size; ++i )
			{
				IIndexableHeapElement indexed_element = m_HeapVector[ i ] as IIndexableHeapElement;
				if ( indexed_element.Get_Heap_Index() != i )
				{
					return false;
				}
			}

			return true;
		}

		public bool Verify_Heap()
		{
			int size = Size;

			for ( int index = 2; index < size; ++index )
			{
				int parent_index = ParentIndex( index );
				if ( m_Comparator( m_HeapVector[ parent_index ], m_HeapVector[ index ] ) > 0 )
				{
					return false;
				}
			}

			return true;
		}

		// Private interface
		private void Heapify_Up( int index )
		{
			int parent = ParentIndex( index );

			while ( parent >= 1 && m_Comparator( m_HeapVector[ parent ], m_HeapVector[ index ] ) > 0 )
			{
				m_Swapper( index, parent );
				
				index = parent;
				parent = ParentIndex( index );			
			}
		}

		private void Heapify_Down( int index )
		{
			int size = Size;
			while ( true )
			{
				int left = LeftIndex( index );
				int right = RightIndex( index );
				int smallest = index;
				
				if ( left <= size && m_Comparator( m_HeapVector[ left ], m_HeapVector[ index ] ) < 0 )
				{
					smallest = left;
				}

				if ( right <= size && m_Comparator( m_HeapVector[ right ], m_HeapVector[ smallest ] ) < 0 )
				{
					smallest = right;
				}
				
				if ( smallest == index )
				{
					break;
				}
				
				m_Swapper( smallest, index );
				
				index = smallest;
			}
		}
		
		private int ParentIndex( int index )
		{
			return index / 2;
		}
		
		private int LeftIndex( int index )
		{
			return index * 2;
		}
		
		private int RightIndex( int index )
		{
			return index * 2 + 1;
		}
		
		// Helper statics
		private static int DefaultHeapComparison< U >( U lhs, U rhs ) where U : IComparable< U >
		{
			return lhs.CompareTo( rhs );
		}

		private void Swap_Unindexed_Elements( int index1, int index2 )
		{
			T temp = m_HeapVector[ index1 ];
			m_HeapVector[ index1 ] = m_HeapVector[ index2 ];
			m_HeapVector[ index2 ] = temp;
		}

		private void Swap_Indexed_Elements( int index1, int index2 )
		{
			IIndexableHeapElement element1 = m_HeapVector[ index1 ] as IIndexableHeapElement;
			IIndexableHeapElement element2 = m_HeapVector[ index2 ] as IIndexableHeapElement;

			T temp = m_HeapVector[ index1 ];
			m_HeapVector[ index1 ] = m_HeapVector[ index2 ];
			m_HeapVector[ index2 ] = temp;

			element1.Set_Heap_Index( index2 );
			element2.Set_Heap_Index( index1 );
		}

		private void Set_Unindexed_Index( int element_index, int index )
		{
		}

		private void Set_Indexed_Index( int element_index, int index ) 
		{
			IIndexableHeapElement indexable_element = m_HeapVector[ element_index ] as IIndexableHeapElement;
			indexable_element.Set_Heap_Index( index );
		}

		// Properties
		public bool Empty { get { return m_HeapVector.Count <= 1; } }
		public int Size { get { return m_HeapVector.Count - 1; } }
		public bool TracksIndices { get; private set; }
		
		// Fields
		private List< T > m_HeapVector = null;
		private DHeapElementComparison< T > m_Comparator = null;
		private DSwapHeapElements m_Swapper = null;
		private DSetElementIndex m_IndexSetter = null;

	}
}
