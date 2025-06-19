using GGemCo.Scripts;
using Newtonsoft.Json;
using UnityEngine;

public static class DialogueLoader
{
    /// <summary>
    /// 대사 json 불러오기
    /// </summary>
    /// <param name="dialogueUid"></param>
    /// <returns></returns>
    public static DialogueData LoadDialogueData(int dialogueUid)
    {
        var info = TableLoaderManager.Instance.TableDialogue.GetDataByUid(dialogueUid);
        if (info == null) return null;

        string jsonFilePath = $"Dialogue/{info.FileName}";
        TextAsset textFile = Resources.Load<TextAsset>(jsonFilePath);

        if (textFile == null)
        {
            GcLogger.LogError($"대사 파일을 찾지 못 했습니다.: {jsonFilePath}");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<DialogueData>(textFile.text);
        }
        catch (System.Exception ex)
        {
            GcLogger.LogError($"대사 json 파일을 불러오는중 오류가 발생했습니다.: {jsonFilePath}, {ex.Message}");
            return null;
        }
    }
}