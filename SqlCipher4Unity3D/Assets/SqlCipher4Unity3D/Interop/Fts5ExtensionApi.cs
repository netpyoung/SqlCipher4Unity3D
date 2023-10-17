using System;

namespace unity.libcipher.Interop
{
    public partial struct Fts5ExtensionApi
    {
        public int iVersion;

        [NativeTypeName("void *(*)(Fts5Context *)")]
        public IntPtr xUserData;

        [NativeTypeName("int (*)(Fts5Context *)")]
        public IntPtr xColumnCount;

        [NativeTypeName("int (*)(Fts5Context *, sqlite3_int64 *)")]
        public IntPtr xRowCount;

        [NativeTypeName("int (*)(Fts5Context *, int, sqlite3_int64 *)")]
        public IntPtr xColumnTotalSize;

        [NativeTypeName("int (*)(Fts5Context *, const char *, int, void *, int (*)(void *, int, const char *, int, int, int))")]
        public IntPtr xTokenize;

        [NativeTypeName("int (*)(Fts5Context *)")]
        public IntPtr xPhraseCount;

        [NativeTypeName("int (*)(Fts5Context *, int)")]
        public IntPtr xPhraseSize;

        [NativeTypeName("int (*)(Fts5Context *, int *)")]
        public IntPtr xInstCount;

        [NativeTypeName("int (*)(Fts5Context *, int, int *, int *, int *)")]
        public IntPtr xInst;

        [NativeTypeName("sqlite3_int64 (*)(Fts5Context *)")]
        public IntPtr xRowid;

        [NativeTypeName("int (*)(Fts5Context *, int, const char **, int *)")]
        public IntPtr xColumnText;

        [NativeTypeName("int (*)(Fts5Context *, int, int *)")]
        public IntPtr xColumnSize;

        [NativeTypeName("int (*)(Fts5Context *, int, void *, int (*)(const Fts5ExtensionApi *, Fts5Context *, void *))")]
        public IntPtr xQueryPhrase;

        [NativeTypeName("int (*)(Fts5Context *, void *, void (*)(void *))")]
        public IntPtr xSetAuxdata;

        [NativeTypeName("void *(*)(Fts5Context *, int)")]
        public IntPtr xGetAuxdata;

        [NativeTypeName("int (*)(Fts5Context *, int, Fts5PhraseIter *, int *, int *)")]
        public IntPtr xPhraseFirst;

        [NativeTypeName("void (*)(Fts5Context *, Fts5PhraseIter *, int *, int *)")]
        public IntPtr xPhraseNext;

        [NativeTypeName("int (*)(Fts5Context *, int, Fts5PhraseIter *, int *)")]
        public IntPtr xPhraseFirstColumn;

        [NativeTypeName("void (*)(Fts5Context *, Fts5PhraseIter *, int *)")]
        public IntPtr xPhraseNextColumn;
    }
}
