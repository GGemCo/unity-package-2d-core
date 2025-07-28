using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public static class SpineJsonFileScanner
    {
        public static List<SpineJsonValidationResult> ValidateAllSkeletonJsons(string rootFolder)
        {
            var results = new List<SpineJsonValidationResult>();
            string[] files = Directory.GetFiles(rootFolder, "*.json", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (!IsSpineJson(file)) continue;

                var result = new SpineJsonValidationResult { FilePath = file };

                try
                {
                    string text = File.ReadAllText(file);
                    JObject root = JObject.Parse(text);

                    if (root.TryGetValue("animations", out var animationsToken) && animationsToken is JObject animationsObj)
                    {
                        foreach (var animProp in animationsObj.Properties())
                        {
                            string animationName = animProp.Name;
                            if (animProp.Value is not JObject animObj) continue;

                            if (animObj.TryGetValue("events", out var eventsToken) && eventsToken is JArray eventArray)
                            {
                                for (int i = 0; i < eventArray.Count; i++)
                                {
                                    var evt = eventArray[i];
                                    if (evt is not JObject evtObj) continue;

                                    var evtName = evtObj.TryGetValue("name", out var nameToken) ? nameToken.ToString() : $"(index:{i})";

                                    if (evtObj.TryGetValue("string", out var stringToken) && stringToken.Type == JTokenType.String)
                                    {
                                        string strVal = stringToken.ToString();
                                        try
                                        {
                                            JToken.Parse(strVal); // 문자열이 JSON 형식인지 검사
                                        }
                                        catch (JsonReaderException ex)
                                        {
                                            result.Errors.Add(new SpineJsonValidationResult.ValidationError
                                            {
                                                EventName = evtName,
                                                OriginalValue = strVal,
                                                ErrorMessage = ex.Message,
                                                JsonPath = $"animations.{animationName}.events[{i}].string"
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (result.Errors.Count > 0)
                        results.Add(result);
                }
                catch (JsonException ex)
                {
                    Debug.LogWarning($"[Spine JSON 파싱 실패] {file} : {ex.Message}");
                }
            }

            return results;
        }

        public static void UpdateJsonValue(string filePath, string jsonPath, string newValue)
        {
            string text = File.ReadAllText(filePath);
            JObject root = JObject.Parse(text);

            try
            {
                string[] parts = jsonPath.Split('.');
                JToken token = root;

                for (int i = 0; i < parts.Length; i++)
                {
                    string key = parts[i];

                    // 배열 접근 처리: events[3]
                    if (key.Contains('['))
                    {
                        int leftBracket = key.IndexOf('[');
                        string arrayName = key.Substring(0, leftBracket);
                        int index = int.Parse(key.Substring(leftBracket + 1, key.IndexOf(']') - leftBracket - 1));

                        token = token[arrayName];
                        if (token is JArray arr && index < arr.Count)
                            token = arr[index];
                        else
                            throw new JsonException($"잘못된 배열 경로: {key}");
                    }
                    else if (i < parts.Length - 1)
                    {
                        token = token[key];
                    }
                    else
                    {
                        // 마지막 키 → 수정
                        if (token[key] != null)
                            token[key] = newValue;
                        else
                            throw new JsonException($"경로에 해당하는 키가 없습니다: {key}");
                    }
                }

                File.WriteAllText(filePath, root.ToString(Formatting.Indented));
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SpineJsonFileScanner] JSON 업데이트 실패\n{ex}");
            }
        }

        private static bool IsSpineJson(string filePath)
        {
            string content = File.ReadAllText(filePath);
            return content.Contains("\"skeleton\"") && content.Contains("\"bones\"") && content.Contains("\"animations\"");
        }
    }
}
