using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using SQLite.Attribute;
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;

namespace SqlCipher4Unity3D
{
    /// <summary>
    ///     Represents an open connection to a SQLite database.
    /// </summary>
    public class SQLiteConnection : IDisposable
    {
        internal static readonly Sqlite3DatabaseHandle NullHandle = default(Sqlite3DatabaseHandle);

        /// <summary>
        ///     Used to list some code that we want the MonoTouch linker
        ///     to see, but that we never want to actually execute.
        /// </summary>
        private static bool s_preserveDuringLinkMagic;

        private readonly Random _rand = new Random();

        private TimeSpan _busyTimeout;
        private long _elapsedMilliseconds;
        private Dictionary<string, TableMapping> _mappings;
        private bool _open;
        private Stopwatch _sw;
        private Dictionary<string, TableMapping> _tables;

        private int _transactionDepth;

        static SQLiteConnection()
        {
            if (s_preserveDuringLinkMagic)
            {
                ColumnInfo ti = new ColumnInfo();
                ti.Name = "magic";
            }
        }

        /// <summary>
        ///     Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        ///     Specifies the path to the database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        ///     Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        ///     absolutely do want to store them as Ticks in all new projects. The default of false is
        ///     only here for backwards compatibility. There is a *significant* speed advantage, with no
        ///     down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, string password = null, bool storeDateTimeAsTicks = false) : this(
            databasePath, password, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks)
        { }

        /// <summary>
        ///     Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name="databasePath">
        ///     Specifies the path to the database file.
        /// </param>
        /// <param name="storeDateTimeAsTicks">
        ///     Specifies whether to store DateTime properties as ticks (true) or strings (false). You
        ///     absolutely do want to store them as Ticks in all new projects. The default of false is
        ///     only here for backwards compatibility. There is a *significant* speed advantage, with no
        ///     down sides, when setting storeDateTimeAsTicks = true.
        /// </param>
        public SQLiteConnection(string databasePath, string password, SQLiteOpenFlags openFlags,
            bool storeDateTimeAsTicks = false)
        {
            if (string.IsNullOrEmpty(databasePath))
                throw new ArgumentException("Must be specified", "databasePath");

            this.DatabasePath = databasePath;

            Sqlite3DatabaseHandle handle;

            // open using the byte[]
            // in the case where the path may include Unicode
            // force open to using UTF-8 using sqlite3_open_v2
            byte[] databasePathAsBytes = GetNullTerminatedUtf8(this.DatabasePath);
            SQLite3.Result r = SQLite3.Open(databasePathAsBytes, out handle, (int)openFlags, IntPtr.Zero);

            this.Handle = handle;
            if (r != SQLite3.Result.OK)
                throw SQLiteException.New(r,
                    string.Format("Could not open database file: {0} ({1})", this.DatabasePath, r));

            if (!string.IsNullOrEmpty(password))
            {
                SQLite3.Result result = SQLite3.Key(handle, password, password.Length);
                if (result != SQLite3.Result.OK)
                    throw SQLiteException.New(r,
                        string.Format("Could not open database file: {0} ({1})", this.DatabasePath, r));
            }

            _open = true;

            this.StoreDateTimeAsTicks = storeDateTimeAsTicks;

            this.BusyTimeout = TimeSpan.FromSeconds(0.1);
        }

        public Sqlite3DatabaseHandle Handle { get; private set; }

        public string DatabasePath { get; private set; }

        public bool TimeExecution { get; set; }

        public bool Trace { get; set; }

        public bool StoreDateTimeAsTicks { get; private set; }

        /// <summary>
        ///     Sets a busy handler to sleep the specified amount of time when a table is locked.
        ///     The handler will sleep multiple times until a total time of <see cref="BusyTimeout" /> has accumulated.
        /// </summary>
        public TimeSpan BusyTimeout
        {
            get { return _busyTimeout; }
            set
            {
                _busyTimeout = value;
                if (this.Handle != NullHandle)
                    SQLite3.BusyTimeout(this.Handle, (int)_busyTimeout.TotalMilliseconds);
            }
        }

        /// <summary>
        ///     Returns the mappings from types to tables that the connection
        ///     currently understands.
        /// </summary>
        public IEnumerable<TableMapping> TableMappings
        {
            get { return _tables != null ? _tables.Values : Enumerable.Empty<TableMapping>(); }
        }

        /// <summary>
        ///     Whether <see cref="BeginTransaction" /> has been called and the database is waiting for a <see cref="Commit" />.
        /// </summary>
        public bool IsInTransaction
        {
            get { return _transactionDepth > 0; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void EnableLoadExtension(int onoff)
        {
            SQLite3.Result r = SQLite3.EnableLoadExtension(this.Handle, onoff);
            if (r != SQLite3.Result.OK)
            {
                string msg = SQLite3.GetErrmsg(this.Handle);
                throw SQLiteException.New(r, msg);
            }
        }

        private static byte[] GetNullTerminatedUtf8(string s)
        {
            int utf8Length = Encoding.UTF8.GetByteCount(s);
            byte[] bytes = new byte[utf8Length + 1];
            utf8Length = Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
            return bytes;
        }

        /// <summary>
        ///     Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name="type">
        ///     The type whose mapping to the database is returned.
        /// </param>
        /// <param name="createFlags">
        ///     Optional flags allowing implicit PK and indexes based on naming conventions
        /// </param>
        /// <returns>
        ///     The mapping represents the schema of the columns of the database and contains
        ///     methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            if (_mappings == null) _mappings = new Dictionary<string, TableMapping>();
            TableMapping map;
            if (!_mappings.TryGetValue(type.FullName, out map))
            {
                map = new TableMapping(type, createFlags);
                _mappings[type.FullName] = map;
            }

            return map;
        }

        /// <summary>
        ///     Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <returns>
        ///     The mapping represents the schema of the columns of the database and contains
        ///     methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        /// <summary>
        ///     Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        public int DropTable<T>()
        {
            TableMapping map = GetMapping(typeof(T));

            string query = string.Format("drop table if exists \"{0}\"", map.TableName);

            return Execute(query);
        }

        public int DropTable(Type t)
        {
            TableMapping map = GetMapping(t);

            string query = string.Format("drop table if exists \"{0}\"", map.TableName);

            return Execute(query);
        }

        /// <summary>
        ///     Executes a "create table if not exists" on the database. It also
        ///     creates any specified indexes on the columns of the table. It uses
        ///     a schema automatically generated from the specified type. You can
        ///     later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        ///     The number of entries added to the database schema.
        /// </returns>
        public int CreateTable<T>(CreateFlags createFlags = CreateFlags.None)
        {
            return CreateTable(typeof(T), createFlags);
        }

        /// <summary>
        ///     Executes a "create table if not exists" on the database. It also
        ///     creates any specified indexes on the columns of the table. It uses
        ///     a schema automatically generated from the specified type. You can
        ///     later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="ty">Type to reflect to a database table.</param>
        /// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>
        /// <returns>
        ///     The number of entries added to the database schema.
        /// </returns>
        public int CreateTable(Type ty, CreateFlags createFlags = CreateFlags.None)
        {
            if (_tables == null) _tables = new Dictionary<string, TableMapping>();
            TableMapping map;
            if (!_tables.TryGetValue(ty.FullName, out map))
            {
                map = GetMapping(ty, createFlags);
                _tables.Add(ty.FullName, map);
            }

            string query = "create table if not exists \"" + map.TableName + "\"(\n";

            IEnumerable<string> decls = map.Columns.Select(p => Orm.SqlDecl(p, this.StoreDateTimeAsTicks));
            string decl = string.Join(",\n", decls.ToArray());
            query += decl;
            query += ")";

            int count = Execute(query);

            if (count == 0) MigrateTable(map);

            Dictionary<string, IndexInfo> indexes = new Dictionary<string, IndexInfo>();
            foreach (TableMapping.Column c in map.Columns)
                foreach (IndexedAttribute i in c.Indices)
                {
                    string iname = i.Name ?? map.TableName + "_" + c.Name;
                    IndexInfo iinfo;
                    if (!indexes.TryGetValue(iname, out iinfo))
                    {
                        iinfo = new IndexInfo
                        {
                            IndexName = iname,
                            TableName = map.TableName,
                            Unique = i.Unique,
                            Columns = new List<IndexedColumn>()
                        };
                        indexes.Add(iname, iinfo);
                    }

                    if (i.Unique != iinfo.Unique)
                        throw new Exception(
                            "All the columns in an index must have the same value for their Unique property");

                    iinfo.Columns.Add(new IndexedColumn
                    {
                        Order = i.Order,
                        ColumnName = c.Name
                    });
                }

            foreach (string indexName in indexes.Keys)
            {
                IndexInfo index = indexes[indexName];
                string[] columns = index.Columns.OrderBy(i => i.Order).Select(i => i.ColumnName).ToArray();
                count += CreateIndex(indexName, index.TableName, columns, index.Unique);
            }

            return count;
        }

        /// <summary>
        ///     Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string[] columnNames, bool unique = false)
        {
            const string sqlFormat = "create {2} index if not exists \"{3}\" on \"{0}\"(\"{1}\")";
            string sql = string.Format(sqlFormat, tableName, string.Join("\", \"", columnNames), unique ? "unique" : "",
                indexName);
            return Execute(sql);
        }

        /// <summary>
        ///     Creates an index for the specified table and column.
        /// </summary>
        /// <param name="indexName">Name of the index to create</param>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string indexName, string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(indexName, tableName, new[] { columnName }, unique);
        }

