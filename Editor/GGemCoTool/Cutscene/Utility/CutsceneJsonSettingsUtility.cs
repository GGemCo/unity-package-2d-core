using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneJsonSettingsUtility
    {
        public static readonly JsonSerializerSettings CutsceneJsonSettings = CreateCutsceneJsonSettings();

        private static JsonSerializerSettings CreateCutsceneJsonSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter>
                {
                    new UnityColorJsonConverter(),
                },
            };
        }

        private sealed class UnityColorJsonConverter : JsonConverter<Color>
        {
            public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("r");
                writer.WriteValue(value.r);
                writer.WritePropertyName("g");
                writer.WriteValue(value.g);
                writer.WritePropertyName("b");
                writer.WriteValue(value.b);
                writer.WritePropertyName("a");
                writer.WriteValue(value.a);
                writer.WriteEndObject();
            }

            public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return default;
                }

                var token = JToken.Load(reader);
                if (token.Type != JTokenType.Object)
                {
                    return existingValue;
                }

                return new Color(
                    token.Value<float?>("r") ?? existingValue.r,
                    token.Value<float?>("g") ?? existingValue.g,
                    token.Value<float?>("b") ?? existingValue.b,
                    token.Value<float?>("a") ?? existingValue.a);
            }
        }
    }
}
