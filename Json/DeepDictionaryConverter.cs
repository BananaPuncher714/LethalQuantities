using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LethalQuantities.Json
{
    public class DeepDictionaryConverter : JsonConverter
    {
        private JsonSerializerSettings settings { get; set; }

        public DeepDictionaryConverter(JsonSerializerSettings settings) {
            this.settings = settings;
        }

        public override bool CanConvert(Type objectType)
        {
            return (typeof(IDictionary).IsAssignableFrom(objectType) ||
                    TypeImplementsGenericInterface(objectType, typeof(IDictionary<,>)));
        }

        private static bool TypeImplementsGenericInterface(Type concreteType, Type interfaceType)
        {
            return concreteType.GetInterfaces()
                   .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IDictionary dict = (IDictionary) value;
            writer.WriteObject(() =>
            {
                foreach (DictionaryEntry entry in dict) {
                    writer.WritePropertyName(JsonConvert.SerializeObject(entry.Key, settings));
                    serializer.Serialize(writer, entry.Value);
                }
            });
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
