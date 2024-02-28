using Newtonsoft.Json;
using System;

namespace LethalQuantities.Json
{
    internal class ItemJsonConverter : JsonConverter<Item>
    {
        public override Item ReadJson(JsonReader reader, Type objectType, Item existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Item value, JsonSerializer serializer)
        {
            writer.WriteValue(value.name);
        }
    }
}
