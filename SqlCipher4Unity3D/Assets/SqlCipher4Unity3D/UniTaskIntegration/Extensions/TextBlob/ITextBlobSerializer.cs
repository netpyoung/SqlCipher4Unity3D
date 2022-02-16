namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.TextBlob
{
    using System;

    public interface ITextBlobSerializer
    {
        string Serialize(object element);
        object Deserialize(string text, Type type);
    }
}