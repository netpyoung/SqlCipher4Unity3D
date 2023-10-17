using System;

namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_mem_methods
    {
        [NativeTypeName("void *(*)(int)")]
        public IntPtr xMalloc;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr xFree;

        [NativeTypeName("void *(*)(void *, int)")]
        public IntPtr xRealloc;

        [NativeTypeName("int (*)(void *)")]
        public IntPtr xSize;

        [NativeTypeName("int (*)(int)")]
        public IntPtr xRoundup;

        [NativeTypeName("int (*)(void *)")]
        public IntPtr xInit;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr xShutdown;

        public void* pAppData;
    }
}
