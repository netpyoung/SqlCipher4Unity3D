namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_vtab
    {
        [NativeTypeName("const sqlite3_module *")]
        public sqlite3_module* pModule;

        public int nRef;

        [NativeTypeName("char *")]
        public sbyte* zErrMsg;
    }
}
