using System.Runtime.InteropServices;

namespace unity.libcipher.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int sqlite3_callback(void* param0, int param1, [NativeTypeName("char **")] sbyte** param2, [NativeTypeName("char **")] sbyte** param3);
}
