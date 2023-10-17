using System;

namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_rtree_query_info
    {
        public void* pContext;

        public int nParam;

        [NativeTypeName("sqlite3_rtree_dbl *")]
        public double* aParam;

        public void* pUser;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr xDelUser;

        [NativeTypeName("sqlite3_rtree_dbl *")]
        public double* aCoord;

        [NativeTypeName("unsigned int *")]
        public uint* anQueue;

        public int nCoord;

        public int iLevel;

        public int mxLevel;

        [NativeTypeName("sqlite3_int64")]
        public long iRowid;

        [NativeTypeName("sqlite3_rtree_dbl")]
        public double rParentScore;

        public int eParentWithin;

        public int eWithin;

        [NativeTypeName("sqlite3_rtree_dbl")]
        public double rScore;

        public sqlite3_value** apSqlParam;
    }
}
