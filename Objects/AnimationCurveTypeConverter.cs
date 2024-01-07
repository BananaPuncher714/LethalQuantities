using BepInEx.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LethalQuantities.Objects
{
    internal class AnimationCurveTypeConverter : TypeConverter
    {
        public AnimationCurveTypeConverter()
        {
            ConvertToObject = (input, type) =>
            {
                if (type != typeof(AnimationCurve))
                {
                    return new AnimationCurve();
                }

                // Strip all whitespace
                string[] frames = Regex.Replace(input, @"\s+", "").Split(",");

                if (frames.Length > 1)
                {
                    Keyframe[] newFrames = new Keyframe[frames.Length];
                    for (int i = 0; i < frames.Length; i++)
                    {
                        string[] frameInfo = frames[i].Split(":");
                        newFrames[i] = new Keyframe(float.Parse(frameInfo[0], CultureInfo.InvariantCulture), float.Parse(frameInfo[1], CultureInfo.InvariantCulture));
                    }
                    return new AnimationCurve(newFrames);
                }
                else if (!string.IsNullOrEmpty(frames[0]))
                {
                    return new AnimationCurve(new Keyframe(0, float.Parse(frames[0], CultureInfo.InvariantCulture)));
                }

                return new AnimationCurve();
            };

            ConvertToString = (input, type) =>
            {
                if (type != typeof(AnimationCurve))
                {
                    return "";
                }

                AnimationCurve curve = (AnimationCurve) input;
                Keyframe[] frames = curve.GetKeys();
                if (frames.Length == 0)
                {
                    return "";
                }
                else if (frames.Length == 1)
                {
                    return frames[0].value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < frames.Length; i++)
                    {
                        Keyframe keyframe = frames[i];
                        stringBuilder.Append(keyframe.time.ToString(CultureInfo.InvariantCulture));
                        stringBuilder.Append(":");
                        stringBuilder.Append(keyframe.value.ToString(CultureInfo.InvariantCulture));

                        if (i < frames.Length - 1)
                        {
                            stringBuilder.Append(", ");
                        }
                    }
                    return stringBuilder.ToString();
                }
            };
        }
    }
}
