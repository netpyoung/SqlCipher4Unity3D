using System;

namespace unity.libcipher.Interop
{
    public partial struct sqlite3_module
    {
        public int iVersion;

        [NativeTypeName("int (*)(sqlite3 *, void *, int, const char *const *, sqlite3_vtab **, char **)")]
        public IntPtr xCreate;

        [NativeTypeName("int (*)(sqlite3 *, void *, int, const char *const *, sqlite3_vtab **, char **)")]
        public IntPtr xConnect;

        [NativeTypeName("int (*)(sqlite3_vtab *, sqlite3_index_info *)")]
        public IntPtr xBestIndex;

        [NativeTypeName("int (*)(sqlite3_vtab *)")]
        public IntPtr xDisconnect;

        [NativeTypeName("int (*)(sqlite3_vtab *)")]
        public IntPtr xDestroy;

        [NativeTypeName("int (*)(sqlite3_vtab *, sqlite3_vtab_cursor **)")]
        public IntPtr xOpen;

        [NativeTypeName("int (*)(sqlite3_vtab_cursor *)")]
        public IntPtr xClose;

        [NativeTypeName("int (*)(sqlite3_vtab_cursor *, int, const char *, int, sqlite3_value **)")]
        public IntPtr xFilter;

        [NativeTypeName("int (*)(sqlite3_vtab_cursor *)")]
        public IntPtr xNext;

        [NativeTypeName("int (*)(sqlite3_vtab_cursor *)")]
        public IntPtr xEof;

        [NativeTypeName("int (*)(sqlite3_vtab_cursor *, sqlite3_context *, int)")]
        public IntPtr xColumn;

        [NativeTypeName("int (*)(sqlite3_vtab_cursor *, sqlite3_int64 *)")]
        public IntPtr xRowid;

        [NativeTypeName("int (*)(sqlite3_vtab *, int, sqlite3_value **, sqlite3_int64 *)")]
        public IntPtr xUpdate;

        [NativeTypeName("int (*)(sqlite3_vtab *)")]
        public IntPtr xBegin;

        [NativeTypeName("int (*)(sqlite3_vtab *)")]
        public IntPtr xSync;

        [NativeTypeName("int (*)(sqlite3_vtab *)")]
        public IntPtr xCommit;

        [NativeTypeName("int (*)(sqlite3_vtab *)")]
        public IntPtr xRollback;

        [NativeTypeName("int (*)(sqlite3_vtab *, int, const char *, void (**)(sqlite3_context *, int, sqlite3_value **), void **)")]
        public IntPtr xFindFunction;

        [NativeTypeName("int (*)(sqlite3_vtab *, const char *)")]
        public IntPtr xRename;

        [NativeTypeName("int (*)(sqlite3_vtab *, int)")]
        public IntPtr xSavepoint;

        [NativeTypeName("int (*)(sqlite3_vtab *, int)")]
        public IntPtr xRelease;

        [NativeTypeName("int (*)(sqlite3_vtab *, int)")]
        public IntPtr xRollbackTo;

        [NativeTypeName("int (*)(const char *)")]
        public IntPtr xShadowName;
    }
}
