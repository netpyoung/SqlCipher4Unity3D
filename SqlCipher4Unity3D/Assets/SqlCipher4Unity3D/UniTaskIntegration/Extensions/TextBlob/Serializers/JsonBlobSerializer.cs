namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.TextBlob.Serializers
{
    using System;
    using Newtonsoft.Json;

    public class JsonBlobSerializer : ITextBlobSerializer
    {
        public string Serialize(object element)
        {
            return JsonConvert.SerializeObject(element);
        }
    
        public object Deserialize(string text, Type type)
        {
            return JsonConvert.DeserializeObject(text, type);
        }
    }
}