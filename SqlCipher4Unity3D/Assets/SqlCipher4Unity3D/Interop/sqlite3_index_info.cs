namespace unity.libcipher.Interop
{
    public unsafe partial struct sqlite3_index_info
    {
        public int nConstraint;

        [NativeTypeName("struct sqlite3_index_constraint *")]
        public sqlite3_index_constraint* aConstraint;

        public int nOrderBy;

        [NativeTypeName("struct sqlite3_index_orderby *")]
        public sqlite3_index_orderby* aOrderBy;

        [NativeTypeName("struct sqlite3_index_constraint_usage *")]
        public sqlite3_index_constraint_usage* aConstraintUsage;

        public int idxNum;

        [NativeTypeName("char *")]
        public sbyte* idxStr;

        public int needToFreeIdxStr;

        public int orderByConsumed;

        public double estimatedCost;

        [NativeTypeName("sqlite3_int64")]
        public long estimatedRows;

        public int idxFlags;

        [NativeTypeName("sqlite3_uint64")]
        public ulong colUsed;

        public partial struct sqlite3_index_constraint
        {
            public int iColumn;

            [NativeTypeName("unsigned char")]
            public byte op;

            [NativeTypeName("unsigned char")]
            public byte usable;

            public int iTermOffset;
        }

        public partial struct sqlite3_index_orderby
        {
            public int iColumn;

            [NativeTypeName("unsigned char")]
            public byte desc;
        }

        public partial struct sqlite3_index_constraint_usage
        {
            public int argvIndex;

            [NativeTypeName("unsigned char")]
            public byte omit;
        }
    }
}
