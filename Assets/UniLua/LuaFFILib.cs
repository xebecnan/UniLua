﻿using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace UniLua
{

	internal class LuaFFILib
	{
		public const string LIB_NAME = "ffi.cs";

		public static int OpenLib( ILuaState lua )
		{
			var define = new NameFuncPair[]
			{
				new NameFuncPair( "clear_assembly_list", FFI_ClearAssemblyList ),
				new NameFuncPair( "add_assembly", FFI_AddAssembly ),

				new NameFuncPair( "clear_using_list", FFI_ClearUsingList ),
				new NameFuncPair( "using", FFI_Using ),

				new NameFuncPair( "parse_signature", FFI_ParseSignature ),

				new NameFuncPair( "get_type", FFI_GetType ),
				new NameFuncPair( "get_constructor", FFI_GetConstructor ),
				new NameFuncPair( "get_static_method", FFI_GetStaticMethod ),
				new NameFuncPair( "get_method", FFI_GetMethod ),
				new NameFuncPair( "call_method", FFI_CallMethod ),

				new NameFuncPair( "get_field", FFI_GetField ),
				new NameFuncPair( "get_field_value", FFI_GetFieldValue ),
				new NameFuncPair( "set_field_value", FFI_SetFieldValue ),

				new NameFuncPair( "get_prop", FFI_GetProp ),
				new NameFuncPair( "get_static_prop", FFI_GetStaticProp ),
				new NameFuncPair( "get_prop_value", FFI_GetPropValue ),
				new NameFuncPair( "set_prop_value", FFI_SetPropValue ),

				// new NameFuncPair( "call_constructor", FFI_CallConstructor ),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static int FFI_ClearAssemblyList( ILuaState lua )
		{
			AssemblyList.Clear();
			return 0;
		}

		private static int FFI_AddAssembly( ILuaState lua )
		{
			var name = lua.ToString(1);
			var assembly = Assembly.Load( name );
			if( assembly != null )
				AssemblyList.Add( assembly );
			else
				Tools.Debug.LogError("assembly not found:" + name);
			return 0;
		}

		private static int FFI_ClearUsingList( ILuaState lua )
		{
			UsingList.Clear();
			return 0;
		}

		private static int FFI_Using( ILuaState lua )
		{
			var name = lua.ToString(1);
			UsingList.Add( name );
			return 0;
		}

		// return `ReturnType', `FuncName', `ParameterTypes'
		private static int FFI_ParseSignature( ILuaState lua )
		{
			var signature = lua.ToString(1);
			var result = FuncSignatureParser.Parse( lua, signature );
			if( result.ReturnType != null )
				lua.PushString( result.ReturnType );
			else
				lua.PushNil();
			lua.PushString( result.FuncName );
			if( result.ParameterTypes != null ) {
				lua.NewTable();
				for( int i=0; i<result.ParameterTypes.Length; ++i ) {
					lua.PushString( result.ParameterTypes[i] );
					lua.RawSetI( -2, i+1 );
				}
			}
			else {
				lua.PushNil();
			}
			return 3;
		}

		private static int FFI_GetType( ILuaState lua )
		{
			string typename = lua.ToString(1);
			var t = GetType(typename);
			// UnityEngine.Debug.Log("GET TYPE:" + typename + " => " + t );
			if( t != null )
				lua.PushLightUserData(t);
			else
				lua.PushNil();
			return 1;
		}

		private static int FFI_GetConstructor( ILuaState lua )
		{
			var t = (Type)lua.ToUserData(1);
			var n = lua.RawLen(2);
			var types = new Type[n];
			for( int i=0; i<n; ++i )
			{
				lua.RawGetI( 2, i+1 );
				types[i] = (Type)lua.ToUserData(-1);
				lua.Pop( 1 );
			}

			var cinfo = t.GetConstructor( types );
			var ffiMethod = new FFIConstructorInfo(cinfo);
			lua.PushLightUserData( ffiMethod );
			return 1;
		}

		private static int GetMethodAux( ILuaState lua, BindingFlags flags )
		{
			var t = (Type)lua.ToUserData(1);
			var mname = lua.ToString(2);
			var n = lua.RawLen(3);
			var types = new Type[n];
			for( int i=0; i<n; ++i )
			{
				lua.RawGetI( 3, i+1 );
				types[i] = (Type)lua.ToUserData(-1);
				lua.Pop(1);
			}
			var minfo = t.GetMethod( mname,
				flags,
				null,
				CallingConventions.Any,
				types,
				null
				);
			if( minfo == null )
			{
				return 0;
			}
			else
			{
				var ffiMethod = new FFIMethodInfo(minfo);
				lua.PushLightUserData( ffiMethod );
				return 1;
			}
		}

		private static int FFI_GetMethod( ILuaState lua )
		{
			// var t = (Type)lua.ToUserData(1);
			// var mname = lua.ToString(2);
			// var n = lua.RawLen(3);
			// var types = new Type[n];
			// for( int i=0; i<n; ++i )
			// {
			// 	lua.RawGetI( 3, i+1 );
			// 	types[i] = (Type)lua.ToUserData(-1);
			// 	lua.Pop(1);
			// }
			// var minfo = t.GetMethod( mname,
			// 	BindingFlags.Instance
			// 		| BindingFlags.Public
			// 		| BindingFlags.InvokeMethod,
			// 	null,
			// 	CallingConventions.Any,
			// 	types,
			// 	null
			// 	);
			// var ffiMethod = new FFIMethodInfo(minfo);
			// lua.PushLightUserData( ffiMethod );
			// return 1;
			return GetMethodAux( lua,
				BindingFlags.Instance |
				BindingFlags.Public |
				BindingFlags.InvokeMethod );
		}

		private static int FFI_GetStaticMethod( ILuaState lua )
		{
			// var t = (Type)lua.ToUserData(1);
			// var mname = lua.ToString(2);
			// var minfo = t.GetMethod( mname,
			// 	BindingFlags.Static |
			// 	BindingFlags.Public |
			// 	BindingFlags.InvokeMethod );
			// var ffiMethod = new FFIMethodInfo(minfo);
			// lua.PushLightUserData( ffiMethod );
			// return 1;
			return GetMethodAux( lua,
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.InvokeMethod );
		}

		private static int FFI_CallMethod( ILuaState lua )
		{
			var ffiMethod = (FFIMethodBase)lua.ToUserData(1);
			if (ffiMethod != null)
			{
				try
				{
					return ffiMethod.Call(lua);
				}
				catch( Exception e )
				{
					lua.PushString( "call_method Exception: " + e.Message +
						"\nSource:\n" + e.Source +
						"\nStaceTrace:\n" + e.StackTrace );
					lua.Error();
					return 0;
				}
			}
			else
			{
				lua.PushString( "call_method cannot find MethodInfo" );
				lua.Error();
				return 0;
			}
		}

		private static int FFI_GetField( ILuaState lua )
		{
			var t = (Type)lua.ToUserData(1);
			var name = lua.ToString(2);
			var finfo = t.GetField( name,
				BindingFlags.Instance |
				BindingFlags.Public );
			if( finfo == null )
				throw new Exception("GetField failed:"+name);
			// UnityEngine.Debug.Log("FFI_GetField t:"+t+" name:"+name+" finfo:"+finfo);
			lua.PushLightUserData(finfo);
			return 1;
		}

		private static int FFI_GetFieldValue( ILuaState lua )
		{
			var finfo = (FieldInfo)lua.ToUserData(1);
			var inst = lua.ToUserData(2);
			var returnType = (Type)lua.ToUserData(3);
			var value = finfo.GetValue( inst );
			LuaStackUtil.PushRawValue( lua, value, returnType );
			return 1;
		}

		private static int FFI_SetFieldValue( ILuaState lua )
		{
			var finfo = (FieldInfo)lua.ToUserData(1);
			var inst = lua.ToUserData(2);
			var t = (Type)lua.ToUserData(4);
			var value = LuaStackUtil.ToRawValue(lua, 3, t);
			// UnityEngine.Debug.Log("FFI_SetPropValue finfo:"+finfo+" t:"+t+" value:"+value);
			finfo.SetValue( inst, value );
			return 0;
		}

		private static int FFI_GetProp( ILuaState lua )
		{
			var t = (Type)lua.ToUserData(1);
			var name = lua.ToString(2);
			var pinfo = t.GetProperty( name,
				BindingFlags.Instance |
				BindingFlags.Public );
			// UnityEngine.Debug.Log("FFI_GetProp t:"+t+" name:"+name+" pinfo:"+pinfo);
			if( pinfo == null )
				throw new Exception("GetProperty failed:"+name);
			lua.PushLightUserData(pinfo);
			return 1;
		}

		private static int FFI_GetStaticProp( ILuaState lua )
		{
			var t = (Type)lua.ToUserData(1);
			var name = lua.ToString(2);
			var pinfo = t.GetProperty( name,
				BindingFlags.Static |
				BindingFlags.Public );
			// UnityEngine.Debug.Log("FFI_GetStaticProp t:"+t+" name:"+name+" pinfo:"+pinfo);
			if( pinfo == null )
				throw new Exception("GetProperty failed:"+name);
			lua.PushLightUserData(pinfo);
			return 1;
		}

		private static int FFI_GetPropValue( ILuaState lua )
		{
			var pinfo = (PropertyInfo)lua.ToUserData(1);
			var inst = lua.ToUserData(2);
			var returnType = (Type)lua.ToUserData(3);
			var value = pinfo.GetValue( inst, null );
			LuaStackUtil.PushRawValue( lua, value, returnType );
			return 1;
		}

		private static int FFI_SetPropValue( ILuaState lua )
		{
			var pinfo = (PropertyInfo)lua.ToUserData(1);
			var inst = lua.ToUserData(2);
			var t = (Type)lua.ToUserData(4);
			var value = LuaStackUtil.ToRawValue(lua, 3, t);
			// UnityEngine.Debug.Log("FFI_SetPropValue pinfo:"+pinfo+" t:"+t+" value:"+value);
			pinfo.SetValue( inst, value, null );
			return 0;
		}

		// private static int FFI_CallConstructor( ILuaState lua )
		// {
		// 	var ffiConstructor = (FFIConstructorInfo)lua.ToUserData(1);
		// 	return ffiConstructor.Call( lua );
		// }

//////////////////////////////////////////////////////////////////////

		private static List<Assembly> 	AssemblyList;
		private static List<string>		UsingList;

		static LuaFFILib()
		{
			AssemblyList 	= new List<Assembly>();
			UsingList		= new List<string>();
		}

		private static Type FindTypeInAllAssemblies(string typename)
		{
			Type result = null;
			foreach( var assembly in AssemblyList )
			{
				var t = assembly.GetType( typename );
				if( t != null )
				{
					if(result == null) {
						result = t;
					}
					else {
						// TODO: handle error: ambiguous type name
					}
				}
			}
			return result;
		}

		private static Type GetType( string typename )
		{
			var result = FindTypeInAllAssemblies( typename );
			if( result != null )
				return result;

			foreach( var ns in UsingList )
			{
				var fullname = ns + "." + typename;
				result = FindTypeInAllAssemblies( fullname );
				if( result != null )
					return result;
			}

			return null;
		}

		static class LuaStackUtil
		{
			public static int PushRawValue( ILuaState lua, object o, Type t )
			{
				switch( t.FullName )
				{
					case "System.Boolean": {
						lua.PushBoolean( (bool)o );
						return 1;
					}

					case "System.Char": {
						lua.PushString( ((char)o).ToString() );
						return 1;
					}

					case "System.Byte": {
						lua.PushNumber( (byte)o );
						return 1;
					}

					case "System.SByte": {
						lua.PushNumber( (sbyte)o );
						return 1;
					}

					case "System.Int16": {
						lua.PushNumber( (short)o );
						return 1;
					}

					case "System.UInt16": {
						lua.PushNumber( (ushort)o );
						return 1;
					}

					case "System.Int32": {
						lua.PushNumber( (int)o );
						return 1;
					}

					case "System.UInt32": {
						lua.PushNumber( (uint)o );
						return 1;
					}

					case "System.Int64": {
						throw new NotImplementedException();
					}

					case "System.UInt64": {
						lua.PushUInt64( (ulong)o );
						return 1;
					}

					case "System.Single": {
						lua.PushNumber( (float)o );
						return 1;
					}

					case "System.Double": {
						lua.PushNumber( (double) o );
						return 1;
					}

					case "System.Decimal": {
						lua.PushLightUserData( (decimal)o );
						return 1;
					}

					case "System.String": {
						lua.PushString( o as string );
						return 1;
					}

					case "System.Object": {
						lua.PushLightUserData( (object)o );
						return 1;
					}

					default: {
						// UnityEngine.Debug.Log( "==========" +
						// 	" Name:" + t.Name +
						// 	" FullName:" + t.FullName );

						lua.PushLightUserData( o );
						return 1;
					}
				}
			}

			public static object ToRawValue( ILuaState lua, int index, Type t )
			{
				switch( t.FullName )
				{
					case "System.Boolean":
						return lua.ToBoolean( index );

					case "System.Char": {
						var s = lua.ToString( index );
						if( string.IsNullOrEmpty( s ) )
							return null;
						return s[0];
					}

					case "System.Byte":
						return (byte)lua.ToNumber( index );

					case "System.SByte":
						return (sbyte)lua.ToNumber( index );

					case "System.Int16":
						return (short)lua.ToNumber( index );

					case "System.UInt16":
						return (ushort)lua.ToNumber( index );

					case "System.Int32":
						return (int)lua.ToNumber( index );

					case "System.UInt32":
						return (uint)lua.ToNumber( index );

					case "System.Int64":
						return (Int64)lua.ToUserData( index );

					case "System.UInt64":
						return (UInt64)lua.ToUserData( index );

					case "System.Single":
						return (float)lua.ToNumber( index );

					case "System.Double":
						return (double)lua.ToNumber( index );

					case "System.Decimal":
						return (decimal)lua.ToUserData( index );

					case "System.String":
						return lua.ToString( index );

					case "System.Object":
						return (object)lua.ToUserData( index );

					default: {
						var u = lua.ToUserData(index);
						if( u == null )
						{
							// UnityEngine.Debug.Log( "==========" +
							// 	" Name:" + t.Name +
							// 	" FullName:" + t.FullName );
							return null;
						}
						else return u;
					}
				}
			}
		}

		abstract class FFIMethodBase
		{
			public FFIMethodBase( MethodBase minfo )
			{
				Method = minfo;

				var parameters = minfo.GetParameters();
				ParameterTypes = new Type[parameters.Length];
				for( int i=0; i<parameters.Length; ++i )
				{
					ParameterTypes[i] = parameters[i].ParameterType;
					// UnityEngine.Debug.Log( string.Format(
					// 	"&&&& PARAMETER {0} {1}", i, ParameterTypes[i] ) );
				}
			}

			public int Call( ILuaState lua )
			{
				const int firstParamPos = 3;
				int n = lua.GetTop();
				var inst  = lua.ToUserData(2);
				// UnityEngine.Debug.Log(string.Format(
				// 	"**** inst {0}", inst
				// 	));
				int nparam = n - firstParamPos + 1;
				var parameters = new object[nparam];
				for( int i=0; i<nparam; ++i )
				{
					// var par = lua.ToObject(firstParamPos + i);
					// // UnityEngine.Debug.Log(string.Format(
					// // 	"**** {0} {1} {2} {3} {4}",
					// // 	firstParamPos+i, par, n, i, ParameterTypes.Length
					// // 	));
					// var partype = ParameterTypes[i];
					// parameters[i] = ConvertParameter(par, partype);
					// // UnityEngine.Debug.Log(string.Format(
					// // 	"parameters[{0}] = {1}" +
					// // 	"\npar: {2}\npartype: {3}",
					// // 	i, parameters[i], par, partype));
					
					var index = firstParamPos + i;
					var partype = ParameterTypes[i];
					parameters[i] = LuaStackUtil.ToRawValue(lua, index, partype);
				}

				var r = Method.Invoke( inst, parameters );
				return PushReturnValue( lua, r );
			}

			private MethodBase 	Method;
			private Type[] 		ParameterTypes;

			// private object ConvertParameter( object par, Type partype )
			// {
			// 	switch( partype.FullName )
			// 	{
			// 		case "System.String": {
			// 			var s = par as LuaString;
			// 			return (s != null) ? s.Value : null;
			// 		}

			// 		case "System.Single": {
			// 			var n = par as LuaNumber;
			// 			return (n != null) ? n.Value : 0.0;
			// 		}

			// 		default: {
			// 			var u = par as LuaLightUserData;
			// 			if( u == null )
			// 			{
			// 				UnityEngine.Debug.Log( "==========" +
			// 					" Name:" + partype.Name +
			// 					" FullName:" + partype.FullName );
			// 				return null;
			// 			}
			// 			else return u.Value;
			// 		}
			// 	}
			// }

			protected virtual int PushReturnValue( ILuaState lua, object o )
			{
				return 0;
			}
		}

		class FFIMethodInfo : FFIMethodBase
		{
			public FFIMethodInfo( MethodInfo minfo ) : base( minfo )
			{
				ReturnType = minfo.ReturnParameter.ParameterType;
			}

			private Type		ReturnType;

			protected override int PushReturnValue( ILuaState lua, object o )
			{
				return LuaStackUtil.PushRawValue( lua, o, ReturnType );
				// switch( ReturnType.FullName )
				// {
				// 	case "System.String": {
				// 		lua.PushString( o as string );
				// 		return 1;
				// 	}

				// 	case "System.Single": {
				// 		lua.PushNumber( (float)o );
				// 		return 1;
				// 	}

				// 	default: {
				// 		UnityEngine.Debug.Log( "==========" +
				// 			" Name:" + ReturnType.Name +
				// 			" FullName:" + ReturnType.FullName );

				// 		lua.PushLightUserData( o );
				// 		return 1;
				// 	}
				// }
			}
		}

		class FFIConstructorInfo : FFIMethodBase
		{
			public FFIConstructorInfo( ConstructorInfo cinfo ) : base( cinfo )
			{
			}

			protected override int PushReturnValue( ILuaState lua, object o )
			{
				lua.PushLightUserData( o );
				return 1;
			}
		}

//////////////////////////////////////////////////////////////////////
///
/// SIGNATURE PARSER
///
//////////////////////////////////////////////////////////////////////

		class FuncSignature
		{
			public string 		FuncName;
			public string 		ReturnType;
			public string[]	 	ParameterTypes;
		}

		class FuncSignatureParser
		{
			public static FuncSignature Parse(
				ILuaState lua, string signature )
			{
				var loadinfo = new StringLoadInfo( signature );

				var parser = new FuncSignatureParser();
				parser.Lexer = new LLex( lua, loadinfo, signature );
				parser.Result = new FuncSignature();

				return parser.parse( signature );
			}

			private LLex			Lexer;
			private FuncSignature	Result;

			private FuncSignature parse( string signature )
			{
				Lexer.Next(); // read first token
				FuncSignature();
				return Result;
			}

			private void FuncSignature()
			{
				var s1 = TypeName();
				var s2 = TypeName();
				if( String.IsNullOrEmpty(s2) ) {
					if( String.IsNullOrEmpty(s1) ) {
						Lexer.SyntaxError("function name expected");
					}
					else {
						Result.ReturnType = null;
						Result.FuncName = s1;
					}
				}
				else {
					Result.ReturnType = s1;
					Result.FuncName = s2;
				}
				// ReturnType();
				// FuncName();
				FuncArgs();
				if( Lexer.Token.TokenType != (int)TK.EOS )
				{
					Lexer.SyntaxError("redundant tail characters:" + Lexer.Token);
				}
			}

			private string TypeName()
			{
				var sb = new StringBuilder();
				while( Lexer.Token.TokenType == (int)TK.NAME )
				{
					sb.Append( CheckName() );

					if( !TestNext( (int)'.' ) )
						break;

					sb.Append( '.' );
				}
				return sb.ToString();
			}

			private void ReturnType()
			{
				if( Lexer.Token.TokenType == (int)TK.NAME )
				{
					var t = Lexer.Token as NameToken;
					if( t != null )
					{
						Result.ReturnType = t.SemInfo;
						Lexer.Next();
					}
				}

				Lexer.SyntaxError("return type expected");
			}

			private void FuncName()
			{
				if( Lexer.Token.TokenType == (int)TK.NAME )
				{
					var t = Lexer.Token as NameToken;
					if( t != null )
					{
						Result.FuncName = t.SemInfo;
						Lexer.Next();
					}
				}

				Lexer.SyntaxError("function name expected");
			}

			private string CheckName()
			{
				var t = Lexer.Token as NameToken;
				string name = t.SemInfo;
				Lexer.Next();
				return name;
			}

			private void TypeList()
			{
				var typelist = new List<string>();
				while( Lexer.Token.TokenType == (int)TK.NAME ) {
					typelist.Add( CheckName() );
					if( ! TestNext( (int)',' ) )
						break;
				}
				Result.ParameterTypes = typelist.ToArray();
			}

			private void FuncArgs()
			{
				if( Lexer.Token.TokenType == (int)'(' ) {
					var line = Lexer.LineNumber;
					Lexer.Next();
					if( TestNext( (int)')' ) ) {
						Result.ParameterTypes = new string[0];
						return;
					}

					TypeList();
					CheckMatch( (int)')', (int)'(', line );
				}
				// else {
				// 	Lexer.SyntaxError( "function arguments expected" );
				// }
			}

			private bool TestNext( int tokenType )
			{
				if( Lexer.Token.TokenType == tokenType ) {
					Lexer.Next();
					return true;
				}
				else return false;
			}

			private void ErrorExpected( int token )
			{
				Lexer.SyntaxError( string.Format( "{0} expected",
					((char)token).ToString() ) );
			}

			private void CheckMatch( int what, int who, int where )
			{
				if( !TestNext( what ) ) {
					if( where == Lexer.LineNumber )
						ErrorExpected( what );
					else
						Lexer.SyntaxError( string.Format(
							"{0} expected (to close {1} at line {2})",
							((char)what).ToString(),
							((char)who).ToString(),
							where ) );
				}
			}

			// private void Check( int tokenType )
			// {
			// 	if( Lexer.Token.TokenType != tokenType )
			// 		ErrorExpected( tokenType );
			// }

			// private void CheckNext( int tokenType )
			// {
			// 	Check( tokenType );
			// 	Lexer.Next();
			// }
		}

	}

}

