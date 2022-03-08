namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Attributes;
    using Exceptions;
    using SqlCipher4Unity3D;
    using SQLite.Attributes;
    using TextBlob;

    public static class WriteOperations
    {
        const int queryLimit = 990; //Make room for extra keys added by the code

        /// <summary>
        /// Enable to allow descriptive error descriptions on incorrect relationships. Enabled by default.
        /// Disable for production environments to remove the checks and reduce performance penalty
        /// </summary>
        public static bool EnableRuntimeAssertions = true;

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
        public static void UpdateWithChildren(this SQLiteConnection conn, object element)
        {
            // Update the current element
            RefreshForeignKeys(element);
            conn.Update(element);

            // Update inverse foreign keys
            conn.UpdateInverseForeignKeys(element);
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
        public static void InsertWithChildren(this SQLiteConnection conn, object element, bool recursive = false) {
            conn.InsertWithChildrenRecursive(element, false, recursive);
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
        public static void InsertOrReplaceWithChildren(this SQLiteConnection conn, object element, bool recursive = false) {
            conn.InsertWithChildrenRecursive(element, true, recursive);
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
        public static void InsertAllWithChildren(this SQLiteConnection conn, IEnumerable elements, bool recursive = false) {
            conn.InsertAllWithChildrenRecursive(elements, false, recursive);
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
        public static void InsertOrReplaceAllWithChildren(this SQLiteConnection conn, IEnumerable elements, bool recursive = false) {
            conn.InsertAllWithChildrenRecursive(elements, true, recursive);
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
        public static void DeleteAll(this SQLiteConnection conn, IEnumerable objects, bool recursive = false) {
            conn.DeleteAllRecursive(objects, recursive);
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
        public static void Delete(this SQLiteConnection conn, object element, bool recursive) {
            if (recursive)
                conn.DeleteAll(new []{ element }, recursive);
            else
                conn.Delete(element);
        }

        /// <summary>
        /// Deletes all the objects passed with IDs equal to the passed parameters from the database.
        /// Relationships are not taken into account in this method
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="primaryKeyValues">Primary keys of the objects to be deleted from the database</param>
        /// <typeparam name="T">The Entity type, it should match de database entity type</typeparam>
        public static void DeleteAllIds<T>(this SQLiteConnection conn, IEnumerable<object> primaryKeyValues) {
            var type = typeof(T);
            var primaryKeyProperty = type.GetPrimaryKey();

            conn.DeleteAllIds(primaryKeyValues.ToArray(), type.GetTableName(), primaryKeyProperty.GetColumnName());
        }


        #region Private methods
        static void InsertAllWithChildrenRecursive(this SQLiteConnection conn, IEnumerable elements, bool replace, bool recursive, ISet<object> objectCache = null) {
            if (elements == null)
                return;

            objectCache = objectCache ?? new HashSet<object>();
            var insertedElements = conn.InsertElements(elements, replace, objectCache).Cast<object>().ToList();

            foreach (var element in insertedElements) {
                conn.InsertChildrenRecursive(element, replace, recursive, objectCache);
            }

            foreach (var element in insertedElements) {
                conn.UpdateWithChildren(element);
            }
        }

        static void InsertWithChildrenRecursive(this SQLiteConnection conn, object element, bool replace, bool recursive, ISet<object> objectCache = null) {
            objectCache = objectCache ?? new HashSet<object>();
            if (objectCache.Contains(element))
                return;

            conn.InsertElement(element, replace, objectCache);

            objectCache.Add(element);
            conn.InsertChildrenRecursive(element, replace, recursive, objectCache);

            conn.UpdateWithChildren(element);
        }

        static void InsertChildrenRecursive(this SQLiteConnection conn, object element, bool replace, bool recursive, ISet<object> objectCache = null) {
            if (element == null)
                return;

            objectCache = objectCache ?? new HashSet<object>();
            foreach (var relationshipProperty in element.GetType().GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                // Ignore read-only attributes and process only 'CascadeInsert' attributes
                if (relationshipAttribute.ReadOnly || !relationshipAttribute.IsCascadeInsert)
                    continue;

                var value = relationshipProperty.GetValue(element, null);
                conn.InsertValue(value, replace, recursive, objectCache);
            }
        }

        static void InsertValue(this SQLiteConnection conn, object value, bool replace, bool recursive, ISet<object> objectCache) {
            if (value == null)
                return;

            var enumerable = value as IEnumerable;
            if (recursive)
            {
                if (enumerable != null)
                    conn.InsertAllWithChildrenRecursive(enumerable, replace, recursive, objectCache);
                else
                    conn.InsertWithChildrenRecursive(value, replace, recursive, objectCache);
            }
            else
            {
                if (enumerable != null)
                    conn.InsertElements(enumerable, replace, objectCache);
                else
                    conn.InsertElement(value, replace, objectCache);
            }
        }

        static IEnumerable InsertElements(this SQLiteConnection conn, IEnumerable elements, bool replace, ISet<object> objectCache) {
            if (elements == null)
                return Enumerable.Empty<object>();

            objectCache = objectCache ?? new HashSet<object>();
            var elementsToInsert = elements.Cast<object>().Except(objectCache).ToList();
            if (elementsToInsert.Count == 0)
                return Enumerable.Empty<object>();

            var primaryKeyProperty = elementsToInsert[0].GetType().GetPrimaryKey();
            var isAutoIncrementPrimaryKey = primaryKeyProperty != null && primaryKeyProperty.GetAttribute<AutoIncrementAttribute>() != null;

            foreach (var element in elementsToInsert) {
                conn.InsertElement(element, replace, primaryKeyProperty, isAutoIncrementPrimaryKey, objectCache);
                objectCache.Add(element);
            }

            return elementsToInsert;
        }

        static void InsertElement(this SQLiteConnection conn, object element, bool replace, ISet<object> objectCache) {
            var primaryKeyProperty = element.GetType().GetPrimaryKey();
            var isAutoIncrementPrimaryKey = primaryKeyProperty != null && primaryKeyProperty.GetAttribute<AutoIncrementAttribute>() != null;

            conn.InsertElement(element, replace, primaryKeyProperty, isAutoIncrementPrimaryKey, objectCache);
        }

        static void InsertElement(this SQLiteConnection conn, object element, bool replace, PropertyInfo primaryKeyProperty, bool isAutoIncrementPrimaryKey, ISet<object> objectCache) {
            if (element == null || (objectCache != null && objectCache.Contains(element)))
                return;

            bool isPrimaryKeySet = false;
            if (replace && isAutoIncrementPrimaryKey)
            {
                var primaryKeyValue = primaryKeyProperty.GetValue(element, null);
                var defaultPrimaryKeyValue = primaryKeyProperty.PropertyType.GetDefault();
                isPrimaryKeySet = primaryKeyValue != null && !primaryKeyValue.Equals(defaultPrimaryKeyValue);
            }

            bool shouldReplace = replace && (!isAutoIncrementPrimaryKey || isPrimaryKeySet);

            // Only replace elements that have an assigned primary key
            if (shouldReplace)
                conn.InsertOrReplace(element);
            else
                conn.Insert(element);
        }

        static void DeleteAllRecursive(this SQLiteConnection conn, IEnumerable elements, bool recursive, ISet<object> objectCache = null) {
            if (elements == null)
                return;

            var isRootElement = objectCache == null;
            objectCache = objectCache ?? new HashSet<object>();

            var elementList = elements.Cast<object>().Except(objectCache).ToList();

            // Mark the objects for deletion
            foreach (var element in elementList)
                objectCache.Add(element);

            if (recursive)
            {
                foreach (var element in elementList)
                {
                    var type = element.GetType();
                    foreach (var relationshipProperty in type.GetRelationshipProperties())
                    {
                        var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                        // Ignore read-only attributes or those that are not marked as CascadeDelete
                        if (!relationshipAttribute.IsCascadeDelete || relationshipAttribute.ReadOnly)
                            continue;

                        var value = relationshipProperty.GetValue(element, null);
                        conn.DeleteValueRecursive(value, recursive, objectCache);
                    }
                }
            }

            // To improve performance, the root method call will delete all the objects at once
            if (isRootElement) {
                conn.DeleteAllObjects(objectCache);
            }
        }

        static void DeleteValueRecursive(this SQLiteConnection conn, object value, bool recursive, ISet<object> objectCache) {
            if (value == null)
                return;

            var enumerable = value as IEnumerable ?? new [] { value };
            conn.DeleteAllRecursive(enumerable, recursive, objectCache);
        }

        static void DeleteAllObjects(this SQLiteConnection conn, IEnumerable elements) {
            if (elements == null)
                return;

            var groupedElements = elements.Cast<object>().GroupBy(o => o.GetType());
            foreach (var groupElement in groupedElements)
            {
                var type = groupElement.Key;
                var primaryKeyProperty = type.GetPrimaryKey();
                Assert(primaryKeyProperty != null, type, null, "Cannot delete objects without primary key");
                var primaryKeyValues = (from element in groupElement
                                                    select primaryKeyProperty.GetValue(element, null)).ToArray();
                conn.DeleteAllIds(primaryKeyValues, type.GetTableName(), primaryKeyProperty.GetColumnName());
            }
        }

        private static void RefreshForeignKeys(object element)
        {
            var type = element.GetType();
            foreach (var relationshipProperty in type.GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                // Ignore read-only attributes
                if (relationshipAttribute.ReadOnly)
                    continue;

                if (relationshipAttribute is OneToOneAttribute || relationshipAttribute is ManyToOneAttribute)
                {
                    var foreignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
                    if (foreignKeyProperty != null)
                    {
                        EnclosedType enclosedType;
                        var entityType = relationshipProperty.GetEntityType(out enclosedType);
                        var destinationPrimaryKeyProperty = entityType.GetPrimaryKey();
                        Assert(enclosedType == EnclosedType.None, type, relationshipProperty,  "ToOne relationships cannot be lists or arrays");
                        Assert(destinationPrimaryKeyProperty != null, type, relationshipProperty,  "Found foreign key but destination Type doesn't have primary key");

                        var relationshipValue = relationshipProperty.GetValue(element, null);
                        object foreignKeyValue = null;
                        if (relationshipValue != null)
                        {
                            foreignKeyValue = destinationPrimaryKeyProperty.GetValue(relationshipValue, null);
                        }
                        foreignKeyProperty.SetValue(element, foreignKeyValue, null);
                    }
                }
                else if (relationshipAttribute is TextBlobAttribute)
                {
                    TextBlobOperations.UpdateTextBlobProperty(element, relationshipProperty);
                }
            }
        }


        private static void UpdateInverseForeignKeys(this SQLiteConnection conn, object element)
        {
            foreach (var relationshipProperty in element.GetType().GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

                // Ignore read-only attributes
                if (relationshipAttribute.ReadOnly)
                    continue;

                if (relationshipAttribute is OneToManyAttribute)
                {
                    conn.UpdateOneToManyInverseForeignKey(element, relationshipProperty);
                }
                else if (relationshipAttribute is OneToOneAttribute)
                {
                    conn.UpdateOneToOneInverseForeignKey(element, relationshipProperty);
                }
                else if (relationshipAttribute is ManyToManyAttribute)
                {
                    conn.UpdateManyToManyForeignKeys(element, relationshipProperty);
                }
            }
        }

        private static void UpdateOneToManyInverseForeignKey(this SQLiteConnection conn, object element, PropertyInfo relationshipProperty)
        {
            var type = element.GetType();

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var originPrimaryKeyProperty = type.GetPrimaryKey();
            var inversePrimaryKeyProperty = entityType.GetPrimaryKey();
            var inverseForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,  "OneToMany relationships must be List or Array of entities");
            Assert(originPrimaryKeyProperty != null, type, relationshipProperty,  "OneToMany relationships require Primary Key in the origin entity");
            Assert(inversePrimaryKeyProperty != null, type, relationshipProperty,  "OneToMany relationships require Primary Key in the destination entity");
            Assert(inverseForeignKeyProperty != null, type, relationshipProperty,  "Unable to find foreign key for OneToMany relationship");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                EnclosedType inverseEnclosedType;
                var inverseEntityType = inverseProperty.GetEntityType(out inverseEnclosedType);
                Assert(inverseEnclosedType == EnclosedType.None, type, relationshipProperty,  "OneToMany inverse relationship shouldn't be List or Array");
                Assert(inverseEntityType == type, type, relationshipProperty,  "OneToMany inverse relationship is not the expected type");
            }

            var keyValue = originPrimaryKeyProperty.GetValue(element, null);
            var children = (IEnumerable)relationshipProperty.GetValue(element, null);
            var childrenKeyList = new List<object>();
            if (children != null)
            {
                foreach (var child in children)
                {
                    var childKey = inversePrimaryKeyProperty.GetValue(child, null);
                    childrenKeyList.Add(childKey);

                    inverseForeignKeyProperty.SetValue(child, keyValue, null);
                    if (inverseProperty != null)
                    {
                        inverseProperty.SetValue(child, element, null);
                    }
                }
            }


            // Delete previous relationships
            var deleteQuery = string.Format("update [{0}] set [{1}] = NULL where [{1}] == ?",
                entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName());
            var deleteParamaters = new List<object> { keyValue };
            conn.Execute(deleteQuery, deleteParamaters.ToArray());

            var chunks = Split(childrenKeyList, queryLimit);
            var loopTo = chunks.Count == 0 ? 1 : chunks.Count;
            for (int i = 0; i < loopTo; i++) {
                var chunk = chunks.Count > i ? chunks[i] :new List<object>();
                // Objects already updated, now change the database
                var childrenPlaceHolders = string.Join(",", Enumerable.Repeat("?", chunk.Count));
                var query = string.Format("update [{0}] set [{1}] = ? where [{2}] in ({3})",
                    entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName(), childrenPlaceHolders);

                var parameters = new List<object> { keyValue };
                parameters.AddRange(chunk);
                conn.Execute(query, parameters.ToArray());
            }
        }

        private static void UpdateOneToOneInverseForeignKey(this SQLiteConnection conn, object element, PropertyInfo relationshipProperty)
        {
            var type = element.GetType();

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var originPrimaryKeyProperty = type.GetPrimaryKey();
            var inversePrimaryKeyProperty = entityType.GetPrimaryKey();
            var inverseForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);

            Assert(enclosedType == EnclosedType.None, type, relationshipProperty,  "OneToOne relationships cannot be List or Array of entities");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                EnclosedType inverseEnclosedType;
                var inverseEntityType = inverseProperty.GetEntityType(out inverseEnclosedType);
                Assert(inverseEnclosedType == EnclosedType.None, type, relationshipProperty,  "OneToOne inverse relationship shouldn't be List or Array");
                Assert(inverseEntityType == type, type, relationshipProperty,  "OneToOne inverse relationship is not the expected type");
            }

            object keyValue = null;
            if (originPrimaryKeyProperty != null && inverseForeignKeyProperty != null)
            {
                keyValue = originPrimaryKeyProperty.GetValue(element, null);
            }

            object childKey = null;
            var child = relationshipProperty.GetValue(element, null);
            if (child != null)
            {
                if (inverseForeignKeyProperty != null && keyValue != null)
                {
                    inverseForeignKeyProperty.SetValue(child, keyValue, null);
                }
                if (inverseProperty != null)
                {
                    inverseProperty.SetValue(child, element, null);
                }
                if (inversePrimaryKeyProperty != null)
                {
                    childKey = inversePrimaryKeyProperty.GetValue(child, null);
                }
            }


            // Objects already updated, now change the database
            if (inverseForeignKeyProperty != null && inversePrimaryKeyProperty != null)
            {
                var query = string.Format("update [{0}] set [{1}] = ? where [{2}] == ?",
                    entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName());
                conn.Execute(query, keyValue, childKey);

                // Delete previous relationships
                var deleteQuery = string.Format("update [{0}] set [{1}] = NULL where [{1}] == ? and [{2}] not in (?)",
                    entityType.GetTableName(), inverseForeignKeyProperty.GetColumnName(), inversePrimaryKeyProperty.GetColumnName());
                conn.Execute(deleteQuery, keyValue, childKey ?? "");
            }
        }

        private static void UpdateManyToManyForeignKeys(this SQLiteConnection conn, object element, PropertyInfo relationshipProperty)
        {
            var type = element.GetType();

            EnclosedType enclosedType;
            var entityType = relationshipProperty.GetEntityType(out enclosedType);

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            var manyToManyMetaInfo = type.GetManyToManyMetaInfo(relationshipProperty);
            var currentEntityForeignKeyProperty = manyToManyMetaInfo.OriginProperty;
            var otherEntityForeignKeyProperty = manyToManyMetaInfo.DestinationProperty;
            var intermediateType = manyToManyMetaInfo.IntermediateType;

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,  "ManyToMany relationship must be a List or Array");
            Assert(currentEntityPrimaryKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship origin must have Primary Key");
            Assert(otherEntityPrimaryKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship destination must have Primary Key");
            Assert(intermediateType != null, type, relationshipProperty,  "ManyToMany relationship intermediate type cannot be null");
            Assert(currentEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Assert(otherEntityForeignKeyProperty != null, type, relationshipProperty,  "ManyToMany relationship destination must have a foreign key defined in the intermediate type");

            var primaryKey = currentEntityPrimaryKeyProperty.GetValue(element, null);

            // Obtain the list of children keys
            var childList = (IEnumerable)relationshipProperty.GetValue(element, null);
            var childKeyList = (from object child in childList ?? new List<object>()
                                select otherEntityPrimaryKeyProperty.GetValue(child, null)).ToList();

            List<object> currentChildKeyList = new List<object>();
            var chunks = Split(childKeyList, queryLimit);
            var loopTo = chunks.Count == 0 ? 1 : chunks.Count;
            for (int i = 0; i < loopTo; i++) {
                var chunk = chunks.Count > i ? chunks[i] : new List<object>();
                // Check for already existing relationships
                var childrenPlaceHolders = string.Join(",", Enumerable.Repeat("?", chunk.Count));
                var currentChildrenQuery = string.Format("select [{0}] from [{1}] where [{2}] == ? and [{0}] in ({3})",
                    otherEntityForeignKeyProperty.GetColumnName(), intermediateType.GetTableName(), currentEntityForeignKeyProperty.GetColumnName(), childrenPlaceHolders);

                var parameters = new List<object> { primaryKey };
                parameters.AddRange(chunk);
                currentChildKeyList.AddRange(
                    from object child in
                        conn.Query(conn.GetMapping(intermediateType), currentChildrenQuery, parameters.ToArray())
                    select otherEntityForeignKeyProperty.GetValue(child, null));
            }

            // Insert missing relationships in the intermediate table
            var missingChildKeyList = childKeyList.Where(o => !currentChildKeyList.Contains(o)).ToList();
            var missingIntermediateObjects = new List<object>(missingChildKeyList.Count);
            foreach (var missingChildKey in missingChildKeyList)
            {
                var intermediateObject = Activator.CreateInstance(intermediateType);
                currentEntityForeignKeyProperty.SetValue(intermediateObject, primaryKey, null);
                otherEntityForeignKeyProperty.SetValue(intermediateObject, missingChildKey, null);

                missingIntermediateObjects.Add(intermediateObject);
            }

            conn.InsertAll(missingIntermediateObjects);



            for (int i = 0; i < loopTo; i++)
            {
                var chunk = chunks.Count > i ? chunks[i] : new List<object>();
                var childrenPlaceHolders = string.Join(",", Enumerable.Repeat("?", chunk.Count));

                // Delete any other pending relationship
                var deleteQuery = string.Format("delete from [{0}] where [{1}] == ? and [{2}] not in ({3})",
                    intermediateType.GetTableName(), currentEntityForeignKeyProperty.GetColumnName(),
                    otherEntityForeignKeyProperty.GetColumnName(), childrenPlaceHolders);

                var parameters = new List<object> { primaryKey };
                parameters.AddRange(chunk);
                conn.Execute(deleteQuery, parameters.ToArray());
            }
            
        }

        private static void DeleteAllIds(this SQLiteConnection conn, object[] primaryKeyValues, string entityName, string primaryKeyName) {
            if (primaryKeyValues == null || primaryKeyValues.Length == 0)
                return;

            if (primaryKeyValues.Length <= queryLimit)
            {
                var placeholdersString = string.Join(",", Enumerable.Repeat("?", primaryKeyValues.Length));
                var deleteQuery = string.Format("delete from [{0}] where [{1}] in ({2})", entityName, primaryKeyName, placeholdersString);

                conn.Execute(deleteQuery, primaryKeyValues);
            }
            else {
                foreach (var primaryKeys in Split(primaryKeyValues.ToList(), queryLimit)) {
                    conn.DeleteAllIds(primaryKeys.ToArray(), entityName, primaryKeyName);
                }

            }
        }

        static List<List<T>> Split<T>(List<T> items, int sliceSize = 30)
        {
            List<List<T>> list = new List<List<T>>();
            for (int i = 0; i < items.Count; i += sliceSize)
                list.Add(items.GetRange(i, Math.Min(sliceSize, items.Count - i)));
            return list;
        }

        static void Assert(bool assertion, Type type, PropertyInfo property, string message) {
            if (EnableRuntimeAssertions && !assertion)
                throw new IncorrectRelationshipException(type.Name, property != null ? property.Name : string.Empty , message);
        }
        #endregion
    }
}