using System;

namespace unity.libcipher.Interop
{
    public partial struct sqlite3_io_methods
    {
        public int iVersion;

        [NativeTypeName("int (*)(sqlite3_file *)")]
        public IntPtr xClose;

        [NativeTypeName("int (*)(sqlite3_file *, void *, int, sqlite3_int64)")]
        public IntPtr xRead;

        [NativeTypeName("int (*)(sqlite3_file *, const void *, int, sqlite3_int64)")]
        public IntPtr xWrite;

        [NativeTypeName("int (*)(sqlite3_file *, sqlite3_int64)")]
        public IntPtr xTruncate;

        [NativeTypeName("int (*)(sqlite3_file *, int)")]
        public IntPtr xSync;

        [NativeTypeName("int (*)(sqlite3_file *, sqlite3_int64 *)")]
        public IntPtr xFileSize;

        [NativeTypeName("int (*)(sqlite3_file *, int)")]
        public IntPtr xLock;

        [NativeTypeName("int (*)(sqlite3_file *, int)")]
        public IntPtr xUnlock;

        [NativeTypeName("int (*)(sqlite3_file *, int *)")]
        public IntPtr xCheckReservedLock;

        [NativeTypeName("int (*)(sqlite3_file *, int, void *)")]
        public IntPtr xFileControl;

        [NativeTypeName("int (*)(sqlite3_file *)")]
        public IntPtr xSectorSize;

        [NativeTypeName("int (*)(sqlite3_file *)")]
        public IntPtr xDeviceCharacteristics;

        [NativeTypeName("int (*)(sqlite3_file *, int, int, int, volatile void **)")]
        public IntPtr xShmMap;

        [NativeTypeName("int (*)(sqlite3_file *, int, int, int)")]
        public IntPtr xShmLock;

        [NativeTypeName("void (*)(sqlite3_file *)")]
        public IntPtr xShmBarrier;

        [NativeTypeName("int (*)(sqlite3_file *, int)")]
        public IntPtr xShmUnmap;

        [NativeTypeName("int (*)(sqlite3_file *, sqlite3_int64, int, void **)")]
        public IntPtr xFetch;

        [NativeTypeName("int (*)(sqlite3_file *, sqlite3_int64, void *)")]
        public IntPtr xUnfetch;
    }
}
