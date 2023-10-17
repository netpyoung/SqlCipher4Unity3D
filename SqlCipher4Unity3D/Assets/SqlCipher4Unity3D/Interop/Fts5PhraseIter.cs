namespace unity.libcipher.Interop
{
    public unsafe partial struct Fts5PhraseIter
    {
        [NativeTypeName("const unsigned char *")]
        public byte* a;

        [NativeTypeName("const unsigned char *")]
        public byte* b;
    }
}
