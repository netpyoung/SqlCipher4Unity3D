using System.Runtime.InteropServices;

namespace unity.libcipher.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void sqlite3_syscall_ptr();
}
