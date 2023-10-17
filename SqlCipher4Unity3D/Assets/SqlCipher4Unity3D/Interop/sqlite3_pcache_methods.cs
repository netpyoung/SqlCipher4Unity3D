using System;

namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_pcache_methods
    {
        public void* pArg;

        [NativeTypeName("int (*)(void *)")]
        public IntPtr xInit;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr xShutdown;

        [NativeTypeName("sqlite3_pcache *(*)(int, int)")]
        public IntPtr xCreate;

        [NativeTypeName("void (*)(sqlite3_pcache *, int)")]
        public IntPtr xCachesize;

        [NativeTypeName("int (*)(sqlite3_pcache *)")]
        public IntPtr xPagecount;

        [NativeTypeName("void *(*)(sqlite3_pcache *, unsigned int, int)")]
        public IntPtr xFetch;

        [NativeTypeName("void (*)(sqlite3_pcache *, void *, int)")]
        public IntPtr xUnpin;

        [NativeTypeName("void (*)(sqlite3_pcache *, void *, unsigned int, unsigned int)")]
        public IntPtr xRekey;

        [NativeTypeName("void (*)(sqlite3_pcache *, unsigned int)")]
        public IntPtr xTruncate;

        [NativeTypeName("void (*)(sqlite3_pcache *)")]
        public IntPtr xDestroy;
    }
}
