/*

	HeapTests.cs

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
using System.Text;
using NUnit.Framework;
using NIPCommon;


namespace NIPCommonTests
{
	[TestFixture]
	public class CHeapTests
	{
		private class CTrackingElement : IIndexableHeapElement
		{
			public CTrackingElement()
			{
				Value = -1;
			}

			public CTrackingElement( int value )
			{
				Value = value;
			}

			public static int Compare_Elements( CTrackingElement element1, CTrackingElement element2 )
			{
				return element1.Value.CompareTo( element2.Value );
			}

			public void Set_Heap_Index( int index ) { m_Index = index; }
			public int Get_Heap_Index() { return m_Index; }

			public int Value { get; private set; }

			private int m_Index = -1;
		}

		// Empty Property Tests
		[Test]
		public void Empty__EmptyInt__ReturnsTrue()
		{
			var heap = THeap< int >.Create< int >();

			Assert.IsTrue( heap.Empty );
		}

		[Test]
		public void Empty__EmptyTrackingElement__ReturnsTrue()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );

			Assert.IsTrue( heap.Empty );
		}

		[Test]
		public void Empty__NonEmptyInt__ReturnsFalse()
		{
			var heap = THeap< int >.Create< int >();

			heap.Add( 5 );

			Assert.IsFalse( heap.Empty );
		}

		[Test]
		public void Empty__NonEmptyTrackingElement__ReturnsFalse()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );

			heap.Add( new CTrackingElement( 5 ) );

			Assert.IsFalse( heap.Empty );
		}

		// Size property tests
		[Test]
		public void Size__Empty__ReturnsZero()
		{
			var heap = THeap< int >.Create< int >();

			Assert.IsTrue( heap.Size == 0 );
		}

		[Test]
		public void Size__NElements__ReturnsN()
		{
			var heap = THeap< int >.Create< int >();

			for ( int i = 1; i < 100; i++ )
			{
				heap.Add( i );
				Assert.IsTrue( heap.Size == i );
			}

			for ( int i = 99; i > 0; i-- )
			{
				Assert.IsTrue( heap.Size == i );
				heap.Pop();
			}
		}

		// Pop tests
		[Test]
		[ExpectedException( typeof( CEmptyHeapException ) )]
		public void Pop__Empty__ThrowsEmptyHeapException()
		{
			var heap = THeap< int >.Create< int >();

			heap.Pop();
		}

		// Peek_Top tests
		[Test]
		[ExpectedException( typeof( CEmptyHeapException ) )]
		public void Peek_Top__Empty__ThrowsEmptyHeapException()
		{
			var heap = THeap< int >.Create< int >();

			int top_value = heap.Peek_Top();
		}

		[Test]
		public void Peek_Top__Sequence__ReturnsSortedSequence()
		{
			var heap = THeap< int >.Create< int >();

			for ( int i = 10; i > 0; i-- )
			{
				heap.Add( i );
			}

			for ( int i = 1; i <= 10; i++ )
			{
				Assert.IsTrue( heap.Peek_Top() == i );
				heap.Pop();
			}
		}

		// Clear tests
		[Test]
		public void Clear__NonEmpty__EmptyTrueVerifiesTrue()
		{
			var heap = THeap< int >.Create< int >();

			for ( int i = 10; i > 0; i-- )
			{
				heap.Add( i );
			}

			heap.Clear();
			Assert.IsTrue( heap.Empty );
			Assert.IsTrue( heap.Verify_Heap() );
			Assert.IsTrue( heap.Verify_Index_Tracking() );
		}

		// Add tests
		[Test]
		public void Add__Sequence__Verify_Index_TrackingReturnsTrue()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );

			for ( int i = 100; i >= 0; i-- )
			{
				heap.Add( new CTrackingElement( i ) );
				Assert.IsTrue( heap.Verify_Index_Tracking() );
			}
		}

		[Test]
		public void Verify_Heap__NonEmpty__ReturnsTrue()
		{
			var heap = THeap< int >.Create< int >();

			new int[] {3, 2, 1}.Apply( n => heap.Add( n ) );

			Assert.IsTrue( heap.Verify_Heap() );
		}

		// Remove tests
		[Test]
		[ExpectedException( typeof( CUnindexableHeapTypeParameterException< int > ) )]
		public void Remove__EmptyNonTracking__ThrowsUnindexableHeapTypeParameterException()
		{
			var heap = THeap< int >.Create< int >();

			heap.Remove( 5 );
		}

		[Test]
		[ExpectedException( typeof( CCorruptedHeapRemoveException ) )]
		public void Remove__InvalidElement1__ThrowsCorruptedHeapRemoveException()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );

			CTrackingElement element = new CTrackingElement( 5 );
			element.Set_Heap_Index( 5 );

			heap.Remove( element );
		}

		[Test]
		[ExpectedException( typeof( CCorruptedHeapRemoveException ) )]
		public void Remove__InvalidElement2__ThrowsCorruptedHeapRemoveException()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );

			heap.Add( new CTrackingElement( 1 ) );
			heap.Add( new CTrackingElement( 2 ) );

			CTrackingElement element = new CTrackingElement( 5 );
			element.Set_Heap_Index( 1 );

			heap.Remove( element );
		}

		[Test]
		public void Remove__Sequence__VerifyIndexTrue()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );
			List< CTrackingElement > elements = new List< CTrackingElement >();

			for ( int i = 100; i > 0; i-- )
			{
				CTrackingElement element = new CTrackingElement( i );
				elements.Add( element );
				heap.Add( element );
			}

			for ( int i = 0; i < elements.Count; i++ )
			{
				heap.Remove( elements[ i ] );
				Assert.IsTrue( heap.Verify_Index_Tracking() );
			}
		}

		// Verify_Heap function tests
		[Test]
		public void Verify_Heap__Empty__ReturnsTrue()
		{
			var heap = THeap< int >.Create< int >();

			Assert.IsTrue( heap.Verify_Heap() );
		}

		// Verify_Index_Tracking function tests
		[Test]
		public void Verify_Index_Tracking__EmptyAndNonTracking__ReturnsTrue()
		{
			var heap = THeap< int >.Create< int >();

			Assert.IsTrue( heap.Verify_Index_Tracking() );
		}

		[Test]
		public void Verify_Index_Tracking__EmptyAndTracking__ReturnsTrue()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );

			Assert.IsTrue( heap.Verify_Index_Tracking() );
		}

		// Compound Tests
		[Test]
		public void Compound__Random_Ops__VerifiesReturnTrue()
		{
			var heap = THeap< CTrackingElement >.Create( CTrackingElement.Compare_Elements );
			var elements = new List< CTrackingElement >();

			for ( int i = 0; i < 10; i++ )
			{
				Random rng = new Random( i );

				for ( int j = 0; j < 50; j++ )
				{
					CTrackingElement element = new CTrackingElement( rng.Next() % 10000 );
					elements.Add( element );
					heap.Add( element );

					Assert.IsTrue( heap.Verify_Index_Tracking() );
					Assert.IsTrue( heap.Verify_Heap() );
				}

				for ( int j = 0; j < 20; j++ )
				{
					int random_op = rng.Next() % 3;
					switch ( random_op )
					{
						case 0:
							heap.Pop();
							break;

						case 1:
							int remove_index = rng.Next() % elements.Count;
							heap.Remove( elements[ remove_index ] );
							break;
					}

					Assert.IsTrue( heap.Verify_Index_Tracking() );
					Assert.IsTrue( heap.Verify_Heap() );
				}
			}
		}
	}
}
