namespace UniLua {
  public enum ExpKind {
    VVOID, /* no value */
    VNIL,
    VTRUE,
    VFALSE,
    VK, /* info = index of constant in `k' */
    VKNUM, /* nval = numerical value */
    VNONRELOC, /* info = result register */
    VLOCAL, /* info = local register */
    VUPVAL, /* info = index of upvalue in 'upvalues' */
    VINDEXED, /* t = table register/upvalue; idx = index R/K */
    VJMP, /* info = instruction pc */
    VRELOCABLE, /* info = instruction pc */
    VCALL, /* info = instruction pc */
    VVARARG /* info = instruction pc */
  }
}