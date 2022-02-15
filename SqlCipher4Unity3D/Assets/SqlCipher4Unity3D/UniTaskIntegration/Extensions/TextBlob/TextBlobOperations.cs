namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.TextBlob
{
    using System.Reflection;
    using Attributes;
    using Serializers;

    public static class TextBlobOperations
    {
        private static ITextBlobSerializer _serializer;

        public static void SetTextSerializer(ITextBlobSerializer serializer)
        {
            _serializer = serializer;
        }

        public static ITextBlobSerializer GetTextSerializer()
        {
            // If not specified, use default JSON serializer
            return _serializer ?? (_serializer = new JsonBlobSerializer());
        }

        public static void GetTextBlobChild(object element, PropertyInfo relationshipProperty)
        {
            var type = element.GetType();
            var relationshipType = relationshipProperty.PropertyType;
            
            // Debug.Assert(relationshipType != typeof(string), "TextBlob property is already a string");

            var textblobAttribute = relationshipProperty.GetAttribute<TextBlobAttribute>();
            var textProperty = type.GetRuntimeProperty(textblobAttribute.TextProperty);
            // Debug.Assert(textProperty != null && textProperty.PropertyType == typeof(string), "Text property for TextBlob relationship not found");
            
            var textValue = (string)textProperty.GetValue(element, null);
            var value = textValue != null ? GetTextSerializer().Deserialize(textValue, relationshipType) : null;

            relationshipProperty.SetValue(element, value, null);
        }

        public static void UpdateTextBlobProperty(object element, PropertyInfo relationshipProperty)
        {
            var type = element.GetType();
            var relationshipType = relationshipProperty.PropertyType;

            // Debug.Assert(relationshipType != typeof(string), "TextBlob property is already a string");

            var textblobAttribute = relationshipProperty.GetAttribute<TextBlobAttribute>();
            var textProperty = type.GetRuntimeProperty(textblobAttribute.TextProperty);
            // Debug.Assert(textProperty != null && textProperty.PropertyType == typeof(string), "Text property for TextBlob relationship not found");

            var value = relationshipProperty.GetValue(element, null);
            var textValue = value != null ? GetTextSerializer().Serialize(value) : null;

            textProperty.SetValue(element, textValue, null);
        }
    }
}