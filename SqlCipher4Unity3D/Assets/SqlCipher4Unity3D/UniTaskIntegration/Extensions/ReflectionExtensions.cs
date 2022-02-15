namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Attributes;
    using SQLite.Attributes;

    public enum EnclosedType
    {
        None,
        Array,
        List,
        ObservableCollection
    }

    public class ManyToManyMetaInfo
    {
        public Type IntermediateType { get; set; }
        public PropertyInfo OriginProperty { get; set; }
        public PropertyInfo DestinationProperty { get; set; }
    }

    public static class ReflectionExtensions
    {
        public static T GetAttribute<T>(this Type type) where T : Attribute  {
            T attribute = null;
            var attributes = (T[])type.GetTypeInfo().GetCustomAttributes(typeof(T), true);
            if (attributes.Length > 0)
            {
                attribute = attributes[0];
            }
            return attribute;
        }

        public static T GetAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            T attribute = null;
            var attributes = (T[])property.GetCustomAttributes(typeof(T), true);
            if (attributes.Length > 0)
            {
                attribute = attributes[0];
            }
            return attribute;
        }

        public static Type GetEntityType(this PropertyInfo property, out EnclosedType enclosedType)
        {
            var type = property.PropertyType;
            enclosedType = EnclosedType.None;

            var typeInfo = type.GetTypeInfo();
            if (type.IsArray)
            {
                type = type.GetElementType();
                enclosedType = EnclosedType.Array;
            }
            else if (typeInfo.IsGenericType && typeof(List<>).GetTypeInfo().IsAssignableFrom(type.GetGenericTypeDefinition().GetTypeInfo()))
            {
                type = typeInfo.GenericTypeArguments[0];
                enclosedType = EnclosedType.List;
            }
            else if (typeInfo.IsGenericType && typeof(ObservableCollection<>).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo().GetGenericTypeDefinition().GetTypeInfo()))
            {
                type = typeInfo.GenericTypeArguments[0];
                enclosedType = EnclosedType.ObservableCollection;
            }
            return type;
        }

        public static object GetDefault(this Type type)
        {
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static PropertyInfo GetExplicitForeignKeyProperty(this Type type, Type destinationType)
        {
            return (from property in type.GetRuntimeProperties() where property.IsPublicInstance()
                    let foreignKeyAttribute = property.GetAttribute<ForeignKeyAttribute>()
                    where foreignKeyAttribute != null && foreignKeyAttribute.ForeignType.GetTypeInfo().IsAssignableFrom(destinationType.GetTypeInfo())
                    select property)
                        .FirstOrDefault();
        }

        private static PropertyInfo GetConventionForeignKeyProperty(this Type type, string destinationTypeName)
        {
            var conventionFormats = new List<string> { "{0}Id", "{0}Key", "{0}ForeignKey" };

            var conventionNames = conventionFormats.Select(format => string.Format(format, destinationTypeName)).ToList();

            // No explicit declaration, search for convention names
            return (from property in type.GetRuntimeProperties()
                    where property.IsPublicInstance() && conventionNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase)
                    select property)
                        .FirstOrDefault();
        }

        public static PropertyInfo GetForeignKeyProperty(this Type type, PropertyInfo relationshipProperty, Type intermediateType = null, bool inverse = false)
        {
            PropertyInfo result;
            var attribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
            RelationshipAttribute inverseAttribute = null;

            EnclosedType enclosedType;
            var propertyType = relationshipProperty.GetEntityType(out enclosedType);

            var originType = intermediateType ?? (inverse ? propertyType : type);
            var destinationType = inverse ? type : propertyType;

            // Inverse relationships may have the foreign key declared in the inverse property relationship attribute
            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                inverseAttribute = inverseProperty.GetAttribute<RelationshipAttribute>();
            }

            if (!inverse && !string.IsNullOrEmpty(attribute.ForeignKey))
            {
                // Explicitly declared foreign key name
                result = originType.GetRuntimeProperty(attribute.ForeignKey);
            }
            else if (!inverse && inverseAttribute != null && !string.IsNullOrEmpty(inverseAttribute.InverseForeignKey))
            {
                // Explicitly declared inverse foreign key name in inverse property (double inverse refers to current entity foreign key)
                result = originType.GetRuntimeProperty(inverseAttribute.InverseForeignKey);
            }
            else if (inverse && !string.IsNullOrEmpty(attribute.InverseForeignKey))
            {
                // Explicitly declared inverse foreign key name
                result = originType.GetRuntimeProperty(attribute.InverseForeignKey);
            }
            else if (inverse && inverseAttribute != null && !string.IsNullOrEmpty(inverseAttribute.ForeignKey))
            {
                // Explicitly declared foreign key name in inverse property
                result = originType.GetRuntimeProperty(inverseAttribute.ForeignKey);
            }
            else
            {
                // Explicitly declared attribute
                result = originType.GetExplicitForeignKeyProperty(destinationType) ??
                    originType.GetConventionForeignKeyProperty(destinationType.Name);
            }

            return result;
        }


        public static PropertyInfo GetInverseProperty(this Type elementType, PropertyInfo property)
        {

            var attribute = property.GetAttribute<RelationshipAttribute>();
            if (attribute == null || (attribute.InverseProperty != null && attribute.InverseProperty.Equals("")))
            {
                // Relationship not reversible
                return null;
            }

            EnclosedType enclosedType;
            var propertyType = property.GetEntityType(out enclosedType);

            PropertyInfo result = null;
            if (attribute.InverseProperty != null)
            {
                result = propertyType.GetRuntimeProperty(attribute.InverseProperty);
            }
            else
            {
                var properties = (from p in propertyType.GetRuntimeProperties() where p.IsPublicInstance() select p);
                foreach (var inverseProperty in properties)
                {
                    var inverseAttribute = inverseProperty.GetAttribute<RelationshipAttribute>();
                    EnclosedType enclosedInverseType;
                    var inverseType = inverseProperty.GetEntityType(out enclosedInverseType);
                    if (inverseAttribute != null && inverseType.GetTypeInfo().Equals(elementType.GetTypeInfo()))
                    {
                        result = inverseProperty;
                        break;
                    }
                }
            }

            return result;
        }

        public static PropertyInfo GetProperty<T>(Expression<Func<T, object>> expression) {
            var type = typeof(T);
            var body = expression.Body as MemberExpression;
            // Debug.Assert(body != null, "Expression should be a property member expression");

            var propertyName = body.Member.Name;
            return type.GetRuntimeProperty(propertyName);
        }

        public static ManyToManyMetaInfo GetManyToManyMetaInfo(this Type type, PropertyInfo relationship)
        {
            var manyToManyAttribute = relationship.GetAttribute<ManyToManyAttribute>();
            // Debug.Assert(manyToManyAttribute != null, "Unable to find ManyToMany attribute");

            var intermediateType = manyToManyAttribute.IntermediateType;
            var destinationKeyProperty = type.GetForeignKeyProperty(relationship, intermediateType);
            var inverseKeyProperty = type.GetForeignKeyProperty(relationship, intermediateType, true);

            return new ManyToManyMetaInfo
                {
                    IntermediateType = intermediateType,
                    OriginProperty = inverseKeyProperty,
                    DestinationProperty = destinationKeyProperty
                };
        }

        public static List<PropertyInfo> GetRelationshipProperties(this Type type)
        {
            return (from property in type.GetRuntimeProperties()
                where property.IsPublicInstance() && property.GetAttribute<RelationshipAttribute>() != null
                select property).ToList();
        } 

        public static PropertyInfo GetPrimaryKey(this Type type)
        {
            return (from property in type.GetRuntimeProperties()
                    where property.IsPublicInstance() && property.GetAttribute<PrimaryKeyAttribute>() != null
                    select property).FirstOrDefault();
        }

        public static string GetTableName(this Type type) {
            var tableName = type.Name;
            var tableAttribute = type.GetAttribute<TableAttribute>();
            if (tableAttribute != null && tableAttribute.Name != null)
                tableName = tableAttribute.Name;

            return tableName;
        }

        public static string GetColumnName(this PropertyInfo property) {
            var column = property.Name;
            var columnAttribute = property.GetAttribute<ColumnAttribute>();
            if (columnAttribute != null && columnAttribute.Name != null)
                column = columnAttribute.Name;

            return column;
        }

        // Equivalent to old GetProperties(BindingFlags.Public | BindingFlags.Instance)
        private static bool IsPublicInstance(this PropertyInfo propertyInfo)
        {
            return propertyInfo != null &&
                   ((propertyInfo.GetMethod != null && !propertyInfo.GetMethod.IsStatic && propertyInfo.GetMethod.IsPublic) &&
                   (propertyInfo.SetMethod != null && !propertyInfo.SetMethod.IsStatic && propertyInfo.SetMethod.IsPublic));
        }
    }
}