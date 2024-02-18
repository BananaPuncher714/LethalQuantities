using Newtonsoft.Json;
using System;
using UnityEngine;

namespace LethalQuantities.Json
{
    public class AnimationCurveConverter : JsonConverter<AnimationCurve>
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
                    curve.AddKey((float) reader.ReadAsDouble(), (float) reader.ReadAsDouble());
                }
                reader.Read();
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
