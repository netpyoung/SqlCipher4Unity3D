using System;

namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_vfs
    {
        public int iVersion;

        public int szOsFile;

        public int mxPathname;

        public sqlite3_vfs* pNext;

        [NativeTypeName("const char *")]
        public sbyte* zName;

        public void* pAppData;

        [NativeTypeName("int (*)(sqlite3_vfs *, sqlite3_filename, sqlite3_file *, int, int *)")]
        public IntPtr xOpen;

        [NativeTypeName("int (*)(sqlite3_vfs *, const char *, int)")]
        public IntPtr xDelete;

        [NativeTypeName("int (*)(sqlite3_vfs *, const char *, int, int *)")]
        public IntPtr xAccess;

        [NativeTypeName("int (*)(sqlite3_vfs *, const char *, int, char *)")]
        public IntPtr xFullPathname;

        [NativeTypeName("void *(*)(sqlite3_vfs *, const char *)")]
        public IntPtr xDlOpen;

        [NativeTypeName("void (*)(sqlite3_vfs *, int, char *)")]
        public IntPtr xDlError;

        [NativeTypeName("void (*(*)(sqlite3_vfs *, void *, const char *))()")]
        public IntPtr xDlSym;

        [NativeTypeName("void (*)(sqlite3_vfs *, void *)")]
        public IntPtr xDlClose;

        [NativeTypeName("int (*)(sqlite3_vfs *, int, char *)")]
        public IntPtr xRandomness;

        [NativeTypeName("int (*)(sqlite3_vfs *, int)")]
        public IntPtr xSleep;

        [NativeTypeName("int (*)(sqlite3_vfs *, double *)")]
        public IntPtr xCurrentTime;

        [NativeTypeName("int (*)(sqlite3_vfs *, int, char *)")]
        public IntPtr xGetLastError;

        [NativeTypeName("int (*)(sqlite3_vfs *, sqlite3_int64 *)")]
        public IntPtr xCurrentTimeInt64;

        [NativeTypeName("int (*)(sqlite3_vfs *, const char *, sqlite3_syscall_ptr)")]
        public IntPtr xSetSystemCall;

        [NativeTypeName("sqlite3_syscall_ptr (*)(sqlite3_vfs *, const char *)")]
        public IntPtr xGetSystemCall;

        [NativeTypeName("const char *(*)(sqlite3_vfs *, const char *)")]
        public IntPtr xNextSystemCall;
    }
}
