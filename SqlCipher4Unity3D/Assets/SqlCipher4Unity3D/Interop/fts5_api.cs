using System;

namespace unity.libcipher.Interop
{
    public partial struct fts5_api
    {
        public int iVersion;

        [NativeTypeName("int (*)(fts5_api *, const char *, void *, fts5_tokenizer *, void (*)(void *))")]
        public IntPtr xCreateTokenizer;

        [NativeTypeName("int (*)(fts5_api *, const char *, void **, fts5_tokenizer *)")]
        public IntPtr xFindTokenizer;

        [NativeTypeName("int (*)(fts5_api *, const char *, void *, fts5_extension_function, void (*)(void *))")]
        public IntPtr xCreateFunction;
    }
}
