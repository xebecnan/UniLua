using System;

namespace UniLua {
  public interface ILuaAPI {
    LuaState NewThread();

    ThreadStatus Load(ILoadInfo loadinfo, string name, string mode);
    DumpStatus Dump(LuaWriter writeFunc);

    ThreadStatus GetContext(out int context);
    void Call(int numArgs, int numResults);

    void CallK(int numArgs, int numResults,
      int context, CSharpFunctionDelegate continueFunc);

    ThreadStatus PCall(int numArgs, int numResults, int errFunc);

    ThreadStatus PCallK(int numArgs, int numResults, int errFunc,
      int context, CSharpFunctionDelegate continueFunc);

    ThreadStatus Resume(ILuaState from, int numArgs);
    int Yield(int numResults);

    int YieldK(int numResults, int context, CSharpFunctionDelegate continueFunc);

    int AbsIndex(int index);
    int GetTop();
    void SetTop(int top);

    void Remove(int index);
    void Insert(int index);
    void Replace(int index);
    void Copy(int fromIndex, int toIndex);
    void XMove(ILuaState to, int n);

    bool CheckStack(int size);
    bool GetStack(int level, LuaDebug ar);
    int Error();

    int UpvalueIndex(int i);
    string GetUpvalue(int funcIndex, int n);
    string SetUpvalue(int funcIndex, int n);

    void CreateTable(int narray, int nrec);
    void NewTable();
    bool Next(int index);
    void RawGetI(int index, int n);
    void RawSetI(int index, int n);
    void RawGet(int index);
    void RawSet(int index);
    void GetField(int index, string key);
    void SetField(int index, string key);
    void GetTable(int index);
    void SetTable(int index);

    void Concat(int n);

    LuaType Type(int index);
    string TypeName(LuaType t);
    bool IsNil(int index);
    bool IsNone(int index);
    bool IsNoneOrNil(int index);
    bool IsString(int index);
    bool IsTable(int index);
    bool IsFunction(int index);

    bool Compare(int index1, int index2, LuaEq op);
    bool RawEqual(int index1, int index2);
    int RawLen(int index);
    void Len(int index);

    void PushNil();
    void PushBoolean(bool b);
    void PushNumber(double n);
    void PushInteger(int n);
    void PushUnsigned(uint n);
    string PushString(string s);
    void PushCSharpFunction(CSharpFunctionDelegate f);
    void PushCSharpClosure(CSharpFunctionDelegate f, int n);
    void PushValue(int index);
    void PushGlobalTable();
    void PushLightUserData(object o);
    void PushUInt64(UInt64 o);
    bool PushThread();

    void Pop(int n);

    bool GetMetaTable(int index);
    bool SetMetaTable(int index);

    void GetGlobal(string name);
    void SetGlobal(string name);

    string ToString(int index);
    double ToNumberX(int index, out bool isnum);
    double ToNumber(int index);
    int ToIntegerX(int index, out bool isnum);
    int ToInteger(int index);
    uint ToUnsignedX(int index, out bool isnum);
    uint ToUnsigned(int index);
    bool ToBoolean(int index);
    UInt64 ToUInt64(int index);
    UInt64 ToUInt64X(int index, out bool isnum);
    object ToObject(int index);
    object ToUserData(int index);
    ILuaState ToThread(int index);

    ThreadStatus Status { get; }

    string DebugGetInstructionHistory();
  }
}