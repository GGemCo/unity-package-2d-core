using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public static class HelperLog
    {
        public static void Info(string msg, EditorSetupContext ctx = null)
        {
            if (ctx != null)
            {
                ctx.logger.Info(msg);    
            }
            else
            {
                Debug.Log(msg);
            }
        }
        public static void Warn(string msg, EditorSetupContext ctx = null)
        {
            if (ctx != null)
            {
                ctx.logger.Warn(msg);    
            }
            else
            {
                Debug.LogWarning(msg);
            }
        }
        public static void Error(string msg, EditorSetupContext ctx = null)
        {
            if (ctx != null)
            {
                ctx.logger.Error(msg);    
            }
            else
            {
                Debug.LogError(msg);
            }
        }
    }
}