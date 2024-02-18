using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LethalQuantities.Json
{
    public static class JsonWriterExtension
    {
        public static void WriteObject(this JsonWriter writer, Action lambda)
        {
            writer.WriteStartObject();
            lambda();
            writer.WriteEndObject();
        }

        public static void WriteArray<T>(this JsonWriter writer, IEnumerable<T> collection, Action<T> lambda)
        {
            writer.WriteStartArray();
            foreach (T item in collection)
            {
                lambda(item);
            }
            writer.WriteEndArray();
        }

        public static void WritePair<T>(this JsonWriter writer, string name, T value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WriteProperty(this JsonWriter writer, string name, Action lambda)
        {
            writer.WritePropertyName(name);
            lambda();
        }
    }
}
