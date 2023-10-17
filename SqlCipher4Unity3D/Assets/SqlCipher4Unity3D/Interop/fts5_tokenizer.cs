using System;

namespace unity.libcipher.Interop
{
    public partial struct fts5_tokenizer
    {
        [NativeTypeName("int (*)(void *, const char **, int, Fts5Tokenizer **)")]
        public IntPtr xCreate;

        [NativeTypeName("void (*)(Fts5Tokenizer *)")]
        public IntPtr xDelete;

        [NativeTypeName("int (*)(Fts5Tokenizer *, void *, int, const char *, int, int (*)(void *, int, const char *, int, int, int))")]
        public IntPtr xTokenize;
    }
}
