namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_file
    {
        [NativeTypeName("const struct sqlite3_io_methods *")]
        public sqlite3_io_methods* pMethods;
    }
}
