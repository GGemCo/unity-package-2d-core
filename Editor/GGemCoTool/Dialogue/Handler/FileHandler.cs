using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 대사 그래프의 편집 원본 저장과 런타임 export 저장을 담당합니다.
    /// </summary>
    public class FileHandler
    {
        private readonly DialogueEditorWindowWindow _editorWindowWindow;
        private readonly DialogueLocalizationExportService _localizationExportService = new DialogueLocalizationExportService();

        /// <summary>
        /// FileHandler 를 초기화합니다.
        /// </summary>
        /// <param name="windowWindow">대사 에디터 윈도우입니다.</param>
        public FileHandler(DialogueEditorWindowWindow windowWindow)
        {
            _editorWindowWindow = windowWindow;
        }

        /// <summary>
        /// 현재 편집 중인 대사 그래프를 Authoring ScriptableObject 와 런타임 json 으로 저장합니다.
        /// 저장 과정에서 Localization String Table Collection 도 함께 갱신합니다.
        /// </summary>
        /// <param name="selectedDialogueIndex">현재 선택된 dialogue 인덱스입니다.</param>
        /// <param name="dialogueInfos">toolbar 에서 사용하는 dialogue 메타 정보입니다.</param>
        public void SaveToJson(int selectedDialogueIndex, Dictionary<int, StruckTableDialogue> dialogueInfos)
        {
            bool result = EditorUtility.DisplayDialog("저장하기", "현재 선택된 대화를 저장하시겠습니까?\nAuthoring Asset, Localization, Runtime Json 이 함께 갱신됩니다.", "네", "아니요");
            if (!result)
            {
                return;
            }

            StruckTableDialogue info = dialogueInfos.GetValueOrDefault(selectedDialogueIndex);
            if (info == null)
            {
                EditorUtility.DisplayDialog("대사 생성툴", "dialogue 정보를 찾지 못했습니다.", "OK");
                return;
            }

            try
            {
                SaveAuthoringAsset(info);
                string collectionName = _localizationExportService.Export(info, _editorWindowWindow.nodes);
                SaveRuntimeJson(info, collectionName);
                EditorUtility.DisplayDialog("대사 생성툴", "저장하기 완료", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("대사 생성툴", $"저장 중 오류가 발생했습니다.\n{ex.Message}", "OK");
            }
        }

        /// <summary>
        /// 현재 선택한 대화 그래프를 Authoring Asset 우선으로 불러옵니다.
        /// Authoring Asset 이 없으면 기존 런타임 json 을 읽어오는 레거시 경로로 fallback 합니다.
        /// </summary>
        /// <param name="dialogueInfo">불러올 dialogue 메타 정보입니다.</param>
        public void Load(StruckTableDialogue dialogueInfo)
        {
            if (dialogueInfo == null)
            {
                return;
            }

            if (LoadFromAuthoringAsset(dialogueInfo))
            {
                return;
            }

            LoadFromLegacyJson(dialogueInfo.FileName);
        }

        /// <summary>
        /// Authoring ScriptableObject 에셋을 저장합니다.
        /// 각 노드는 서브 에셋으로 복제 저장하여, 툴 원본 데이터와 런타임 export 를 분리합니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        private void SaveAuthoringAsset(StruckTableDialogue dialogueInfo)
        {
            string assetPath = GetAuthoringAssetPath(dialogueInfo);
            EnsureParentDirectory(assetPath);

            DialogueGraphAsset graphAsset = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(assetPath);
            if (graphAsset == null)
            {
                graphAsset = ScriptableObject.CreateInstance<DialogueGraphAsset>();
                graphAsset.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(graphAsset, assetPath);
            }

            graphAsset.DialogueUid = dialogueInfo.Uid;
            graphAsset.DialogueFileName = dialogueInfo.FileName;

            ClearNodeSubAssets(assetPath, graphAsset);
            graphAsset.Nodes.Clear();

            for (int index = 0; index < _editorWindowWindow.nodes.Count; index++)
            {
                DialogueNode sourceNode = _editorWindowWindow.nodes[index];
                if (sourceNode == null)
                {
                    continue;
                }

                DialogueNode clonedNode = Object.Instantiate(sourceNode);
                clonedNode.name = BuildNodeAssetName(index, sourceNode.guid);
                AssetDatabase.AddObjectToAsset(clonedNode, graphAsset);
                graphAsset.Nodes.Add(clonedNode);
            }

            EditorUtility.SetDirty(graphAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// 현재 노드 목록을 런타임 전용 json 으로 내보냅니다.
        /// 본문과 선택지는 원문 대신 Localization table/key 참조를 기록합니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        /// <param name="collectionName">대사에 연결된 Localization 컬렉션 이름입니다.</param>
        private void SaveRuntimeJson(StruckTableDialogue dialogueInfo, string collectionName)
        {
            DialogueData data = BuildRuntimeExportData(dialogueInfo, collectionName);
            string jsonPath = GetRuntimeJsonPath(dialogueInfo);
            EnsureParentDirectory(jsonPath);

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 현재 에디터 노드 목록을 기반으로 런타임 export 데이터를 구성합니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        /// <param name="collectionName">대사 로컬라이제이션 컬렉션 이름입니다.</param>
        /// <returns>런타임 export 데이터입니다.</returns>
        private DialogueData BuildRuntimeExportData(StruckTableDialogue dialogueInfo, string collectionName)
        {
            DialogueData data = new DialogueData();

            foreach (DialogueNode node in _editorWindowWindow.nodes)
            {
                if (node == null)
                {
                    continue;
                }

                DialogueNodeData nodeData = new DialogueNodeData
                {
                    guid = node.guid,
                    dialogueText = string.Empty,
                    dialogueTable = collectionName,
                    dialogueKey = DialogueLocalizationExportService.BuildNodeTextKey(dialogueInfo.Uid, node.guid),
                    position = new Vec2(node.position),
                    characterType = node.characterType,
                    characterUid = node.characterUid,
                    fontSize = node.fontSize,
                    thumbnailImage = node.thumbnailImage,
                    thumbnailPositionType = node.thumbnailPositionType,
                    thumbnailFlipPolicy = node.thumbnailFlipPolicy,
                    thumbnailSourceFacing = node.thumbnailSourceFacing,
                    nextNodeGuid = node.nextNodeGuid,
                    startQuestUid = node.startQuestUid,
                    startQuestStep = node.startQuestStep,
                    options = BuildRuntimeOptions(dialogueInfo, collectionName, node),
                };
                data.nodes.Add(nodeData);
            }

            return data;
        }

        /// <summary>
        /// 런타임 json 에 기록할 선택지 목록을 생성합니다.
        /// 각 선택지는 Localization table/key 를 사용하고, editor 원문 문자열은 포함하지 않습니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        /// <param name="collectionName">대사 로컬라이제이션 컬렉션 이름입니다.</param>
        /// <param name="node">현재 노드입니다.</param>
        /// <returns>런타임 선택지 목록입니다.</returns>
        private static List<DialogueOption> BuildRuntimeOptions(StruckTableDialogue dialogueInfo, string collectionName, DialogueNode node)
        {
            List<DialogueOption> result = new List<DialogueOption>();
            if (node?.options == null)
            {
                return result;
            }

            for (int optionIndex = 0; optionIndex < node.options.Count; optionIndex++)
            {
                DialogueOption option = node.options[optionIndex];
                if (option == null)
                {
                    continue;
                }

                result.Add(new DialogueOption
                {
                    optionText = string.Empty,
                    optionTable = collectionName,
                    optionKey = DialogueLocalizationExportService.BuildOptionTextKey(dialogueInfo.Uid, node.guid, optionIndex),
                    nextNodeGuid = option.nextNodeGuid,
                });
            }

            return result;
        }

        /// <summary>
        /// Authoring Asset 에서 편집 원본을 불러옵니다.
        /// </summary>
        /// <param name="dialogueInfo">불러올 dialogue 메타 정보입니다.</param>
        /// <returns>Authoring Asset 을 읽어왔으면 true 입니다.</returns>
        private bool LoadFromAuthoringAsset(StruckTableDialogue dialogueInfo)
        {
            string assetPath = GetAuthoringAssetPath(dialogueInfo);
            DialogueGraphAsset graphAsset = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(assetPath);
            if (graphAsset == null)
            {
                return false;
            }

            _editorWindowWindow.nodes.Clear();
            if (graphAsset.Nodes == null)
            {
                return true;
            }

            foreach (DialogueNode assetNode in graphAsset.Nodes)
            {
                if (assetNode == null)
                {
                    continue;
                }

                DialogueNode nodeInstance = Object.Instantiate(assetNode);
                _editorWindowWindow.nodes.Add(nodeInstance);
            }

            return true;
        }

        /// <summary>
        /// 기존 json 포맷을 그대로 읽어 편집기 노드로 복원합니다.
        /// Authoring Asset 이 도입되기 전 데이터를 계속 열 수 있도록 유지하는 레거시 경로입니다.
        /// </summary>
        /// <param name="fileName">dialogue.txt 의 FileName 입니다.</param>
        private void LoadFromLegacyJson(string fileName)
        {
            string jsonFilePath = $"{ConfigAddressablePath.Narrative.Dialogue}/{fileName}.json";
            try
            {
                string content = AssetDatabaseLoaderManager.LoadFileJson(jsonFilePath);
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                DialogueData data = JsonConvert.DeserializeObject<DialogueData>(content);
                _editorWindowWindow.nodes.Clear();

                foreach (DialogueNodeData nodeData in data.nodes)
                {
                    DialogueNode node = ScriptableObject.CreateInstance<DialogueNode>();
                    node.guid = nodeData.guid;
                    node.dialogueText = nodeData.dialogueText;
                    node.characterType = nodeData.characterType;
                    node.characterUid = nodeData.characterUid;
                    node.fontSize = nodeData.fontSize;
                    node.thumbnailImage = nodeData.thumbnailImage;
                    node.thumbnailPositionType = nodeData.thumbnailPositionType;
                    node.thumbnailFlipPolicy = nodeData.thumbnailFlipPolicy;
                    node.thumbnailSourceFacing = nodeData.thumbnailSourceFacing;
                    node.position = nodeData.position.ToVector2();
                    node.options = nodeData.options ?? new List<DialogueOption>();
                    node.nextNodeGuid = nodeData.nextNodeGuid;
                    node.startQuestUid = nodeData.startQuestUid;
                    node.startQuestStep = nodeData.startQuestStep;
                    _editorWindowWindow.nodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"json 파일을 읽어오는데 오류가 발생하였습니다. path: {jsonFilePath}, error message: {ex.Message}");
            }
        }

        /// <summary>
        /// Authoring Asset 경로를 계산합니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        /// <returns>Authoring Asset 경로입니다.</returns>
        private static string GetAuthoringAssetPath(StruckTableDialogue dialogueInfo)
        {
            return $"Assets/Editor/Dialogue/DialogueGraph_{dialogueInfo.Uid}.asset";
        }

        /// <summary>
        /// 런타임 json 경로를 계산합니다.
        /// </summary>
        /// <param name="dialogueInfo">대사 메타 정보입니다.</param>
        /// <returns>json 경로입니다.</returns>
        private static string GetRuntimeJsonPath(StruckTableDialogue dialogueInfo)
        {
            return $"{ConfigAddressablePath.Narrative.Dialogue}/{dialogueInfo.FileName}.json";
        }

        /// <summary>
        /// 지정한 에셋 경로 아래의 DialogueNode 서브 에셋을 모두 제거합니다.
        /// </summary>
        /// <param name="assetPath">Authoring Asset 경로입니다.</param>
        /// <param name="graphAsset">대상 그래프 에셋입니다.</param>
        private static void ClearNodeSubAssets(string assetPath, DialogueGraphAsset graphAsset)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object subAsset in subAssets)
            {
                if (subAsset is DialogueNode)
                {
                    Object.DestroyImmediate(subAsset, true);
                }
            }

            if (graphAsset?.Nodes != null)
            {
                graphAsset.Nodes.RemoveAll(node => node == null);
            }
        }

        /// <summary>
        /// 에셋 파일명으로 사용할 노드 이름을 생성합니다.
        /// </summary>
        /// <param name="index">노드 인덱스입니다.</param>
        /// <param name="guid">노드 GUID 입니다.</param>
        /// <returns>서브 에셋 이름입니다.</returns>
        private static string BuildNodeAssetName(int index, string guid)
        {
            string safeGuid = string.IsNullOrWhiteSpace(guid)
                ? "node"
                : new string(guid.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());

            if (string.IsNullOrWhiteSpace(safeGuid))
            {
                safeGuid = "node";
            }

            return $"DialogueNode_{index}_{safeGuid}";
        }

        /// <summary>
        /// 파일 저장 전 상위 폴더 존재를 보장합니다.
        /// </summary>
        /// <param name="filePath">대상 파일 경로입니다.</param>
        private static void EnsureParentDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory) || Directory.Exists(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
    }
}
