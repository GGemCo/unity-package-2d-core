using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 저장, 불러오기
    /// </summary>
    public class FileHandler
    {
        private readonly DialogueEditorWindow editorWindow;

        public FileHandler(DialogueEditorWindow window)
        {
            editorWindow = window;
        }

        public void SaveToJson(int selectedQuestIndex, Dictionary<int, StruckTableDialogue> dialogueInfos)
        {
            DialogueData data = new DialogueData();

            foreach (var node in editorWindow.nodes)
            {
                DialogueNodeData nodeData = new DialogueNodeData
                {
                    guid = node.guid,
                    dialogueText = node.dialogueText,
                    position = new Vec2(node.position),
                    characterType = node.characterType,
                    characterUid = node.characterUid,
                    fontSize = node.fontSize,
                    thumbnailImage = node.thumbnailImage,
                    nextNodeGuid = node.nextNodeGuid,
                    startQuestUid = node.startQuestUid,
                    startQuestStep = node.startQuestStep,
                    options = node.options
                };
                data.nodes.Add(nodeData);
            }

            bool result = EditorUtility.DisplayDialog("저장하기", "현재 선택된 대화에 저장하시겠습니까?", "네", "아니요");
            if (!result) return;
            var info = dialogueInfos.GetValueOrDefault(selectedQuestIndex);
            if (info == null) return;
            string fileName = info.FileName;
            string path = Path.Combine(DialogueConstants.GetJsonFolderPath(), fileName+".json");
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("대사 생성툴", "Json 저장하기 완료", "OK");
        }

        public void LoadFromJson(string fileName)
        {
            string jsonFilePath = $"Dialogue/{fileName}";
            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{jsonFilePath}");
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (string.IsNullOrEmpty(content)) return;
                    DialogueData data = JsonConvert.DeserializeObject<DialogueData>(content);

                    editorWindow.nodes.Clear();

                    foreach (var nodeData in data.nodes)
                    {
                        DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
                        node.guid = nodeData.guid;
                        node.dialogueText = nodeData.dialogueText;
                        node.characterType = nodeData.characterType;
                        node.characterUid = nodeData.characterUid;
                        node.fontSize = nodeData.fontSize;
                        node.thumbnailImage = nodeData.thumbnailImage;
                        node.position = nodeData.position.ToVector2();
                        node.options = nodeData.options;
                        node.nextNodeGuid = nodeData.nextNodeGuid;
                        node.startQuestUid = nodeData.startQuestUid;
                        node.startQuestStep = nodeData.startQuestStep;
                        editorWindow.nodes.Add(node);
                    }
                }
                else
                {
                    GcLogger.LogError("파일이 없습니다. path: " + jsonFilePath);
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"json 파일을 읽어오는데 오류가 발생하였습니다. path: {jsonFilePath}, error message: {ex.Message}");
            }
        }
    }
}