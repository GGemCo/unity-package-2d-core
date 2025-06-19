using UnityEngine;

namespace GGemCo.Scripts
{
    public static class DialogueConstants
    {
        public const string JsonFolderName = "Dialogue/";
        public const string JsonFolderPath = "/Resources/"+JsonFolderName;
        
        public static string GetJsonFolderPath()
        {
            return Application.dataPath+ JsonFolderPath;
        }

    }
}