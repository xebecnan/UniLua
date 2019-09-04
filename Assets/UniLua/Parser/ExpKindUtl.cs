namespace UniLua {
  public static class ExpKindUtl {
    public static bool VKIsVar(ExpKind k) {
      return ((int) ExpKind.VLOCAL <= (int) k &&
              (int) k <= (int) ExpKind.VINDEXED);
    }

    public static bool VKIsInReg(ExpKind k) {
      return k == ExpKind.VNONRELOC || k == ExpKind.VLOCAL;
    }
  }
}