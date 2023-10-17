using System;

namespace unity.libcipher.Interop
{
    public partial struct sqlite3_mutex_methods
    {
        [NativeTypeName("int (*)()")]
        public IntPtr xMutexInit;

        [NativeTypeName("int (*)()")]
        public IntPtr xMutexEnd;

        [NativeTypeName("sqlite3_mutex *(*)(int)")]
        public IntPtr xMutexAlloc;

        [NativeTypeName("void (*)(sqlite3_mutex *)")]
        public IntPtr xMutexFree;

        [NativeTypeName("void (*)(sqlite3_mutex *)")]
        public IntPtr xMutexEnter;

        [NativeTypeName("int (*)(sqlite3_mutex *)")]
        public IntPtr xMutexTry;

        [NativeTypeName("void (*)(sqlite3_mutex *)")]
        public IntPtr xMutexLeave;

        [NativeTypeName("int (*)(sqlite3_mutex *)")]
        public IntPtr xMutexHeld;

        [NativeTypeName("int (*)(sqlite3_mutex *)")]
        public IntPtr xMutexNotheld;
    }
}
