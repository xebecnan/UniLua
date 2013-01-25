
// #define DEBUG_LUA_TABLE

using System.Collections.Generic;

using Debug = UniLua.Tools.Debug;

namespace UniLua
{

	public class LuaTable : LuaObject
	{
		private class Node {
			public LuaObject		Key;
			public LuaObject		Value;
			public Node				Prev;
			public Node				Next;
			public Node( LuaObject key, LuaObject value )
				{ Key = key; Value = value; }
		}

		private Dictionary<LuaObject, Node> 	DictPart;
		private Node							Head;

		// 当 dirty 为 false 时, 缓存的 legnth 是精确的
		// 当 dirty 为 true 时, 实际 length 一定 >= 缓存的 length
		private int								CachedLength;
		private bool							CachedLengthDirty;

		public uint								NoTagMethodFlags;

		public int Length {
			get {
				if( !CachedLengthDirty )
					return CachedLength;
				else {
					int i = CachedLength + 1;
					while( DictPart.ContainsKey( new LuaNumber(i) ) ) {
						++i;
					}
					CachedLength = i - 1;
					CachedLengthDirty = false;
					return CachedLength;
				}
			}
		}

		public override LuaType LuaType {
			get { return LuaType.LUA_TTABLE; }
		}

		public override bool IsTable		{ get { return true; } }

		public LuaTable MetaTable { get; set; }

		public LuaTable( int arraySize=0, int dictSize=0 )
		{
			DictPart = new Dictionary<LuaObject, Node>( arraySize + dictSize );
			Head	 = null;

			CachedLength		= 0;
			CachedLengthDirty	= false;

			NoTagMethodFlags	= ~0u;
		}

		public void SetInt( int key, LuaObject val )
		{
			Set( new LuaNumber(key), val );
		}

		public LuaObject GetInt( int key )
		{
			return Get( new LuaNumber( key ) );
		}

		public LuaObject GetStr( string key )
		{
			return Get( new LuaString( key ) );
		}

		public void Set( LuaObject key, LuaObject val )
		{
			if( val.IsNil ) {
				_Remove( key );
			}
			else {
				_Set( key, val );
			}

			// invalidate no-tag-method cache
			NoTagMethodFlags = 0u;
		}

		public LuaObject Get( LuaObject key )
		{
			Node node;
			if( DictPart.TryGetValue( key, out node ) )
			{
				return node.Value;
			}

			return new LuaNil();
		}

		public void DebugGet( LuaObject key, out LuaObject outKey,
			out LuaObject outValue )
		{
			Node node;
			if( DictPart.TryGetValue( key, out node ) ) {
				outKey	 = node.Key;
				outValue = node.Value;
			}
			else {
				outKey 		= new LuaNil();
				outValue	= new LuaNil();
			}
		}

		public bool Next( LuaState lua, StkId key )
		{
			var val = key+1;
			Node next;

			if( key.Value.IsNil )
				next = Head;
			else if( DictPart.TryGetValue( key.Value, out next ) )
				next = next.Next;
			else
				((LuaState)lua).G_RunError("invalid key to 'next'");

			if( next != null ) {
				key.Value = next.Key;
				val.Value = next.Value;
				return true;
			}
			else return false;
		}

		private void _UpdateLengthOnRemove( LuaObject key )
		{
			var n = key as LuaNumber;
			if( n == null )
				return;

			int i = (int)n.Value;
			if( 1 <= i && i <= CachedLength ) {
				CachedLength = i - 1;
				CachedLengthDirty = false;
			}
		}

		private void _UpdateLengthOnAppend( LuaObject key )
		{
			var n = key as LuaNumber;
			if( n == null )
				return;

			int i = (int)n.Value;
			if( i == CachedLength + 1 ) {
				CachedLength = i;
				CachedLengthDirty = true;
			}
		}

		private void _Remove( LuaObject key )
		{
			Node old;
			if( DictPart.TryGetValue( key, out old ) ) {
				if( Head == old ) Head = old.Next;
				if( old.Prev != null ) old.Prev.Next = old.Next;
				if( old.Next != null ) old.Next.Prev = old.Prev;
				DictPart.Remove( key );

				_UpdateLengthOnRemove(key);
			}
		}

		private void _Set( LuaObject key, LuaObject val )
		{
			var vnode = new Node(key, val);
			Node old;
			if( DictPart.TryGetValue( key, out old ) ) {
				if( Head == old ) Head = vnode;
				vnode.Prev = old.Prev;
				vnode.Next = old.Next;
				if( old.Prev != null ) old.Prev.Next = vnode;
				if( old.Next != null ) old.Next.Prev = vnode;
			}
			else {
				vnode.Next = Head;
				if( Head != null ) Head.Prev = vnode;
				Head = vnode;

				_UpdateLengthOnAppend( key );
			}

			DictPart[ key ] = vnode;
		}

	}

}

