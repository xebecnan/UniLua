namespace UniLua {
  public struct NameFuncPair {
    public string Name;
    public CSharpFunctionDelegate Func;

    public NameFuncPair(string name, CSharpFunctionDelegate func) {
      Name = name;
      Func = func;
    }
  }
}
