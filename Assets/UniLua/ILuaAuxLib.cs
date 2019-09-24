using System;

namespace UniLua {
  public interface ILuaAuxLib {
    void L_Where(int level);
    int L_Error(string fmt, params object[] args);
    void L_CheckStack(int size, string msg);
    void L_CheckAny(int narg);
    void L_CheckType(int index, LuaType t);
    double L_CheckNumber(int narg);
    UInt64 L_CheckUInt64(int narg);
    int L_CheckInteger(int narg);
    string L_CheckString(int narg);
    uint L_CheckUnsigned(int narg);
    void L_ArgCheck(bool cond, int narg, string extraMsg);
    int L_ArgError(int narg, string extraMsg);
    string L_TypeName(int index);

    string L_ToString(int index);
    bool L_GetMetaField(int index, string method);
    int L_GetSubTable(int index, string fname);

    void L_RequireF(string moduleName, CSharpFunctionDelegate openFunc, bool global);
    void L_OpenLibs();
    void L_NewLibTable(NameFuncPair[] define);
    void L_NewLib(NameFuncPair[] define);
    void L_SetFuncs(NameFuncPair[] define, int nup);

    T L_Opt<T>(Func<int, T> f, int n, T def);
    int L_OptInt(int narg, int def);
    string L_OptString(int narg, string def);
    bool L_CallMeta(int obj, string name);
    void L_Traceback(ILuaState otherLua, string msg, int level);
    int L_Len(int index);

    ThreadStatus L_LoadBuffer(string s, string name);
    ThreadStatus L_LoadBufferX(string s, string name, string mode);
    ThreadStatus L_LoadFile(string filename);
    ThreadStatus L_LoadFileX(string filename, string mode);

    ThreadStatus L_LoadString(string s);
    ThreadStatus L_LoadBytes(byte[] bytes, string name);
    ThreadStatus L_DoString(string s);
    ThreadStatus L_DoFile(string filename);


    string L_Gsub(string src, string pattern, string rep);

    // reference system
    int L_Ref(int t);
    void L_Unref(int t, int reference);
  }
}
