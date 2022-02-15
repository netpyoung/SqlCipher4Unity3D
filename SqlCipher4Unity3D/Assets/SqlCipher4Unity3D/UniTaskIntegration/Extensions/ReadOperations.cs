using ObjectCache =
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<object, object>>;


namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Attributes;
    using Exceptions;
    using SqlCipher4Unity3D;
    using TextBlob;

    public static class ReadOperations
    {
        #region Public API

        /// <summary>
        /// Enable to allow descriptive error descriptions on incorrect relationships. Enabled by default.
        /// Disable for production environments to remove the checks and reduce performance penalty
        /// </summary>
        public static bool EnableRuntimeAssertions = true;

        /// <summary>
        /// Fetches all the entities of the specified type with the filter and fetches all the relationship
        /// properties of all the returned elements.
        /// </summary>
        /// <returns>List of all the elements of the type T that matches the filter with the children already loaded</returns>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="filter">Filter that will be passed to the <c>Where</c> clause when fetching
        /// objects from the database. No relationship properties are allowed in this filter as they
        /// are loaded afterwards</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static List<T> GetAllWithChildren<T>(this SQLiteConnection conn, Expression<Func<T, bool>> filter = null,
            bool recursive = false)
            where T : new()
        {
            var elements = conn.Table<T>();
            if (filter != null)
            {
                elements = elements.Where(filter);
            }

            var list = elements.ToList();

            foreach (T element in list)
            {
                conn.GetChildren(element, recursive);
            }

            return list;
        }

        /// <summary>
        /// Obtains the object from the database and fetches all the properties annotated with
        /// any subclass of <c>RelationshipAttribute</c>. If the object with the specified primary key doesn't
        /// exist in the database, an exception will be raised.
        /// </summary>
        /// <returns>The object with all the children loaded</returns>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="pk">Primary key for the object to search in the database</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static T GetWithChildren<T>(this SQLiteConnection conn, object pk, bool recursive = false)
            where T : new()
        {
            var element = conn.Get<T>(pk);
            conn.GetChildren(element, recursive);
            return element;
        }

        /// <summary>
        /// The behavior is the same that <c>GetWithChildren</c> but it returns null if the object doesn't
        /// exist in the database instead of throwing an exception
        /// Obtains the object from the database and fetch all the properties annotated with
        /// any subclass of <c>RelationshipAttribute</c>. If the object with the specified primary key doesn't
        /// exist in the database, it will return null
        /// </summary>
        /// <returns>The object with all the children loaded or null if it doesn't exist</returns>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="pk">Primary key for the object to search in the database</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static T FindWithChildren<T>(this SQLiteConnection conn, object pk, bool recursive = false)
            where T : new()
        {
            var element = conn.Find<T>(pk);
            if (!EqualityComparer<T>.Default.Equals(element, default(T)))
                conn.GetChildren(element, recursive);
            return element;
        }

        /// <summary>
        /// Fetches all the properties annotated with any subclass of <c>RelationshipAttribute</c> of the current
        /// object and keeps fetching recursively if the <c>recursive</c> flag has been set.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Element used to load all the relationship properties</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static void GetChildren<T>(this SQLiteConnection conn, T element, bool recursive = false)
        {
            GetChildrenRecursive(conn, element, false, recursive);
        }

        /// <summary>
        /// Fetches a specific property of the current object and keeps fetching recursively if the
        /// <c>recursive</c> flag has been set.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Element used to load all the relationship properties</param>
        /// <param name="relationshipProperty">Name of the property to fetch from the database</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static void GetChild<T>(this SQLiteConnection conn, T element, string relationshipProperty,
            bool recursive = false)
        {
            conn.GetChild(element, element.GetType().GetRuntimeProperty(relationshipProperty), recursive);
        }

        /// <summary>
        /// Fetches a specific property of the current object and keeps fetching recursively if the
        /// <c>recursive</c> flag has been set.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Element used to load all the relationship properties</param>
        /// <param name="propertyExpression">Expression that returns the property to be loaded from the database.
        /// This variant is useful to avoid spelling mistakes and make the code refactor-safe.</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static void GetChild<T>(this SQLiteConnection conn, T element,
            Expression<Func<T, object>> propertyExpression, bool recursive = false)
        {
            conn.GetChild(element, ReflectionExtensions.GetProperty(propertyExpression), recursive);
        }

        /// <summary>
        /// Fetches a specific property of the current object and keeps fetching recursively if the
        /// <c>recursive</c> flag has been set.
        /// </summary>
        /// <param name="conn">SQLite Net connection object</param>
        /// <param name="element">Element used to load all the relationship properties</param>
        /// <param name="relationshipProperty">Property to load from the database</param>
        /// <param name="recursive">If set to <c>true</c> all the relationships with
        /// <c>CascadeOperation.CascadeRead</c> will be loaded recusively.</param>
        /// <typeparam name="T">Entity type where the object should be fetched from</typeparam>
        public static void GetChild<T>(this SQLiteConnection conn, T element, PropertyInfo relationshipProperty,
            bool recursive = false)
        {
            conn.GetChildRecursive(element, relationshipProperty, recursive, new ObjectCache());
        }

        #endregion

        #region Private methods

        private static void GetChildrenRecursive(this SQLiteConnection conn, object element, bool onlyCascadeChildren,
            bool recursive, ObjectCache objectCache = null)
        {
            objectCache = objectCache ?? new ObjectCache();

            foreach (var relationshipProperty in element.GetType().GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
                if (!onlyCascadeChildren || relationshipAttribute.IsCascadeRead)
                {
                    conn.GetChildRecursive(element, relationshipProperty, recursive, objectCache);
                }
                else if (relationshipAttribute is TextBlobAttribute)
                {
                    conn.GetChildRecursive(element, relationshipProperty, false, objectCache);
                }
            }
        }

        private static void GetChildRecursive(this SQLiteConnection conn, object element,
            PropertyInfo relationshipProperty, bool recursive, ObjectCache objectCache)
        {
            var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

            switch (relationshipAttribute)
            {
                case OneToOneAttribute _:
                    conn.GetOneToOneChildren(new List<object> { element }, relationshipProperty, recursive, objectCache);
                    break;
                case OneToManyAttribute _:
                    conn.GetOneToManyChildren(element, relationshipProperty, recursive, objectCache);
                    break;
                case ManyToOneAttribute _:
                    conn.GetManyToOneChildren(new List<object> { element }, relationshipProperty, recursive,
                        objectCache);
                    break;
                case ManyToManyAttribute _:
                    conn.GetManyToManyChildren(element, relationshipProperty, recursive, objectCache);
                    break;
                case TextBlobAttribute _:
                    TextBlobOperations.GetTextBlobChild(element, relationshipProperty);
                    break;
            }
        }

        private static object GetOneToOneChildren<T>(this SQLiteConnection conn, IList<T> elements,
            PropertyInfo relationshipProperty,
            bool recursive, ObjectCache objectCache)
        {
            var primaryKeys = new Dictionary<object, IList<T>>();
            var type = elements[0].GetType();
            
            var entityType = relationshipProperty.GetEntityType(out var enclosedType);

            Assert(enclosedType == EnclosedType.None, type, relationshipProperty,
                "OneToOne relationship cannot be of type List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            
            Assert(currentEntityPrimaryKeyProperty != null || otherEntityPrimaryKeyProperty != null, type,
                relationshipProperty,
                "At least one entity in a OneToOne relationship must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            
            Assert(currentEntityForeignKeyProperty != null || otherEntityForeignKeyProperty != null, type,
                relationshipProperty,
                "At least one entity in a OneToOne relationship must have Foreign Key");

            var hasForeignKey = otherEntityPrimaryKeyProperty != null && currentEntityForeignKeyProperty != null;
            var hasInverseForeignKey = currentEntityPrimaryKeyProperty != null && otherEntityForeignKeyProperty != null;
            
            Assert(hasForeignKey || hasInverseForeignKey, type, relationshipProperty,
                "Missing either ForeignKey or PrimaryKey for a complete OneToOne relationship");

            var tableMapping = conn.GetMapping(entityType);
            
            Assert(tableMapping != null, type, relationshipProperty,
                "There's no mapping table for OneToOne relationship");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);

            foreach (var element in elements)
            {
                var isLoadedFromCache = false;
                var value = default(T);
                object keyValue = null;

                if (hasForeignKey)
                {
                    keyValue = currentEntityForeignKeyProperty.GetValue(element, null);
                    if (keyValue != null)
                    {
                        // Try to load from cache when possible
                        if (recursive)
                        {
                            value = (T)GetObjectFromCache(entityType, keyValue, objectCache);
                        }

                        if (value != null)
                        {
                            isLoadedFromCache = true;
                        }
                    }
                }
                else
                {
                    keyValue = currentEntityPrimaryKeyProperty?.GetValue(element, null);
                    // Try to replace the loaded entity with the same object from the cache whenever possible
                    value = recursive
                        ? (T)ReplaceWithCacheObjectIfPossible(value, otherEntityPrimaryKeyProperty, objectCache,
                            out isLoadedFromCache)
                        : value;
                }

                if (isLoadedFromCache)
                {
                    relationshipProperty.SetValue(element, value, null);
                    if (value != null && inverseProperty != null)
                    {
                        inverseProperty.SetValue(value, element, null);
                    }
                }
                else
                {
                    if (keyValue != null)
                    {
                        AddPrimaryKeyToDictionary<T>(keyValue, element, primaryKeys);
                    }
                }
            }

            if (primaryKeys.Count <= 0) return elements[0];
            {
                string columnName;
                if (otherEntityForeignKeyProperty != null)
                {
                    columnName = otherEntityForeignKeyProperty.GetColumnName();
                }
                else
                {
                    columnName = tableMapping?.PK.Name;
                }

                var placeHolders = string.Join(",", Enumerable.Repeat("?", primaryKeys.Count));

                var query = string.Format("select * from [{0}] where [{1}] in ({2})", tableMapping?.TableName,
                    columnName, placeHolders);

                IList<object> values = conn.Query(tableMapping, query, primaryKeys.Keys.ToArray());

                if (values.Count <= 0) return elements[0];

                var keyProperty = otherEntityForeignKeyProperty ?? values[0].GetType().GetPrimaryKey();

                foreach (var value in values)
                {
                    var keyValue = keyProperty.GetValue(value);

                    if (!primaryKeys.TryGetValue(keyValue, out var keyElements)) continue;

                    foreach (var keyElement in keyElements)
                    {
                        relationshipProperty.SetValue(keyElement, value, null);
                        if (value != null && inverseProperty != null)
                        {
                            inverseProperty.SetValue(value, keyElement, null);
                        }

                        if (value != null && recursive)
                        {
                            SaveObjectToCache(value, otherEntityPrimaryKeyProperty.GetValue(value, null), objectCache);
                            conn.GetChildrenRecursive(value, true, recursive, objectCache);
                        }
                    }
                }
            }
            return elements[0];
        }


        private static object GetManyToOneChildren<T>(this SQLiteConnection conn, IList<T> elements,
            PropertyInfo relationshipProperty,
            bool recursive, ObjectCache objectCache)
        {
            var primaryKeys = new Dictionary<object, IList<T>>();
            var type = elements[0].GetType();

            var entityType = relationshipProperty.GetEntityType(out var enclosedType);

            Assert(enclosedType == EnclosedType.None, type, relationshipProperty,
                "ManyToOne relationship cannot be of type List or Array");

            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();
            Assert(otherEntityPrimaryKeyProperty != null, type, relationshipProperty,
                "ManyToOne relationship destination must have Primary Key");

            var currentEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty);
            Assert(currentEntityForeignKeyProperty != null, type, relationshipProperty,
                "ManyToOne relationship origin must have Foreign Key");

            var tableMapping = conn.GetMapping(entityType);
            Assert(tableMapping != null, type, relationshipProperty,
                "There's no mapping table for OneToMany relationship destination");

            foreach (var element in elements)
            {
                object value = null;
                var isLoadedFromCache = false;
                var foreignKeyValue = currentEntityForeignKeyProperty?.GetValue(element, null);
                if (foreignKeyValue != null)
                {
                    // Try to load from cache when possible
                    if (recursive)
                    {
                        value = GetObjectFromCache(entityType, foreignKeyValue, objectCache);
                    }

                    if (value == null)
                    {
                        AddPrimaryKeyToDictionary<T>(foreignKeyValue, element, primaryKeys);
                    }

                    else
                    {
                        isLoadedFromCache = true;
                    }
                }

                if (isLoadedFromCache)
                {
                    relationshipProperty.SetValue(element, value, null);
                }
            }

            if (primaryKeys.Count <= 0) return elements[0];
            {
                var placeHolders = string.Join(",", Enumerable.Repeat("?", primaryKeys.Count));

                var query = string.Format("select * from [{0}] where [{1}] in ({2})", tableMapping?.TableName,
                    tableMapping?.PK.Name, placeHolders);

                IList<object> values = conn.Query(tableMapping, query, primaryKeys.Keys.ToArray());

                if (values.Count <= 0) return elements[0];

                var keyProperty = values[0].GetType().GetPrimaryKey();

                foreach (var value in values)
                {
                    var keyValue = keyProperty.GetValue(value);

                    if (!primaryKeys.TryGetValue(keyValue, out var keyElements)) continue;

                    foreach (var keyElement in keyElements)
                    {
                        relationshipProperty.SetValue(keyElement, value, null);
                        if (value == null || !recursive) continue;

                        SaveObjectToCache(value, otherEntityPrimaryKeyProperty?.GetValue(value, null),
                            objectCache);

                        conn.GetChildrenRecursive(value, true, recursive, objectCache);
                    }
                }
            }

            return elements[0];
        }

        private static void AddPrimaryKeyToDictionary<T>(object key, T element,
            Dictionary<object, IList<T>> primaryKeys)
        {
            if (!primaryKeys.TryGetValue(key, out var list))
            {
                list = new List<T>() { element };
                primaryKeys.Add(key, list);
            }
            else
            {
                list.Add(element);
            }
        }

        private static IEnumerable GetOneToManyChildren<T>(this SQLiteConnection conn, T element,
            PropertyInfo relationshipProperty,
            bool recursive, ObjectCache objectCache)
        {
            var type = element.GetType();

            var entityType = relationshipProperty.GetEntityType(out var enclosedType);

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,
                "OneToMany relationship must be a List or Array");

            var currentEntityPrimaryKeyProperty = type.GetPrimaryKey();
            Assert(currentEntityPrimaryKeyProperty != null, type, relationshipProperty,
                "OneToMany relationship origin must have Primary Key");

            var otherEntityForeignKeyProperty = type.GetForeignKeyProperty(relationshipProperty, inverse: true);
            Assert(otherEntityForeignKeyProperty != null, type, relationshipProperty,
                "OneToMany relationship destination must have Foreign Key to the origin class");

            var otherEntityPrimaryKeyProperty = entityType.GetPrimaryKey();

            var tableMapping = conn.GetMapping(entityType);
            Assert(tableMapping != null, type, relationshipProperty,
                "There's no mapping table for OneToMany relationship destination");

            var inverseProperty = type.GetInverseProperty(relationshipProperty);

            IList<T> cascadeElements = new List<T>();
            IList values = null;

            var primaryKeyValue = currentEntityPrimaryKeyProperty?.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                var query = string.Format("select * from [{0}] where [{1}] = ?", entityType.GetTableName(),
                    otherEntityForeignKeyProperty.GetColumnName());

                var queryResults = conn.Query(tableMapping, query, primaryKeyValue);

                Array array = null;

                values = enclosedType switch
                {
                    // Create a generic list of the expected type
                    EnclosedType.List => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType)),
                    EnclosedType.ObservableCollection => (IList)Activator.CreateInstance(
                        typeof(ObservableCollection<>).MakeGenericType(entityType)),
                    _ => array = Array.CreateInstance(entityType, queryResults.Count)
                };

                var i = 0;

                foreach (T result in queryResults)
                {
                    // Replace obtained value with a cached one whenever possible
                    var loadedFromCache = false;
                    var value = recursive
                        ? ReplaceWithCacheObjectIfPossible(result, otherEntityPrimaryKeyProperty, objectCache,
                            out loadedFromCache)
                        : result;

                    if (array != null)
                    {
                        array.SetValue(value, i);
                    }
                    else
                    {
                        values.Add(value);
                    }

                    if (!loadedFromCache)
                    {
                        cascadeElements.Add(result);
                    }

                    i++;
                }
            }

            relationshipProperty.SetValue(element, values, null);

            if (inverseProperty != null && values != null)
            {
                // Stablish inverse relationships (we already have that object anyway)
                foreach (var value in values)
                {
                    inverseProperty.SetValue(value, element, null);
                }
            }

            if (recursive)
            {
                if (cascadeElements.Count > 0)
                {
                    conn.GetChildrenRecursiveBatched(cascadeElements, objectCache);
                }
            }

            return values;
        }

        private static void GetChildrenRecursiveBatched<T>(this SQLiteConnection conn, IList<T> elements,
            ObjectCache objectCache)
        {
            var element = elements[0];
            foreach (var relationshipProperty in element.GetType().GetRelationshipProperties())
            {
                var relationshipAttribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
                if (relationshipAttribute.IsCascadeRead)
                {
                    if (relationshipAttribute is OneToOneAttribute)
                    {
                        conn.GetOneToOneChildren(elements, relationshipProperty, true, objectCache);
                    }
                    else if (relationshipAttribute is OneToManyAttribute)
                    {
                        foreach (var e in elements)
                        {
                            conn.GetOneToManyChildren(e, relationshipProperty, true, objectCache);
                        }
                    }
                    else if (relationshipAttribute is ManyToOneAttribute)
                    {
                        conn.GetManyToOneChildren(elements, relationshipProperty, true, objectCache);
                    }
                    else if (relationshipAttribute is ManyToManyAttribute)
                    {
                        foreach (var e in elements)
                        {
                            conn.GetManyToManyChildren(e, relationshipProperty, true, objectCache);
                        }
                    }
                    else if (relationshipAttribute is TextBlobAttribute)
                    {
                        TextBlobOperations.GetTextBlobChild(element, relationshipProperty);
                    }
                }
                else if (relationshipAttribute is TextBlobAttribute)
                {
                    foreach (var e in elements)
                    {
                        conn.GetChildRecursive(e, relationshipProperty, false, objectCache);
                    }
                }
            }
        }

        private static IEnumerable GetManyToManyChildren<T>(this SQLiteConnection conn, T element,
            PropertyInfo relationshipProperty,
            bool recursive, ObjectCache objectCache)
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
            var tableMapping = conn.GetMapping(entityType);

            Assert(enclosedType != EnclosedType.None, type, relationshipProperty,
                "ManyToMany relationship must be a List or Array");
            Assert(currentEntityPrimaryKeyProperty != null, type, relationshipProperty,
                "ManyToMany relationship origin must have Primary Key");
            Assert(otherEntityPrimaryKeyProperty != null, type, relationshipProperty,
                "ManyToMany relationship destination must have Primary Key");
            Assert(intermediateType != null, type, relationshipProperty,
                "ManyToMany relationship intermediate type cannot be null");
            Assert(currentEntityForeignKeyProperty != null, type, relationshipProperty,
                "ManyToMany relationship origin must have a foreign key defined in the intermediate type");
            Assert(otherEntityForeignKeyProperty != null, type, relationshipProperty,
                "ManyToMany relationship destination must have a foreign key defined in the intermediate type");
            Assert(tableMapping != null, type, relationshipProperty,
                "There's no mapping table defined for ManyToMany relationship origin");

            IList cascadeElements = new List<object>();
            IList values = null;

            var primaryKeyValue = currentEntityPrimaryKeyProperty?.GetValue(element, null);
            if (primaryKeyValue != null)
            {
                // Obtain the relationship keys
                var keysQuery = string.Format("select [{0}] from [{1}] where [{2}] = ?",
                    otherEntityForeignKeyProperty.GetColumnName(),
                    intermediateType.GetTableName(), currentEntityForeignKeyProperty.GetColumnName());

                var query = string.Format("select * from [{0}] where [{1}] in ({2})", entityType.GetTableName(),
                    otherEntityPrimaryKeyProperty.GetColumnName(), keysQuery);

                var queryResults = conn.Query(tableMapping, query, primaryKeyValue);

                Array array = null;

                values = enclosedType switch
                {
                    // Create a generic list of the expected type
                    EnclosedType.List => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType)),
                    EnclosedType.ObservableCollection => (IList)Activator.CreateInstance(
                        typeof(ObservableCollection<>).MakeGenericType(entityType)),
                    _ => array = Array.CreateInstance(entityType, queryResults.Count)
                };

                var i = 0;
                foreach (var result in queryResults)
                {
                    // Replace obtained value with a cached one whenever possible
                    var loadedFromCache = false;
                    var value = recursive
                        ? ReplaceWithCacheObjectIfPossible(result, otherEntityPrimaryKeyProperty, objectCache,
                            out loadedFromCache)
                        : result;

                    if (array != null)
                    {
                        array.SetValue(value, i);
                    }
                    else
                    {
                        values.Add(value);
                    }

                    if (!loadedFromCache)
                    {
                        cascadeElements.Add(result);
                    }

                    i++;
                }
            }


            relationshipProperty.SetValue(element, values, null);

            if (recursive)
            {
                foreach (var child in cascadeElements)
                {
                    conn.GetChildrenRecursive(child, true, recursive, objectCache);
                }
            }

            return values;
        }

        static object ReplaceWithCacheObjectIfPossible(object element, PropertyInfo primaryKeyProperty,
            ObjectCache objectCache, out bool isLoadedFromCache)
        {
            isLoadedFromCache = false;

            if (element == null || primaryKeyProperty == null || objectCache == null)
            {
                return element;
            }

            object primaryKey = null;
            primaryKey = primaryKeyProperty.GetValue(element, null);

            if (primaryKey == null)
            {
                return element;
            }

            var cachedElement = GetObjectFromCache(element.GetType(), primaryKey, objectCache);
            object result;
            if (cachedElement != null)
            {
                result = cachedElement;
                isLoadedFromCache = true;
            }
            else
            {
                result = element;
                SaveObjectToCache(element, primaryKey, objectCache);
            }

            return result;
        }

        static void Assert(bool assertion, Type type, PropertyInfo property, string message)
        {
            if (EnableRuntimeAssertions && !assertion)
            {
                throw new IncorrectRelationshipException(type.Name, property.Name, message);
            }
        }

        static object GetObjectFromCache(Type objectType, object primaryKey, ObjectCache objectCache)
        {
            if (objectCache == null)
            {
                return null;
            }

            var typeName = objectType.FullName;
            Dictionary<object, object> typeDict = null;
            object value = null;
            
            if (typeName != null && objectCache.TryGetValue(typeName, out typeDict))
            {
                typeDict.TryGetValue(primaryKey, out value);
            }

            return value;
        }

        static void SaveObjectToCache(object element, object primaryKey, ObjectCache objectCache)
        {
            if (objectCache == null || primaryKey == null || element == null)
            {
                return;
            }

            var typeName = element.GetType().FullName;
            Dictionary<object, object> typeDict = null;
            if (!objectCache.TryGetValue(typeName, out typeDict))
            {
                typeDict = new Dictionary<object, object>();
                objectCache[typeName] = typeDict;
            }

            typeDict[primaryKey] = element;
        }

        #endregion
    }
}