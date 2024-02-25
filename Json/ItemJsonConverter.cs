using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
