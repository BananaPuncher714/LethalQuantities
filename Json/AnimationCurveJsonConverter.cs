using Newtonsoft.Json;
using System;
using System.Globalization;
using UnityEngine;

namespace LethalQuantities.Json
{
    public class AnimationCurveJsonConverter : JsonConverter<AnimationCurve>
    {
        public override AnimationCurve ReadJson(JsonReader reader, Type objectType, AnimationCurve existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return new AnimationCurve();
            }
            else if (reader.TokenType == JsonToken.Float)
            {
                return new AnimationCurve(new Keyframe(0, float.Parse(reader.ReadAsString(), CultureInfo.InvariantCulture)));
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                reader.Read();
                AnimationCurve curve = new AnimationCurve();
                while (reader.TokenType != JsonToken.EndArray) {
                    reader.Read();
                    string timeStr = reader.ReadAsString();
                    float time = 0;
                    try
                    {
                        time = float.Parse(timeStr, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        Plugin.LETHAL_LOGGER.LogError($"Encountered an invalid value for an animation curve key: '{timeStr}'");
                        Plugin.LETHAL_LOGGER.LogError($"Please find and replace the value with a valid number, using 0 for now.");
                    }
                    reader.Read();
                    string valueStr = reader.ReadAsString();
                    float value = 0;
                    try
                    {
                        value = float.Parse(valueStr, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        Plugin.LETHAL_LOGGER.LogError($"Encountered an invalid value for an animation curve value: '{valueStr}'");
                        Plugin.LETHAL_LOGGER.LogError($"Please find and replace the value with a valid number, using 0 for now.");
                    }
                    reader.Read();

                    curve.AddKey(time, value);

                    if (reader.TokenType != JsonToken.EndArray)
                    {
                        reader.Read();
                    }
                }
                return curve;
            }
            return new AnimationCurve();
        }

        public override void WriteJson(JsonWriter writer, AnimationCurve value, JsonSerializer serializer)
        {
            writer.WriteArray(value.keys, key =>
            {
                writer.WriteObject(() =>
                {
                    writer.WritePair("time", key.time);
                    writer.WritePair("value", key.value);
                });
            });
        }
    }
}
