using LethalQuantities.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace LethalQuantities.Json
{
    internal class SelectableLevelGuidJsonConverter : JsonConverter<Guid>
    {
        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            string levelName = "Unknown";
            try
            {
                levelName = value.getLevelName();
            }
            catch
            {
                MiniLogger.LogError($"Could not find a level with the guid {value}, is this being used correctly?");
            }
            writer.WriteValue(levelName);
        }
    }
}