        /// <summary>
        ///     Creates an index for the specified table and column.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnName">Name of the column to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string columnName, bool unique = false)
        {
            return CreateIndex(tableName + "_" + columnName, tableName, columnName, unique);
        }

        /// <summary>
        ///     Creates an index for the specified table and columns.
        /// </summary>
        /// <param name="tableName">Name of the database table</param>
        /// <param name="columnNames">An array of column names to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public int CreateIndex(string tableName, string[] columnNames, bool unique = false)
        {
            return CreateIndex(tableName + "_" + string.Join("_", columnNames), tableName, columnNames, unique);
        }

        /// <summary>
        ///     Creates an index for the specified object property.
        ///     e.g. CreateIndex<Client>(c => c.Name);
        /// </summary>
        /// <typeparam name="T">Type to reflect to a database table.</typeparam>
        /// <param name="property">Property to index</param>
        /// <param name="unique">Whether the index should be unique</param>
        public void CreateIndex<T>(Expression<Func<T, object>> property, bool unique = false)
        {
            MemberExpression mx;
            if (property.Body.NodeType == ExpressionType.Convert)
                mx = ((UnaryExpression)property.Body).Operand as MemberExpression;
            else
                mx = property.Body as MemberExpression;
            PropertyInfo propertyInfo = mx.Member as PropertyInfo;
            if (propertyInfo == null)
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");

            string propName = propertyInfo.Name;

            TableMapping map = GetMapping<T>();
            string colName = map.FindColumnWithPropertyName(propName).Name;

            CreateIndex(map.TableName, colName, unique);
        }

        public List<ColumnInfo> GetTableInfo(string tableName)
        {
            string query = "pragma table_info(\"" + tableName + "\")";
            return Query<ColumnInfo>(query);
        }

        private void MigrateTable(TableMapping map)
        {
            List<ColumnInfo> existingCols = GetTableInfo(map.TableName);

            List<TableMapping.Column> toBeAdded = new List<TableMapping.Column>();

            foreach (TableMapping.Column p in map.Columns)
            {
                bool found = false;
                foreach (ColumnInfo c in existingCols)
                {
                    found = string.Compare(p.Name, c.Name, StringComparison.OrdinalIgnoreCase) == 0;
                    if (found)
                        break;
                }

                if (!found) toBeAdded.Add(p);
            }

            foreach (TableMapping.Column p in toBeAdded)
            {
                string addCol = "alter table \"" + map.TableName + "\" add column " +
                                Orm.SqlDecl(p, this.StoreDateTimeAsTicks);
                Execute(addCol);
            }
        }

        /// <summary>
        ///     Creates a new SQLiteCommand. Can be overridden to provide a sub-class.
        /// </summary>
        /// <seealso cref="SQLiteCommand.OnInstanceCreated" />
        protected virtual SQLiteCommand NewCommand()
        {
            return new SQLiteCommand(this);
        }

        /// <summary>
        ///     Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        ///     in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        ///     A <see cref="SQLiteCommand" />
        /// </returns>
        public SQLiteCommand CreateCommand(string cmdText, params object[] ps)
        {
            if (!_open)
                throw SQLiteException.New(SQLite3.Result.Error, "Cannot create commands from unopened database");

            SQLiteCommand cmd = NewCommand();
            cmd.CommandText = cmdText;
            foreach (object o in ps) cmd.Bind(o);
            return cmd;
        }

        /// <summary>
        ///     Creates a new SQLiteCommand given the command text with arguments. Place a "[@:]VVV"
        ///     in the command text for each of the arguments.
        /// </summary>
        /// <param name="cmdText">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of "[@:]VVV" in the command text.
        /// </param>
        /// <returns>
        ///     A <see cref="SQLiteCommand" />
        /// </returns>
        public SQLiteCommand CreateCommand(string cmdText, Dictionary<string, object> args)
        {
            if (!this._open)
                throw SQLiteException.New(SQLite3.Result.Error, "Cannot create commands from unopened database");

            SQLiteCommand cmd = NewCommand();
            cmd.CommandText = cmdText;
            foreach (var kv in args)
            {
                cmd.Bind(kv.Key, kv.Value);
            }
            return cmd;
        }
	
        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     Use this method instead of Query when you don't expect rows back. Such cases include
        ///     INSERTs, UPDATEs, and DELETEs.
        ///     You can set the Trace or TimeExecution properties of the connection
        ///     to profile execution.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     The number of rows modified in the database as a result of this execution.
        /// </returns>
        public int Execute(string query, params object[] args)
        {
            SQLiteCommand cmd = CreateCommand(query, args);

            if (this.TimeExecution)
            {
                if (_sw == null) _sw = new Stopwatch();
                _sw.Reset();
                _sw.Start();
            }

            int r = cmd.ExecuteNonQuery();

            if (this.TimeExecution)
            {
                _sw.Stop();
                _elapsedMilliseconds += _sw.ElapsedMilliseconds;
                Debug.WriteLine("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds,
                    _elapsedMilliseconds / 1000.0);
            }

            return r;
        }

