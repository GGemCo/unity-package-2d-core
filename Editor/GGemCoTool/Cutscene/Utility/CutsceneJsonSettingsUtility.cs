using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷신 JSON 직렬화/역직렬화에 공통으로 사용하는 JsonSerializerSettings를 제공합니다.
    /// Unity 전용 타입은 필요한 커스텀 컨버터를 통해 처리합니다.
    /// </summary>
    internal static class CutsceneJsonSettingsUtility
    {
        /// <summary>
        /// 컷신 데이터 직렬화와 역직렬화에 사용하는 공용 JSON 설정입니다.
        /// </summary>
        public static readonly JsonSerializerSettings CutsceneJsonSettings = CreateCutsceneJsonSettings();

        /// <summary>
        /// 컷신 JSON 처리에 사용할 기본 serializer 설정을 생성합니다.
        /// </summary>
        /// <returns>컷신 전용 JSON serializer 설정입니다.</returns>
        private static JsonSerializerSettings CreateCutsceneJsonSettings()
        {
            return new JsonSerializerSettings
            {
                // null 필드는 출력하지 않아 JSON 크기를 줄입니다.
                NullValueHandling = NullValueHandling.Ignore,

                // 순환 참조가 있어도 직렬화가 중단되지 않도록 무시합니다.
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

                // Unity 타입 직렬화를 위한 커스텀 컨버터를 등록합니다.
                Converters = new List<JsonConverter>
                {
                    new UnityColorJsonConverter(),
                },
            };
        }

        /// <summary>
        /// Unity <see cref="Color"/>를 JSON 객체로 직렬화하고 다시 복원하는 컨버터입니다.
        /// </summary>
        private sealed class UnityColorJsonConverter : JsonConverter<Color>
        {
            /// <summary>
            /// <see cref="Color"/> 값을 r, g, b, a 프로퍼티를 가진 JSON 객체로 기록합니다.
            /// </summary>
            /// <param name="writer">JSON 작성기입니다.</param>
            /// <param name="value">직렬화할 색상 값입니다.</param>
            /// <param name="serializer">현재 serializer 인스턴스입니다.</param>
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

            /// <summary>
            /// JSON 객체의 r, g, b, a 값을 읽어 <see cref="Color"/>로 복원합니다.
            /// 누락된 채널 값은 기존 색상 값을 유지합니다.
            /// </summary>
            /// <param name="reader">JSON 읽기기입니다.</param>
            /// <param name="objectType">역직렬화 대상 타입입니다.</param>
            /// <param name="existingValue">기존 색상 값입니다.</param>
            /// <param name="hasExistingValue">기존 값 존재 여부입니다.</param>
            /// <param name="serializer">현재 serializer 인스턴스입니다.</param>
            /// <returns>역직렬화된 색상 값입니다.</returns>
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