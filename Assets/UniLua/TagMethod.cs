
namespace UniLua
{
	// grep `NoTagMethodFlags' if num of TMS >= 32
	internal enum TMS
	{
		TM_INDEX,
		TM_NEWINDEX,
		TM_GC,
		TM_MODE,
		TM_LEN,
		TM_EQ,
		TM_ADD,
		TM_SUB,
		TM_MUL,
		TM_DIV,
		TM_MOD,
		TM_POW,
		TM_UNM,
		TM_LT,
		TM_LE,
		TM_CONCAT,
		TM_CALL,
		TM_N		/* number of elements in the enum */
	}

	public partial class LuaState
	{
		private string GetTagMethodName( TMS tm )
		{
			switch( tm )
			{
				case TMS.TM_INDEX: 		return "__index";
				case TMS.TM_NEWINDEX: 	return "__newindex";
				case TMS.TM_GC: 		return "__gc";
				case TMS.TM_MODE: 		return "__mode";
				case TMS.TM_LEN: 		return "__len";
				case TMS.TM_EQ: 		return "__eq";
				case TMS.TM_ADD: 		return "__add";
				case TMS.TM_SUB: 		return "__sub";
				case TMS.TM_MUL: 		return "__mul";
				case TMS.TM_DIV: 		return "__div";
				case TMS.TM_MOD: 		return "__mod";
				case TMS.TM_POW: 		return "__pow";
				case TMS.TM_UNM: 		return "__unm";
				case TMS.TM_LT: 		return "__lt";
				case TMS.TM_LE: 		return "__le";
				case TMS.TM_CONCAT: 	return "__concat";
				case TMS.TM_CALL: 		return "__call";
				default: throw new System.NotImplementedException();
			}
		}

		private StkId T_GetTM( LuaTable mt, TMS tm )
		{
			if( mt == null )
				return null;

			var res = mt.GetStr( GetTagMethodName( tm ) );
			if(res.V.TtIsNil()) // no tag method?
			{
				// cache this fact
				mt.NoTagMethodFlags |= 1u << (int)tm;
				return null;
			}
			else
				return res;
		}

		private StkId T_GetTMByObj( ref TValue o, TMS tm )
		{
			LuaTable mt = null;

			switch( o.Tt )
			{
				case (int)LuaType.LUA_TTABLE:
				{
					var tbl = o.HValue();
					mt = tbl.MetaTable;
					break;
				}
				case (int)LuaType.LUA_TUSERDATA:
				{
					var ud = o.RawUValue();
					mt = ud.MetaTable;
					break;
				}
				default:
				{
					mt = G.MetaTables[o.Tt];
					break;
				}
			}
			return (mt != null)
				 ? mt.GetStr( GetTagMethodName( tm ) )
				 : TheNilValue;
		}

	}

}