        public T ExecuteScalar<T>(string query, params object[] args)
        {
            SQLiteCommand cmd = CreateCommand(query, args);

            if (this.TimeExecution)
            {
                if (_sw == null) _sw = new Stopwatch();
                _sw.Reset();
                _sw.Start();
            }

            T r = cmd.ExecuteScalar<T>();

            if (this.TimeExecution)
            {
                _sw.Stop();
                _elapsedMilliseconds += _sw.ElapsedMilliseconds;
                Debug.WriteLine("Finished in {0} ms ({1:0.0} s total)", _sw.ElapsedMilliseconds,
                    _elapsedMilliseconds / 1000.0);
            }

            return r;
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the mapping automatically generated for
        ///     the given type.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string query, params object[] args) where T : new()
        {
            SQLiteCommand cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<T>();
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the mapping automatically generated for
        ///     the given type.
        /// </summary>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        ///     The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///     connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : new()
        {
            SQLiteCommand cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<T>();
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the specified mapping. This function is
        ///     only used by libraries in order to query the database via introspection. It is
        ///     normally not used.
        /// </summary>
        /// <param name="map">
        ///     A <see cref="TableMapping" /> to use to convert the resulting rows
        ///     into objects.
        /// </param>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<object> Query(TableMapping map, string query, params object[] args)
        {
            SQLiteCommand cmd = CreateCommand(query, args);
            return cmd.ExecuteQuery<object>(map);
        }

        /// <summary>
        ///     Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///     in the command text for each of the arguments and then executes that command.
        ///     It returns each row of the result using the specified mapping. This function is
        ///     only used by libraries in order to query the database via introspection. It is
        ///     normally not used.
        /// </summary>
        /// <param name="map">
        ///     A <see cref="TableMapping" /> to use to convert the resulting rows
        ///     into objects.
        /// </param>
        /// <param name="query">
        ///     The fully escaped SQL.
        /// </param>
        /// <param name="args">
        ///     Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///     An enumerable with one result for each row returned by the query.
        ///     The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///     connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable<object> DeferredQuery(TableMapping map, string query, params object[] args)
        {
            SQLiteCommand cmd = CreateCommand(query, args);
            return cmd.ExecuteDeferredQuery<object>(map);
        }

        /// <summary>
        ///     Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        ///     A queryable object that is able to translate Where, OrderBy, and Take
        ///     queries into native SQL.
        /// </returns>
        public TableQuery<T> Table<T>() where T : new()
        {
            return new TableQuery<T>(this);
        }

        /// <summary>
        ///     Attempts to retrieve an object with the given primary key from the table
        ///     associated with the specified type. Use of this method requires that
        ///     the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        ///     The primary key.
        /// </param>
        /// <returns>
        ///     The object with the given primary key. Throws a not found exception
        ///     if the object is not found.
        /// </returns>
        public T Get<T>(object pk) where T : new()
        {
            TableMapping map = GetMapping(typeof(T));
            return Query<T>(map.GetByPrimaryKeySql, pk).First();
        }

        /// <summary>
        ///     Attempts to retrieve the first object that matches the predicate from the table
        ///     associated with the specified type.
        /// </summary>
        /// <param name="predicate">
        ///     A predicate for which object to find.
        /// </param>
        /// <returns>
        ///     The object that matches the given predicate. Throws a not found exception
        ///     if the object is not found.
        /// </returns>
        public T Get<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            return Table<T>().Where(predicate).First();
        }

        /// <summary>
        ///     Attempts to retrieve an object with the given primary key from the table
        ///     associated with the specified type. Use of this method requires that
        ///     the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        ///     The primary key.
        /// </param>
        /// <returns>
        ///     The object with the given primary key or null
        ///     if the object is not found.
        /// </returns>
        public T Find<T>(object pk) where T : new()
        {
            TableMapping map = GetMapping(typeof(T));
            return Query<T>(map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to retrieve an object with the given primary key from the table
        ///     associated with the specified type. Use of this method requires that
        ///     the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="pk">
        ///     The primary key.
        /// </param>
        /// <param name="map">
        ///     The TableMapping used to identify the object type.
        /// </param>
        /// <returns>
        ///     The object with the given primary key or null
        ///     if the object is not found.
        /// </returns>
        public object Find(object pk, TableMapping map)
        {
            return Query(map, map.GetByPrimaryKeySql, pk).FirstOrDefault();
        }

        /// <summary>
        ///     Attempts to retrieve the first object that matches the predicate from the table
        ///     associated with the specified type.
        /// </summary>
        /// <param name="predicate">
        ///     A predicate for which object to find.
        /// </param>
        /// <returns>
        ///     The object that matches the given predicate or null
        ///     if the object is not found.
        /// </returns>
        public T Find<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            return Table<T>().Where(predicate).FirstOrDefault();
        }

        /// <summary>
        ///     Begins a new transaction. Call <see cref="Commit" /> to end the transaction.
        /// </summary>
        /// <example cref="System.InvalidOperationException">Throws if a transaction has already begun.</example>
        public void BeginTransaction()
        {
            // The BEGIN command only works if the transaction stack is empty, 
            //    or in other words if there are no pending transactions. 
            // If the transaction stack is not empty when the BEGIN command is invoked, 
            //    then the command fails with an error.
            // Rather than crash with an error, we will just ignore calls to BeginTransaction
            //    that would result in an error.
            if (Interlocked.CompareExchange(ref _transactionDepth, 1, 0) == 0)
                try
                {
                    Execute("begin transaction");
                }
                catch (Exception ex)
                {
                    SQLiteException sqlExp = ex as SQLiteException;
                    if (sqlExp != null)
                        switch (sqlExp.Result)
                        {
                            case SQLite3.Result.IOError:
                            case SQLite3.Result.Full:
                            case SQLite3.Result.Busy:
                            case SQLite3.Result.NoMem:
                            case SQLite3.Result.Interrupt:
                                RollbackTo(null, true);
                                break;
                        }
                    else
                        Interlocked.Decrement(ref _transactionDepth);

                    throw;
                }
            else
                throw new InvalidOperationException("Cannot begin a transaction while already in a transaction.");
        }

        /// <summary>
        ///     Creates a savepoint in the database at the current point in the transaction timeline.
        ///     Begins a new transaction if one is not in progress.
        ///     Call <see cref="RollbackTo" /> to undo transactions since the returned savepoint.
        ///     Call <see cref="Release" /> to commit transactions after the savepoint returned here.
        ///     Call <see cref="Commit" /> to end the transaction, committing all changes.
        /// </summary>
        /// <returns>A string naming the savepoint.</returns>
        public string SaveTransactionPoint()
        {
            int depth = Interlocked.Increment(ref _transactionDepth) - 1;
            string retVal = "S" + _rand.Next(short.MaxValue) + "D" + depth;

            try
            {
                Execute("savepoint " + retVal);
            }
            catch (Exception ex)
            {
                SQLiteException sqlExp = ex as SQLiteException;
                if (sqlExp != null)
                    switch (sqlExp.Result)
                    {
                        case SQLite3.Result.IOError:
                        case SQLite3.Result.Full:
                        case SQLite3.Result.Busy:
                        case SQLite3.Result.NoMem:
                        case SQLite3.Result.Interrupt:
                            RollbackTo(null, true);
                            break;
                    }
                else
                    Interlocked.Decrement(ref _transactionDepth);

                throw;
            }

            return retVal;
        }

        /// <summary>
        ///     Rolls back the transaction that was begun by <see cref="BeginTransaction" /> or <see cref="SaveTransactionPoint" />
        ///     .
        /// </summary>
        public void Rollback()
        {
            RollbackTo(null, false);
        }

        /// <summary>
        ///     Rolls back the savepoint created by <see cref="BeginTransaction" /> or SaveTransactionPoint.
        /// </summary>
        /// <param name="savepoint">
        ///     The name of the savepoint to roll back to, as returned by <see cref="SaveTransactionPoint" />.
        ///     If savepoint is null or empty, this method is equivalent to a call to <see cref="Rollback" />
        /// </param>
        public void RollbackTo(string savepoint)
        {
            RollbackTo(savepoint, false);
        }

        /// <summary>
        ///     Rolls back the transaction that was begun by <see cref="BeginTransaction" />.
        /// </summary>
        /// <param name="noThrow">true to avoid throwing exceptions, false otherwise</param>
        private void RollbackTo(string savepoint, bool noThrow)
        {
            // Rolling back without a TO clause rolls backs all transactions 
            //    and leaves the transaction stack empty.   
            try
            {
                if (string.IsNullOrEmpty(savepoint))
                {
                    if (Interlocked.Exchange(ref _transactionDepth, 0) > 0) Execute("rollback");
                }
                else
                {
                    DoSavePointExecute(savepoint, "rollback to ");
                }
            }
            catch (SQLiteException)
            {
                if (!noThrow)
                    throw;
            }

            // No need to rollback if there are no transactions open.
        }

        /// <summary>
        ///     Releases a savepoint returned from <see cref="SaveTransactionPoint" />.  Releasing a savepoint
        ///     makes changes since that savepoint permanent if the savepoint began the transaction,
        ///     or otherwise the changes are permanent pending a call to <see cref="Commit" />.
        ///     The RELEASE command is like a COMMIT for a SAVEPOINT.
        /// </summary>
        /// <param name="savepoint">
        ///     The name of the savepoint to release.  The string should be the result of a call to
        ///     <see cref="SaveTransactionPoint" />
        /// </param>
        public void Release(string savepoint)
        {
            DoSavePointExecute(savepoint, "release ");
        }

        private void DoSavePointExecute(string savepoint, string cmd)
        {
            // Validate the savepoint
            int firstLen = savepoint.IndexOf('D');
            if (firstLen >= 2 && savepoint.Length > firstLen + 1)
            {
                int depth;
                if (int.TryParse(savepoint.Substring(firstLen + 1), out depth))
                    if (0 <= depth && depth < _transactionDepth)
                    {
#if NETFX_CORE
						Volatile.Write (ref _transactionDepth, depth);
#elif SILVERLIGHT
						_transactionDepth = depth;
#else
                        Thread.VolatileWrite(ref _transactionDepth, depth);
#endif
                        Execute(cmd + savepoint);
                        return;
                    }
            }

            throw new ArgumentException(
                "savePoint is not valid, and should be the result of a call to SaveTransactionPoint.", "savePoint");
        }

        /// <summary>
        ///     Commits the transaction that was begun by <see cref="BeginTransaction" />.
        /// </summary>
        public void Commit()
        {
            if (Interlocked.Exchange(ref _transactionDepth, 0) != 0) Execute("commit");
            // Do nothing on a commit with no open transaction
        }

        /// <summary>
        ///     Executes
        ///     <param name="action">
        ///         within a (possibly nested) transaction by wrapping it in a SAVEPOINT. If an
        ///         exception occurs the whole transaction is rolled back, not just the current savepoint. The exception
        ///         is rethrown.
        /// </summary>
        /// <param name="action">
        ///     The <see cref="Action" /> to perform within a transaction.
        ///     <param name="action">
        ///         can contain any number
        ///         of operations on the connection but should never call <see cref="BeginTransaction" /> or
        ///         <see cref="Commit" />.
        ///     </param>
        public void RunInTransaction(Action action)
        {
            try
            {
                string savePoint = SaveTransactionPoint();
                action();
                Release(savePoint);
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        ///     Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int InsertAll(IEnumerable objects)
        {
            int c = 0;
            RunInTransaction(() =>
            {
                foreach (object r in objects) c += Insert(r);
            });
            return c;
        }

        /// <summary>
        ///     Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name="extra">
        ///     Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int InsertAll(IEnumerable objects, string extra)
        {
            int c = 0;
            RunInTransaction(() =>
            {
                foreach (object r in objects) c += Insert(r, extra);
            });
            return c;
        }

        /// <summary>
        ///     Inserts all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int InsertAll(IEnumerable objects, Type objType)
        {
            int c = 0;
            RunInTransaction(() =>
            {
                foreach (object r in objects) c += Insert(r, objType);
            });
            return c;
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int Insert(object obj)
        {
            if (obj == null) return 0;
            return Insert(obj, "", obj.GetType());
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        ///     If a UNIQUE constraint violation occurs with
        ///     some pre-existing object, this function deletes
        ///     the old object.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows modified.
        /// </returns>
        public int InsertOrReplace(object obj)
        {
            if (obj == null) return 0;
            return Insert(obj, "OR REPLACE", obj.GetType());
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, Type objType)
        {
            return Insert(obj, "", objType);
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        ///     If a UNIQUE constraint violation occurs with
        ///     some pre-existing object, this function deletes
        ///     the old object.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows modified.
        /// </returns>
        public int InsertOrReplace(object obj, Type objType)
        {
            return Insert(obj, "OR REPLACE", objType);
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="extra">
        ///     Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, string extra)
        {
            if (obj == null) return 0;
            return Insert(obj, extra, obj.GetType());
        }

        /// <summary>
        ///     Inserts the given object and retrieves its
        ///     auto incremented primary key if it has one.
        /// </summary>
        /// <param name="obj">
        ///     The object to insert.
        /// </param>
        /// <param name="extra">
        ///     Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows added to the table.
        /// </returns>
        public int Insert(object obj, string extra, Type objType)
        {
            if (obj == null || objType == null) return 0;

            TableMapping map = GetMapping(objType);

            if (map.PK != null && map.PK.IsAutoGuid)
            {
                PropertyInfo prop = objType.GetProperty(map.PK.PropertyName);
                if (prop != null)
                    if (prop.GetValue(obj, null).Equals(Guid.Empty))
                        prop.SetValue(obj, Guid.NewGuid(), null);
            }

            bool replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

            TableMapping.Column[] cols = replacing ? map.InsertOrReplaceColumns : map.InsertColumns;
            object[] vals = new object[cols.Length];
            for (int i = 0; i < vals.Length; i++) vals[i] = cols[i].GetValue(obj);

            PreparedSqlLiteInsertCommand insertCmd = map.GetInsertCommand(this, extra);
            int count;

            try
            {
                count = insertCmd.ExecuteNonQuery(vals);
            }
            catch (SQLiteException ex)
            {
                if (SQLite3.ExtendedErrCode(this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                    throw NotNullConstraintViolationException.New(ex.Result, ex.Message, map, obj);
                throw;
            }

            if (map.HasAutoIncPK)
            {
                long id = SQLite3.LastInsertRowid(this.Handle);
                map.SetAutoIncPK(obj, id);
            }

            return count;
        }

        /// <summary>
        ///     Updates all of the columns of a table using the specified object
        ///     except for its primary key.
        ///     The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        ///     The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        ///     The number of rows updated.
        /// </returns>
        public int Update(object obj)
        {
            if (obj == null) return 0;
            return Update(obj, obj.GetType());
        }

        /// <summary>
        ///     Updates all of the columns of a table using the specified object
        ///     except for its primary key.
        ///     The object is required to have a primary key.
        /// </summary>
        /// <param name="obj">
        ///     The object to update. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <param name="objType">
        ///     The type of object to insert.
        /// </param>
        /// <returns>
        ///     The number of rows updated.
        /// </returns>
        public int Update(object obj, Type objType)
        {
            int rowsAffected = 0;
            if (obj == null || objType == null) return 0;

            TableMapping map = GetMapping(objType);
            TableMapping.Column pk = map.PK;

            if (pk == null) throw new NotSupportedException("Cannot update " + map.TableName + ": it has no PK");

            IEnumerable<TableMapping.Column> cols = from p in map.Columns where p != pk select p;
            IEnumerable<object> vals = from c in cols select c.GetValue(obj);
            List<object> ps = new List<object>(vals);
            ps.Add(pk.GetValue(obj));

            string q = string.Format("update \"{0}\" set {1} where {2} = ? ", map.TableName,
                string.Join(",", (from c in cols select "\"" + c.Name + "\" = ? ").ToArray()), pk.Name);

            try
            {
                rowsAffected = Execute(q, ps.ToArray());
            }
            catch (SQLiteException ex)
            {
                if (ex.Result == SQLite3.Result.Constraint &&
                    SQLite3.ExtendedErrCode(this.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                    throw NotNullConstraintViolationException.New(ex, map, obj);

                throw ex;
            }

            return rowsAffected;
        }

        /// <summary>
        ///     Updates all specified objects.
        /// </summary>
        /// <param name="objects">
        ///     An <see cref="IEnumerable" /> of the objects to insert.
        /// </param>
        /// <returns>
        ///     The number of rows modified.
        /// </returns>
        public int UpdateAll(IEnumerable objects)
        {
            int c = 0;
            RunInTransaction(() =>
            {
                foreach (object r in objects) c += Update(r);
            });
            return c;
        }

        /// <summary>
        ///     Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name="objectToDelete">
        ///     The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        ///     The number of rows deleted.
        /// </returns>
        public int Delete(object objectToDelete)
        {
            TableMapping map = GetMapping(objectToDelete.GetType());
            TableMapping.Column pk = map.PK;
            if (pk == null) throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            string q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, pk.GetValue(objectToDelete));
        }

        /// <summary>
        ///     Deletes the object with the specified primary key.
        /// </summary>
        /// <param name="primaryKey">
        ///     The primary key of the object to delete.
        /// </param>
        /// <returns>
        ///     The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        ///     The type of object.
        /// </typeparam>
        public int Delete<T>(object primaryKey)
        {
            TableMapping map = GetMapping(typeof(T));
            TableMapping.Column pk = map.PK;
            if (pk == null) throw new NotSupportedException("Cannot delete " + map.TableName + ": it has no PK");
            string q = string.Format("delete from \"{0}\" where \"{1}\" = ?", map.TableName, pk.Name);
            return Execute(q, primaryKey);
        }

        /// <summary>
        ///     Deletes all the objects from the specified table.
        ///     WARNING WARNING: Let me repeat. It deletes ALL the objects from the
        ///     specified table. Do you really want to do that?
        /// </summary>
        /// <returns>
        ///     The number of objects deleted.
        /// </returns>
        /// <typeparam name='T'>
        ///     The type of objects to delete.
        /// </typeparam>
        public int DeleteAll<T>()
        {
            TableMapping map = GetMapping(typeof(T));
            string query = string.Format("delete from \"{0}\"", map.TableName);
            return Execute(query);
        }

        ~SQLiteConnection()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Close();
        }

        public void Close()
        {
            if (_open && this.Handle != NullHandle)
                try
                {
                    if (_mappings != null)
                        foreach (TableMapping sqlInsertCommand in _mappings.Values)
                            sqlInsertCommand.Dispose();
                    SQLite3.Result r = SQLite3.Close(this.Handle);
                    if (r != SQLite3.Result.OK)
                    {
                        string msg = SQLite3.GetErrmsg(this.Handle);
                        throw SQLiteException.New(r, msg);
                    }
                }
                finally
                {
                    this.Handle = NullHandle;
                    _open = false;
                }
        }

        private struct IndexedColumn
        {
            public int Order;
            public string ColumnName;
        }

        private struct IndexInfo
        {
            public string IndexName;
            public string TableName;
            public bool Unique;
            public List<IndexedColumn> Columns;
        }

        public class ColumnInfo
        {
            //			public int cid { get; set; }

            [Column("name")] public string Name { get; set; }

            //			[Column ("type")]
            //			public string ColumnType { get; set; }

            public int notnull { get; set; }

            //			public string dflt_value { get; set; }

            //			public int pk { get; set; }

            public override string ToString()
            {
                return this.Name;
            }
        }
    }

    /// <summary>
    ///     Represents a parsed connection string.
    /// </summary>
    internal class SQLiteConnectionString
    {
        public string ConnectionString { get; private set; }
        public string DatabasePath { get; private set; }
        public bool StoreDateTimeAsTicks { get; private set; }

        public SQLiteConnectionString(string databasePath, bool storeDateTimeAsTicks)
        {
            this.ConnectionString = databasePath;
            this.StoreDateTimeAsTicks = storeDateTimeAsTicks;
            this.DatabasePath = databasePath;
        }
    }

    public class TableMapping
    {
        private readonly Column _autoPk;
        private Column[] _insertColumns;

        private PreparedSqlLiteInsertCommand _insertCommand;
        private string _insertCommandExtra;
        private Column[] _insertOrReplaceColumns;

        public TableMapping(Type type, CreateFlags createFlags = CreateFlags.None)
        {
            this.MappedType = type;
            TableAttribute tableAttr =
                (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();
            this.TableName = tableAttr != null ? tableAttr.Name : this.MappedType.Name;

            PropertyInfo[] props = this.MappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                                 BindingFlags.SetProperty);
            List<Column> cols = new List<Column>();
            foreach (PropertyInfo p in props)
            {
                bool ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Length > 0;
                if (p.CanWrite && !ignore) cols.Add(new Column(p, createFlags));
            }

            this.Columns = cols.ToArray();
            foreach (Column c in this.Columns)
            {
                if (c.IsAutoInc && c.IsPK) _autoPk = c;
                if (c.IsPK) this.PK = c;
            }

            this.HasAutoIncPK = _autoPk != null;

            if (this.PK != null)
                this.GetByPrimaryKeySql =
                    string.Format("select * from \"{0}\" where \"{1}\" = ?", this.TableName, this.PK.Name);
            else
                this.GetByPrimaryKeySql = string.Format("select * from \"{0}\" limit 1", this.TableName);
        }

        public Type MappedType { get; private set; }
        public string TableName { get; private set; }
        public Column[] Columns { get; private set; }
        public Column PK { get; private set; }
        public string GetByPrimaryKeySql { get; private set; }

        public bool HasAutoIncPK { get; private set; }

        public Column[] InsertColumns
        {
            get
            {
                if (_insertColumns == null) _insertColumns = this.Columns.Where(c => !c.IsAutoInc).ToArray();
                return _insertColumns;
            }
        }

        public Column[] InsertOrReplaceColumns
        {
            get
            {
                if (_insertOrReplaceColumns == null) _insertOrReplaceColumns = this.Columns.ToArray();
                return _insertOrReplaceColumns;
            }
        }

        public void SetAutoIncPK(object obj, long id)
        {
            if (_autoPk != null) _autoPk.SetValue(obj, Convert.ChangeType(id, _autoPk.ColumnType, null));
        }

        public Column FindColumnWithPropertyName(string propertyName)
        {
            Column exact = this.Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            return exact;
        }

        public Column FindColumn(string columnName)
        {
            Column exact = this.Columns.FirstOrDefault(c => c.Name == columnName);
            return exact;
        }

        public PreparedSqlLiteInsertCommand GetInsertCommand(SQLiteConnection conn, string extra)
        {
            if (_insertCommand == null)
            {
                _insertCommand = CreateInsertCommand(conn, extra);
                _insertCommandExtra = extra;
            }
            else if (_insertCommandExtra != extra)
            {
                _insertCommand.Dispose();
                _insertCommand = CreateInsertCommand(conn, extra);
                _insertCommandExtra = extra;
            }

            return _insertCommand;
        }

        private PreparedSqlLiteInsertCommand CreateInsertCommand(SQLiteConnection conn, string extra)
        {
            Column[] cols = this.InsertColumns;
            string insertSql;
            if (!cols.Any() && this.Columns.Count() == 1 && this.Columns[0].IsAutoInc)
            {
                insertSql = string.Format("insert {1} into \"{0}\" default values", this.TableName, extra);
            }
            else
            {
                bool replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

                if (replacing) cols = this.InsertOrReplaceColumns;

                insertSql = string.Format("insert {3} into \"{0}\"({1}) values ({2})", this.TableName,
                    string.Join(",", (from c in cols select "\"" + c.Name + "\"").ToArray()),
                    string.Join(",", (from c in cols select "?").ToArray()), extra);
            }

            PreparedSqlLiteInsertCommand insertCommand = new PreparedSqlLiteInsertCommand(conn);
            insertCommand.CommandText = insertSql;
            return insertCommand;
        }

        protected internal void Dispose()
        {
            if (_insertCommand != null)
            {
                _insertCommand.Dispose();
                _insertCommand = null;
            }
        }

        public class Column
        {
            private readonly PropertyInfo _prop;

            public Column(PropertyInfo prop, CreateFlags createFlags = CreateFlags.None)
            {
                ColumnAttribute colAttr =
                    (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

                _prop = prop;
                this.Name = colAttr == null ? prop.Name : colAttr.Name;
                //If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
                this.ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                this.Collation = Orm.Collation(prop);

                this.IsPK = Orm.IsPK(prop) ||
                            (createFlags & CreateFlags.ImplicitPK) == CreateFlags.ImplicitPK &&
                            string.Compare(prop.Name, Orm.ImplicitPkName, StringComparison.OrdinalIgnoreCase) == 0;

                bool isAuto = Orm.IsAutoInc(prop) ||
                              this.IsPK && (createFlags & CreateFlags.AutoIncPK) == CreateFlags.AutoIncPK;
                this.IsAutoGuid = isAuto && this.ColumnType == typeof(Guid);
                this.IsAutoInc = isAuto && !this.IsAutoGuid;

                this.Indices = Orm.GetIndices(prop);
                if (!this.Indices.Any() &&
                    !this.IsPK &&
                    (createFlags & CreateFlags.ImplicitIndex) == CreateFlags.ImplicitIndex &&
                    this.Name.EndsWith(Orm.ImplicitIndexSuffix, StringComparison.OrdinalIgnoreCase)
                )
                    this.Indices = new[] { new IndexedAttribute() };
                this.IsNullable = !(this.IsPK || Orm.IsMarkedNotNull(prop));
                this.MaxStringLength = Orm.MaxStringLength(prop);
            }

            public string Name { get; private set; }

            public string PropertyName
            {
                get { return _prop.Name; }
            }

            public Type ColumnType { get; private set; }

            public string Collation { get; private set; }

            public bool IsAutoInc { get; private set; }
            public bool IsAutoGuid { get; private set; }

            public bool IsPK { get; private set; }

            public IEnumerable<IndexedAttribute> Indices { get; set; }

            public bool IsNullable { get; private set; }

            public int? MaxStringLength { get; private set; }

            public void SetValue(object obj, object val)
            {
                _prop.SetValue(obj, val, null);
            }

            public object GetValue(object obj)
            {
                return _prop.GetValue(obj, null);
            }
        }
    }

    public static class Orm
    {
        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks) + " ";

            if (p.IsPK) decl += "primary key ";
            if (p.IsAutoInc) decl += "autoincrement ";
            if (!p.IsNullable) decl += "not null ";
            if (!string.IsNullOrEmpty(p.Collation)) decl += "collate " + p.Collation + " ";

            return decl;
        }

        public static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            Type clrType = p.ColumnType;
            if (clrType == typeof(bool) || clrType == typeof(byte) || clrType == typeof(ushort) ||
                clrType == typeof(sbyte) || clrType == typeof(short) || clrType == typeof(int)) return "integer";

            if (clrType == typeof(uint) || clrType == typeof(long)) return "bigint";

            if (clrType == typeof(float) || clrType == typeof(double) || clrType == typeof(decimal)) return "float";

            if (clrType == typeof(string))
            {
                int? len = p.MaxStringLength;

                if (len.HasValue)
                    return "varchar(" + len.Value + ")";

                return "varchar";
            }

            if (clrType == typeof(TimeSpan)) return "bigint";

            if (clrType == typeof(DateTime)) return storeDateTimeAsTicks ? "bigint" : "datetime";

            if (clrType == typeof(DateTimeOffset))
            {
                return "bigint";
            }

            if (clrType.IsEnum)
            {
                return "integer";
            }

            if (clrType == typeof(byte[]))
                return "blob";
            if (clrType == typeof(Guid))
                return "varchar(36)";
            throw new NotSupportedException("Don't know about " + clrType);
        }

        public static bool IsPK(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
            return attrs.Length > 0;
        }

        public static string Collation(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof(CollationAttribute), true);
            if (attrs.Length > 0)
                return ((CollationAttribute)attrs[0]).Value;
            return string.Empty;
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof(AutoIncrementAttribute), true);
            return attrs.Length > 0;
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int? MaxStringLength(PropertyInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof(MaxLengthAttribute), true);
            if (attrs.Length > 0)
                return ((MaxLengthAttribute)attrs[0]).Value;
            return null;
        }

        public static bool IsMarkedNotNull(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof(NotNullAttribute), true);
            return attrs.Length > 0;
        }
    }

    public class SQLiteCommand
    {
        internal static IntPtr NegativePointer = new IntPtr(-1);
        private readonly List<Binding> _bindings;
        private readonly SQLiteConnection _conn;

        internal SQLiteCommand(SQLiteConnection conn)
        {
            _conn = conn;
            _bindings = new List<Binding>();
            this.CommandText = "";
        }

        public string CommandText { get; set; }

        public int ExecuteNonQuery()
        {
            if (_conn.Trace) Debug.WriteLine("Executing: " + this);

            SQLite3.Result r = SQLite3.Result.OK;
            IntPtr stmt = Prepare();
            r = SQLite3.Step(stmt);
            Finalize(stmt);
            if (r == SQLite3.Result.Done)
            {
                int rowsAffected = SQLite3.Changes(_conn.Handle);
                return rowsAffected;
            }

            if (r == SQLite3.Result.Error)
            {
                string msg = SQLite3.GetErrmsg(_conn.Handle);
                throw SQLiteException.New(r, msg);
            }

            if (r == SQLite3.Result.Constraint)
                if (SQLite3.ExtendedErrCode(_conn.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
                    throw NotNullConstraintViolationException.New(r, SQLite3.GetErrmsg(_conn.Handle));

            throw SQLiteException.New(r, r.ToString());
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T)));
        }

        public List<T> ExecuteQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T))).ToList();
        }

        public List<T> ExecuteQuery<T>(TableMapping map)
        {
            return ExecuteDeferredQuery<T>(map).ToList();
        }

        /// <summary>
        ///     Invoked every time an instance is loaded from the database.
        /// </summary>
        /// <param name='obj'>
        ///     The newly created object.
        /// </param>
        /// <remarks>
        ///     This can be overridden in combination with the <see cref="SQLiteConnection.NewCommand" />
        ///     method to hook into the life-cycle of objects.
        ///     Type safety is not possible because MonoTouch does not support virtual generic methods.
        /// </remarks>
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            if (_conn.Trace) Debug.WriteLine("Executing Query: " + this);

            IntPtr stmt = Prepare();
            try
            {
                TableMapping.Column[] cols = new TableMapping.Column[SQLite3.ColumnCount(stmt)];

                for (int i = 0; i < cols.Length; i++)
                {
                    string name = SQLite3.ColumnName16(stmt, i);
                    cols[i] = map.FindColumn(name);
                }

                while (SQLite3.Step(stmt) == SQLite3.Result.Row)
                {
                    object obj = Activator.CreateInstance(map.MappedType);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] == null)
                            continue;
                        SQLite3.ColType colType = SQLite3.ColumnType(stmt, i);
                        object val = ReadCol(stmt, i, colType, cols[i].ColumnType);
                        cols[i].SetValue(obj, val);
                    }

