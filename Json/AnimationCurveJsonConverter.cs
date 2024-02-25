using Newtonsoft.Json;
using System;
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
                return new AnimationCurve(new Keyframe(0, (float) reader.ReadAsDouble()));
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                reader.Read();
                AnimationCurve curve = new AnimationCurve();
                while (reader.TokenType != JsonToken.EndArray) {
                    reader.Read();
                    float time = float.Parse(reader.ReadAsString());
                    reader.Read();
                    float value = float.Parse(reader.ReadAsString());
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
