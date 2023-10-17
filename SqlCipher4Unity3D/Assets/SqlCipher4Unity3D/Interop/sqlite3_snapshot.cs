namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_snapshot
    {
        [NativeTypeName("unsigned char[48]")]
        public fixed byte hidden[48];
    }
}
