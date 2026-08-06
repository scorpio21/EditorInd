namespace IndLib;

// C1: resuelve el valor Int16 que se escribe para un campo Boolean.
// Si la casilla no cambió (current == (raw != 0)), se conserva el short
// crudo original (p. ej. 0x00FF) → round-trip byte-exacto.
// Si el usuario conmutó, se normaliza a -1 (true) o 0 (false).
public static class IndValueLogic
{
    public static short ResolveBoolean(bool current, short raw)
        => current == (raw != 0) ? raw : (short)(current ? -1 : 0);
}