                    OnInstanceCreated(obj);
                    yield return (T)obj;
                }
            }
            finally
            {
                SQLite3.Finalize(stmt);
            }
        }

        public T ExecuteScalar<T>()
        {
            if (_conn.Trace) Debug.WriteLine("Executing Query: " + this);

            T val = default(T);

            IntPtr stmt = Prepare();

            try
            {
                SQLite3.Result r = SQLite3.Step(stmt);
                if (r == SQLite3.Result.Row)
                {
                    SQLite3.ColType colType = SQLite3.ColumnType(stmt, 0);
                    val = (T)ReadCol(stmt, 0, colType, typeof(T));
                }
                else if (r == SQLite3.Result.Done) { }
                else
                {
                    throw SQLiteException.New(r, SQLite3.GetErrmsg(_conn.Handle));
                }
            }
            finally
            {
                Finalize(stmt);
            }

            return val;
        }

        public void Bind(string name, object val)
        {
            _bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        public override string ToString()
        {
            string[] parts = new string[1 + _bindings.Count];
            parts[0] = this.CommandText;
            int i = 1;
            foreach (Binding b in _bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }

            return string.Join(Environment.NewLine, parts);
        }

        private Sqlite3Statement Prepare()
        {
            IntPtr stmt = SQLite3.Prepare2(_conn.Handle, this.CommandText);
            BindAll(stmt);
            return stmt;
        }

        private void Finalize(Sqlite3Statement stmt)
        {
            SQLite3.Finalize(stmt);
        }

        private void BindAll(Sqlite3Statement stmt)
        {
            int nextIdx = 1;
            foreach (Binding b in _bindings)
            {
                if (b.Name != null)
                    b.Index = SQLite3.BindParameterIndex(stmt, b.Name);
                else
                    b.Index = nextIdx++;

                BindParameter(stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks);
            }
        }

        internal static int BindParameter(Sqlite3Statement stmt, int index, object value, bool storeDateTimeAsTicks)
        {
            if (value == null)
                return SQLite3.BindNull(stmt, index);
            if (value is int)
                return SQLite3.BindInt(stmt, index, (int)value);
            if (value is string)
                return SQLite3.BindText(stmt, index, (string)value, -1, NegativePointer);
            if (value is byte || value is ushort || value is sbyte || value is short)
                return SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
            if (value is bool)
                return SQLite3.BindInt(stmt, index, (bool)value ? 1 : 0);
            if (value is uint || value is long)
                return SQLite3.BindInt64(stmt, index, Convert.ToInt64(value));
            if (value is float || value is double || value is decimal)
                return SQLite3.BindDouble(stmt, index, Convert.ToDouble(value));
            if (value is TimeSpan)
                return SQLite3.BindInt64(stmt, index, ((TimeSpan)value).Ticks);
            if (value is DateTime)
            {
                if (storeDateTimeAsTicks)
                    return SQLite3.BindInt64(stmt, index, ((DateTime)value).Ticks);
                return SQLite3.BindText(stmt, index, ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"), -1,
                    NegativePointer);
            }

            if (value is DateTimeOffset)
                return SQLite3.BindInt64(stmt, index, ((DateTimeOffset)value).UtcTicks);

            {
                if (value.GetType().IsEnum)
                    return SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
            }

            if (value is byte[])
                return SQLite3.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer);
            if (value is Guid)
                return SQLite3.BindText(stmt, index, ((Guid)value).ToString(), 72, NegativePointer);

            throw new NotSupportedException("Cannot store type: " + value.GetType());
        }

        private object ReadCol(Sqlite3Statement stmt, int index, SQLite3.ColType type, Type clrType)
        {
            if (type == SQLite3.ColType.Null)
                return null;

            if (clrType == typeof(string)) return SQLite3.ColumnString(stmt, index);

            if (clrType == typeof(int)) return SQLite3.ColumnInt(stmt, index);

            if (clrType == typeof(bool)) return SQLite3.ColumnInt(stmt, index) == 1;

            if (clrType == typeof(double)) return SQLite3.ColumnDouble(stmt, index);

            if (clrType == typeof(float)) return (float)SQLite3.ColumnDouble(stmt, index);

            if (clrType == typeof(TimeSpan)) return new TimeSpan(SQLite3.ColumnInt64(stmt, index));

            if (clrType == typeof(DateTime))
            {
                if (_conn.StoreDateTimeAsTicks) return new DateTime(SQLite3.ColumnInt64(stmt, index));

                string text = SQLite3.ColumnString(stmt, index);
                return DateTime.Parse(text);
            }

            if (clrType == typeof(DateTimeOffset))
            {
                return new DateTimeOffset(SQLite3.ColumnInt64(stmt, index), TimeSpan.Zero);
            }

            if (clrType.IsEnum)
            {
                return SQLite3.ColumnInt(stmt, index);
            }

            if (clrType == typeof(long)) return SQLite3.ColumnInt64(stmt, index);

            if (clrType == typeof(uint)) return (uint)SQLite3.ColumnInt64(stmt, index);

            if (clrType == typeof(decimal)) return (decimal)SQLite3.ColumnDouble(stmt, index);

            if (clrType == typeof(byte)) return (byte)SQLite3.ColumnInt(stmt, index);

            if (clrType == typeof(ushort)) return (ushort)SQLite3.ColumnInt(stmt, index);

            if (clrType == typeof(short)) return (short)SQLite3.ColumnInt(stmt, index);

            if (clrType == typeof(sbyte)) return (sbyte)SQLite3.ColumnInt(stmt, index);

            if (clrType == typeof(byte[])) return SQLite3.ColumnByteArray(stmt, index);

            if (clrType == typeof(Guid))
            {
                string text = SQLite3.ColumnString(stmt, index);
                return new Guid(text);
            }

            throw new NotSupportedException("Don't know how to read " + clrType);
        }

        private class Binding
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public int Index { get; set; }
        }
    }

    /// <summary>
    ///     Since the insert never changed, we only need to prepare once.
    /// </summary>
    public class PreparedSqlLiteInsertCommand : IDisposable
    {
        internal static readonly Sqlite3Statement NullStatement = default(Sqlite3Statement);

        internal PreparedSqlLiteInsertCommand(SQLiteConnection conn)
        {
            this.Connection = conn;
        }

        public bool Initialized { get; set; }
        public string CommandText { get; set; }

        protected SQLiteConnection Connection { get; set; }
        protected Sqlite3Statement Statement { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int ExecuteNonQuery(object[] source)
        {
            if (this.Connection.Trace) Debug.WriteLine("Executing: " + this.CommandText);

            SQLite3.Result r = SQLite3.Result.OK;

            if (!this.Initialized)
            {
                this.Statement = Prepare();
                this.Initialized = true;
            }

            //bind the values.
            if (source != null)
                for (int i = 0; i < source.Length; i++)
                    SQLiteCommand.BindParameter(this.Statement, i + 1, source[i], this.Connection.StoreDateTimeAsTicks);
            r = SQLite3.Step(this.Statement);

            if (r == SQLite3.Result.Done)
            {
                int rowsAffected = SQLite3.Changes(this.Connection.Handle);
                SQLite3.Reset(this.Statement);
                return rowsAffected;
            }

            if (r == SQLite3.Result.Error)
            {
                string msg = SQLite3.GetErrmsg(this.Connection.Handle);
                SQLite3.Reset(this.Statement);
                throw SQLiteException.New(r, msg);
            }

            if (r == SQLite3.Result.Constraint &&
                SQLite3.ExtendedErrCode(this.Connection.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
            {
                SQLite3.Reset(this.Statement);
                throw NotNullConstraintViolationException.New(r, SQLite3.GetErrmsg(this.Connection.Handle));
            }

            SQLite3.Reset(this.Statement);
            throw SQLiteException.New(r, r.ToString());
        }

        protected virtual Sqlite3Statement Prepare()
        {
            IntPtr stmt = SQLite3.Prepare2(this.Connection.Handle, this.CommandText);
            return stmt;
        }

        private void Dispose(bool disposing)
        {
            if (this.Statement != NullStatement)
                try
                {
                    SQLite3.Finalize(this.Statement);
                }
                finally
                {
                    this.Statement = NullStatement;
                    this.Connection = null;
                }
        }

        ~PreparedSqlLiteInsertCommand()
        {
            Dispose(false);
        }
    }

    public abstract class BaseTableQuery
    {
        protected class Ordering
        {
            public string ColumnName { get; set; }
            public bool Ascending { get; set; }
        }
    }

    public class TableQuery<T> : BaseTableQuery, IEnumerable<T>
    {
        private bool _deferred;

        private BaseTableQuery _joinInner;
        private Expression _joinInnerKeySelector;
        private BaseTableQuery _joinOuter;
        private Expression _joinOuterKeySelector;
        private Expression _joinSelector;
        private int? _limit;
        private int? _offset;
        private List<Ordering> _orderBys;

        private Expression _selector;

        private Expression _where;

        private TableQuery(SQLiteConnection conn, TableMapping table)
        {
            this.Connection = conn;
            this.Table = table;
        }

        public TableQuery(SQLiteConnection conn)
        {
            this.Connection = conn;
            this.Table = this.Connection.GetMapping(typeof(T));
        }

        public SQLiteConnection Connection { get; private set; }

        public TableMapping Table { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            if (!_deferred)
                return GenerateCommand("*").ExecuteQuery<T>().GetEnumerator();

            return GenerateCommand("*").ExecuteDeferredQuery<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TableQuery<U> Clone<U>()
        {
            TableQuery<U> q = new TableQuery<U>(this.Connection, this.Table);
            q._where = _where;
            q._deferred = _deferred;
            if (_orderBys != null) q._orderBys = new List<Ordering>(_orderBys);
            q._limit = _limit;
            q._offset = _offset;
            q._joinInner = _joinInner;
            q._joinInnerKeySelector = _joinInnerKeySelector;
            q._joinOuter = _joinOuter;
            q._joinOuterKeySelector = _joinOuterKeySelector;
            q._joinSelector = _joinSelector;
            q._selector = _selector;
            return q;
        }

        public TableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            if (predExpr.NodeType == ExpressionType.Lambda)
            {
                LambdaExpression lambda = predExpr;
                Expression pred = lambda.Body;
                TableQuery<T> q = Clone<T>();
                q.AddWhere(pred);
                return q;
            }

            throw new NotSupportedException("Must be a predicate");
        }

        public TableQuery<T> Take(int n)
        {
            TableQuery<T> q = Clone<T>();
            q._limit = n;
            return q;
        }

        public TableQuery<T> Skip(int n)
        {
            TableQuery<T> q = Clone<T>();
            q._offset = n;
            return q;
        }

        public T ElementAt(int index)
        {
            return Skip(index).Take(1).First();
        }

        public TableQuery<T> Deferred()
        {
            TableQuery<T> q = Clone<T>();
            q._deferred = true;
            return q;
        }

        public TableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr)
        {
            return AddOrderBy(orderExpr, true);
        }

        public TableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr)
        {
            return AddOrderBy(orderExpr, false);
        }

        public TableQuery<T> ThenBy<U>(Expression<Func<T, U>> orderExpr)
        {
            return AddOrderBy(orderExpr, true);
        }

        public TableQuery<T> ThenByDescending<U>(Expression<Func<T, U>> orderExpr)
        {
            return AddOrderBy(orderExpr, false);
        }

        private TableQuery<T> AddOrderBy<U>(Expression<Func<T, U>> orderExpr, bool asc)
        {
            if (orderExpr.NodeType == ExpressionType.Lambda)
            {
                LambdaExpression lambda = orderExpr;

                MemberExpression mem = null;

                UnaryExpression unary = lambda.Body as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    mem = unary.Operand as MemberExpression;
                else
                    mem = lambda.Body as MemberExpression;

                if (mem != null && mem.Expression.NodeType == ExpressionType.Parameter)
                {
                    TableQuery<T> q = Clone<T>();
                    if (q._orderBys == null) q._orderBys = new List<Ordering>();
                    q._orderBys.Add(new Ordering
                    {
                        ColumnName = this.Table.FindColumnWithPropertyName(mem.Member.Name).Name,
                        Ascending = asc
                    });
                    return q;
                }

                throw new NotSupportedException("Order By does not support: " + orderExpr);
            }

            throw new NotSupportedException("Must be a predicate");
        }

        private void AddWhere(Expression pred)
        {
            if (_where == null)
                _where = pred;
            else
                _where = Expression.AndAlso(_where, pred);
        }

        public TableQuery<TResult> Join<TInner, TKey, TResult>(
            TableQuery<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Expression<Func<T, TInner, TResult>> resultSelector)
        {
            TableQuery<TResult> q =
                new TableQuery<TResult>(this.Connection, this.Connection.GetMapping(typeof(TResult)))
                {
                    _joinOuter = this,
                    _joinOuterKeySelector = outerKeySelector,
                    _joinInner = inner,
                    _joinInnerKeySelector = innerKeySelector,
                    _joinSelector = resultSelector
                };
            return q;
        }

        public TableQuery<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            TableQuery<TResult> q = Clone<TResult>();
            q._selector = selector;
            return q;
        }

        private SQLiteCommand GenerateCommand(string selectionList)
        {
            if (_joinInner != null && _joinOuter != null)
                throw new NotSupportedException("Joins are not supported.");

            string cmdText = "select " + selectionList + " from \"" + this.Table.TableName + "\"";
            List<object> args = new List<object>();
            if (_where != null)
            {
                CompileResult w = CompileExpr(_where, args);
                cmdText += " where " + w.CommandText;
            }

            if (_orderBys != null && _orderBys.Count > 0)
            {
                string t = string.Join(", ",
                    _orderBys.Select(o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " desc")).ToArray());
                cmdText += " order by " + t;
            }

            if (_limit.HasValue) cmdText += " limit " + _limit.Value;
            if (_offset.HasValue)
            {
                if (!_limit.HasValue) cmdText += " limit -1 ";
                cmdText += " offset " + _offset.Value;
            }

            return this.Connection.CreateCommand(cmdText, args.ToArray());
        }

        private CompileResult CompileExpr(Expression expr, List<object> queryArgs)
        {
            if (expr == null)
                throw new NotSupportedException("Expression is NULL");

            if (expr is BinaryExpression)
            {
                BinaryExpression bin = (BinaryExpression)expr;

                CompileResult leftr = CompileExpr(bin.Left, queryArgs);
                CompileResult rightr = CompileExpr(bin.Right, queryArgs);

                //If either side is a parameter and is null, then handle the other side specially (for "is null"/"is not null")
                string text;
                if (leftr.CommandText == "?" && leftr.Value == null)
                    text = CompileNullBinaryExpression(bin, rightr);
                else if (rightr.CommandText == "?" && rightr.Value == null)
                    text = CompileNullBinaryExpression(bin, leftr);
                else
                    text = "(" + leftr.CommandText + " " + GetSqlName(bin) + " " + rightr.CommandText + ")";
                return new CompileResult { CommandText = text };
            }

            if (expr.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = (MethodCallExpression)expr;
                CompileResult[] args = new CompileResult[call.Arguments.Count];
                CompileResult obj = call.Object != null ? CompileExpr(call.Object, queryArgs) : null;

                for (int i = 0; i < args.Length; i++) args[i] = CompileExpr(call.Arguments[i], queryArgs);

                string sqlCall = "";

                if (call.Method.Name == "Like" && args.Length == 2)
                {
                    sqlCall = "(" + args[0].CommandText + " like " + args[1].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 2)
                {
                    sqlCall = "(" + args[1].CommandText + " in " + args[0].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 1)
                {
                    if (call.Object != null && call.Object.Type == typeof(string))
                        sqlCall = "(" + obj.CommandText + " like ('%' || " + args[0].CommandText + " || '%'))";
                    else
                        sqlCall = "(" + args[0].CommandText + " in " + obj.CommandText + ")";
                }
                else if (call.Method.Name == "StartsWith" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " like (" + args[0].CommandText + " || '%'))";
                }
                else if (call.Method.Name == "EndsWith" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " like ('%' || " + args[0].CommandText + "))";
                }
                else if (call.Method.Name == "Equals" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " = (" + args[0].CommandText + "))";
                }
                else if (call.Method.Name == "ToLower")
                {
                    sqlCall = "(lower(" + obj.CommandText + "))";
                }
                else if (call.Method.Name == "ToUpper")
                {
                    sqlCall = "(upper(" + obj.CommandText + "))";
                }
                else
                {
                    sqlCall = call.Method.Name.ToLower() + "(" +
                              string.Join(",", args.Select(a => a.CommandText).ToArray()) + ")";
                }

                return new CompileResult { CommandText = sqlCall };
            }

            if (expr.NodeType == ExpressionType.Constant)
            {
                ConstantExpression c = (ConstantExpression)expr;
                queryArgs.Add(c.Value);
                return new CompileResult
                {
                    CommandText = "?",
                    Value = c.Value
                };
            }

            if (expr.NodeType == ExpressionType.Convert)
            {
                UnaryExpression u = (UnaryExpression)expr;
                Type ty = u.Type;
                CompileResult valr = CompileExpr(u.Operand, queryArgs);
                return new CompileResult
                {
                    CommandText = valr.CommandText,
                    Value = valr.Value != null ? ConvertTo(valr.Value, ty) : null
                };
            }

            if (expr.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression mem = (MemberExpression)expr;

                if (mem.Expression != null && mem.Expression.NodeType == ExpressionType.Parameter)
                {
                    //
                    // This is a column of our table, output just the column name
                    // Need to translate it if that column name is mapped
                    //
                    string columnName = this.Table.FindColumnWithPropertyName(mem.Member.Name).Name;
                    return new CompileResult { CommandText = "\"" + columnName + "\"" };
                }

                object obj = null;
                if (mem.Expression != null)
                {
                    CompileResult r = CompileExpr(mem.Expression, queryArgs);
                    if (r.Value == null) throw new NotSupportedException("Member access failed to compile expression");
                    if (r.CommandText == "?") queryArgs.RemoveAt(queryArgs.Count - 1);
                    obj = r.Value;
                }

                //
                // Get the member value
                //
                object val = null;

                if (mem.Member.MemberType == MemberTypes.Property)
                {
                    PropertyInfo m = (PropertyInfo)mem.Member;
                    val = m.GetValue(obj, null);
                }
                else if (mem.Member.MemberType == MemberTypes.Field)
                {
                    FieldInfo m = (FieldInfo)mem.Member;
                    val = m.GetValue(obj);
                }
                else
                {
                    throw new NotSupportedException("MemberExpr: " + mem.Member.MemberType);
                }

                //
                // Work special magic for enumerables
                //
                if (val != null && val is IEnumerable && !(val is string) && !(val is IEnumerable<byte>))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("(");
                    string head = "";
                    foreach (object a in (IEnumerable)val)
                    {
                        queryArgs.Add(a);
                        sb.Append(head);
                        sb.Append("?");
                        head = ",";
                    }

                    sb.Append(")");
                    return new CompileResult
                    {
                        CommandText = sb.ToString(),
                        Value = val
                    };
                }

                queryArgs.Add(val);
                return new CompileResult
                {
                    CommandText = "?",
                    Value = val
                };
            }

            throw new NotSupportedException("Cannot compile: " + expr.NodeType);
        }

        private static object ConvertTo(object obj, Type t)
        {
            if (obj == null)
                return null;

            Type nut = Nullable.GetUnderlyingType(t);
            if (nut == null)
                return Convert.ChangeType(obj, t);
            return Convert.ChangeType(obj, nut);
        }

        /// <summary>
        ///     Compiles a BinaryExpression where one of the parameters is null.
        /// </summary>
        /// <param name="parameter">The non-null parameter</param>
        private string CompileNullBinaryExpression(BinaryExpression expression, CompileResult parameter)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                    return "(" + parameter.CommandText + " is ?)";
                case ExpressionType.NotEqual:
                    return "(" + parameter.CommandText + " is not ?)";
                default:
                    throw new NotSupportedException("Cannot compile Null-BinaryExpression with type " +
                                                    expression.NodeType);
            }
        }

        private string GetSqlName(Expression expr)
        {
            ExpressionType n = expr.NodeType;

            switch (n)
            {
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "and";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "or";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "!=";
                default:
                    throw new NotSupportedException("Cannot get SQL for: " + n);
            }
        }

        public int Count()
        {
            return GenerateCommand("count(*)").ExecuteScalar<int>();
        }

        public int Count(Expression<Func<T, bool>> predExpr)
        {
            return Where(predExpr).Count();
        }

        public T First()
        {
            TableQuery<T> query = Take(1);
            return query.ToList().First();
        }

        public T FirstOrDefault()
        {
            TableQuery<T> query = Take(1);
            return query.ToList().FirstOrDefault();
        }

        private class CompileResult
        {
            public string CommandText { get; set; }
            public object Value { get; set; }
        }
    }
}
