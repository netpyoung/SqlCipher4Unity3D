using System;

namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_rtree_geometry
    {
        public void* pContext;

        public int nParam;

        [NativeTypeName("sqlite3_rtree_dbl *")]
        public double* aParam;

        public void* pUser;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr xDelUser;
    }
}
