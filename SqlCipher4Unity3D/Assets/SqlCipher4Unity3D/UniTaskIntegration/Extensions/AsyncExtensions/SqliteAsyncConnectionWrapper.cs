namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.AsyncExtensions
{
    using System;
    using System.Reflection;
    using SqlCipher4Unity3D;

    public static class SqliteAsyncConnectionWrapper
    {
        private static readonly MethodInfo GetConnectionMethodInfo = typeof(SQLiteAsyncConnection).GetTypeInfo().GetDeclaredMethod("GetConnection");

        private static SQLiteConnectionWithLock GetConnectionWithLock(SQLiteAsyncConnection asyncConnection)
        {
            return (SQLiteConnectionWithLock) GetConnectionMethodInfo.Invoke(asyncConnection, null);
        }

        public static SQLiteConnectionWithLock Lock(SQLiteAsyncConnection asyncConnection)
        {
            return GetConnectionWithLock(asyncConnection);
        }
    }

    public static class SqliteConnectionExtensions
    {
        public static IDisposable Lock(this SQLiteConnectionWithLock connection)
        {
            var lockMethod = connection.GetType().GetTypeInfo().GetDeclaredMethod("Lock");
            return (IDisposable)lockMethod.Invoke(connection, null);
        }
    }
}