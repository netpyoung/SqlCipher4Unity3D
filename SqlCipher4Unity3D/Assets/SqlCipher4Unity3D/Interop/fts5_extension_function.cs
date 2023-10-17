using System.Runtime.InteropServices;

namespace unity.libcipher.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void fts5_extension_function([NativeTypeName("const Fts5ExtensionApi *")] Fts5ExtensionApi* pApi, Fts5Context* pFts, sqlite3_context* pCtx, int nVal, sqlite3_value** apVal);
}
