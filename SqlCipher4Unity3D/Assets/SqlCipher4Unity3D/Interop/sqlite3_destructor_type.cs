using System.Runtime.InteropServices;

namespace unity.libcipher.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void sqlite3_destructor_type(void* param0);
}
