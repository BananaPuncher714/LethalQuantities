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
                // TODO Improve this
                reader.Read();
                AnimationCurve curve = new AnimationCurve();
                while (reader.TokenType != JsonToken.EndArray) {
                    // Should always be an object that contains a time and value
                    if (reader.TokenType == JsonToken.StartObject) {
                        Optional<float> time = Optional<float>.Empty();
                        Optional<float> value = Optional<float>.Empty();

                        reader.Read();
                        while (reader.TokenType != JsonToken.EndObject)
                        {
                            string keyType = reader.Value.ToString();
                            string keyVal = reader.ReadAsString();
                            float keyFloat = 0;
                            try
                            {
                                keyFloat = float.Parse(keyVal, CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                MiniLogger.LogError($"Encountered an invalid value for an animation curve {keyType}: '{keyVal}'");
                                MiniLogger.LogError($"Please find and replace the value with a valid number, using 0 for now.");
                            }
                            if (keyType == "time")
                            {
                                time.setValue(keyFloat);
                            }
                            else if (keyType == "value")
                            {
                                value.setValue(keyFloat);
                            }

                            if (reader.TokenType != JsonToken.EndObject)
                            {
                                reader.Read();
                            }
                        }
                        reader.Read();

                        if (time.isSet() && value.isSet())
                        {
                            curve.AddKey(time.value, value.value);
                        }
                    }
                    else
                    {
                        // Skip, I guess?
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
