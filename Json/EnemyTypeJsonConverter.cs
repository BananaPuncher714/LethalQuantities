using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalQuantities.Json
{
    internal class EnemyTypeJsonConverter : JsonConverter<EnemyType>
    {
        public override EnemyType ReadJson(JsonReader reader, Type objectType, EnemyType existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, EnemyType value, JsonSerializer serializer)
        {
            writer.WriteValue(value.name);
        }
    }
}
