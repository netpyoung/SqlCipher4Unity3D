namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.AsyncExtensions
{
	using System.Collections;
	using System.Collections.Generic;
	using Cysharp.Threading.Tasks;
	using SqlCipher4Unity3D;

	public static class WriteOperations
    {
        /// <summary>
        /// Updates the with foreign keys of the current object and save changes to the database and
        /// updates the inverse foreign keys of the defined relationships so the relationships are
        /// stored correctly in the database. This operation will create or delete the required intermediate
        /// objects for ManyToMany relationships. All related objects must have a primary key assigned in order
        /// to work correctly. This also implies that any object with 'AutoIncrement' primary key must've been
        /// inserted in the database previous to this call.
        /// This method will also update inverse relationships of objects that currently exist in the object tree,
        /// but it won't update inverse relationships of objects that are not reachable through this object
        /// properties. For example, objects removed from a 'ToMany' relationship won't be updated in memory.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Object to be updated. Must already have been inserted in the database</param>
        public static UniTask UpdateWithChildrenAsync(this SQLiteAsyncConnection conn, object element)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.UpdateWithChildren(element);
				 }
			 });
		}

        /// <summary>
        /// Inserts the element and all the relationships that are annotated with <c>CascadeOperation.CascadeInsert</c>
        /// into the database. If any element already exists in the database a 'Constraint' exception will be raised.
        /// Elements with a primary key that it's not <c>AutoIncrement</c> will need a valid identifier before calling
        /// this method.
        /// If the <c>recursive</c> flag is set to true, all the relationships annotated with
        /// <c>CascadeOperation.CascadeInsert</c> are inserted recursively in the database. This method will handle
        /// loops and inverse relationships correctly. <c>ReadOnly</c> properties will be omitted.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Object to be inserted.</param>
        /// <param name="recursive">If set to <c>true</c> all the insert-cascade properties will be inserted</param>
        public static UniTask InsertWithChildrenAsync(this SQLiteAsyncConnection conn, object element, bool recursive = false)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.InsertWithChildren(element, recursive);
				 }
			 });
		}

        /// <summary>
        /// Inserts or replace the element and all the relationships that are annotated with
        /// <c>CascadeOperation.CascadeInsert</c> into the database. If any element already exists in the database
        /// it will be replaced. Elements with <c>AutoIncrement</c> primary keys that haven't been assigned will
        /// be always inserted instead of replaced.
        /// If the <c>recursive</c> flag is set to true, all the relationships annotated with
        /// <c>CascadeOperation.CascadeInsert</c> are inserted recursively in the database. This method will handle
        /// loops and inverse relationships correctly. <c>ReadOnly</c> properties will be omitted.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Object to be inserted.</param>
        /// <param name="recursive">If set to <c>true</c> all the insert-cascade properties will be inserted</param>
        public static UniTask InsertOrReplaceWithChildrenAsync(this SQLiteAsyncConnection conn, object element, bool recursive = false)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.InsertOrReplaceWithChildren(element, recursive);
				 }
			 });
		}

        /// <summary>
        /// Inserts all the elements and all the relationships that are annotated with <c>CascadeOperation.CascadeInsert</c>
        /// into the database. If any element already exists in the database a 'Constraint' exception will be raised.
        /// Elements with a primary key that it's not <c>AutoIncrement</c> will need a valid identifier before calling
        /// this method.
        /// If the <c>recursive</c> flag is set to true, all the relationships annotated with
        /// <c>CascadeOperation.CascadeInsert</c> are inserted recursively in the database. This method will handle
        /// loops and inverse relationships correctly. <c>ReadOnly</c> properties will be omitted.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="elements">Objects to be inserted.</param>
        /// <param name="recursive">If set to <c>true</c> all the insert-cascade properties will be inserted</param>
        public static UniTask InsertAllWithChildrenAsync(this SQLiteAsyncConnection conn, IEnumerable elements, bool recursive = false)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.InsertAllWithChildren(elements, recursive);
				 }
			 });
		}

        /// <summary>
        /// Inserts or replace all the elements and all the relationships that are annotated with
        /// <c>CascadeOperation.CascadeInsert</c> into the database. If any element already exists in the database
        /// it will be replaced. Elements with <c>AutoIncrement</c> primary keys that haven't been assigned will
        /// be always inserted instead of replaced.
        /// If the <c>recursive</c> flag is set to true, all the relationships annotated with
        /// <c>CascadeOperation.CascadeInsert</c> are inserted recursively in the database. This method will handle
        /// loops and inverse relationships correctly. <c>ReadOnly</c> properties will be omitted.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="elements">Objects to be inserted.</param>
        /// <param name="recursive">If set to <c>true</c> all the insert-cascade properties will be inserted</param>
        public static UniTask InsertOrReplaceAllWithChildrenAsync(this SQLiteAsyncConnection conn, IEnumerable elements, bool recursive = false)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.InsertOrReplaceAllWithChildren(elements, recursive);
				 }
			 });
		}

        /// <summary>
        /// Deletes all the objects passed as parameters from the database.
        /// If recursive flag is set to true, all relationships marked with 'CascadeDelete' will be
        /// deleted from the database recursively. Inverse relationships and closed entity loops are handled
        /// correctly to avoid endless loops
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="recursive">If set to <c>true</c> all relationships marked with 'CascadeDelete' will be
        /// deleted from the database recursively</param>
        /// <param name="objects">Objects to be deleted from the database</param>
        public static UniTask DeleteAllAsync(this SQLiteAsyncConnection conn, IEnumerable objects, bool recursive = false)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.DeleteAll(objects, recursive);
				 }
			 });
		}

        /// <summary>
        /// Deletes all the objects passed as parameters from the database.
        /// If recursive flag is set to true, all relationships marked with 'CascadeDelete' will be
        /// deleted from the database recursively. Inverse relationships and closed entity loops are handled
        /// correctly to avoid endless loops
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="recursive">If set to <c>true</c> all relationships marked with 'CascadeDelete' will be
        /// deleted from the database recursively</param>
        /// <param name="element">Object to be deleted from the database</param>
        public static UniTask DeleteAsync(this SQLiteAsyncConnection conn, object element, bool recursive)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.Delete(element, recursive);
				 }
			 });
		}

        /// <summary>
        /// Deletes all the objects passed with IDs equal to the passed parameters from the database.
        /// Relationships are not taken into account in this method
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="primaryKeyValues">Primary keys of the objects to be deleted from the database</param>
        /// <typeparam name="T">The Entity type, it should match de database entity type</typeparam>
        public static UniTask DeleteAllIdsAsync<T>(this SQLiteAsyncConnection conn, IEnumerable<object> primaryKeyValues)
        {
			return UniTask.Run(() =>
			 {
				 var connectionWithLock = SqliteAsyncConnectionWrapper.Lock(conn);
				 using (connectionWithLock.Lock())
				 {
					 connectionWithLock.DeleteAllIds<T>(primaryKeyValues);
				 }
			 });
		}
    }
}