using LethalQuantities.Objects;
using Newtonsoft.Json;
using System;

namespace LethalQuantities.Json
{
    internal class DirectionalSpawnableMapObjectJsonConverter : JsonConverter<DirectionalSpawnableMapObject>
    {
        public override DirectionalSpawnableMapObject ReadJson(JsonReader reader, Type objectType, DirectionalSpawnableMapObject existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, DirectionalSpawnableMapObject value, JsonSerializer serializer)
        {
            writer.WriteValue(value.obj.name);
        }
    }
}
