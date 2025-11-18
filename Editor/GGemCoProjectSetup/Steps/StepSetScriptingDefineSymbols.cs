#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace GGemCo2DCoreEditor
{
    public class StepSetScriptingDefineSymbols : SetupStepBase
    {
        public string[] addSymbols;     // 예: GGEMCO_USE_NEW_INPUT, GGEMCO_SIMULATION
        public string[] removeSymbols;  // 제거할 심볼

        public override void Execute(EditorSetupContext ctx)
        {
            // 주요 타깃 그룹만 처리 (원하시면 확장)
            var targetGroups = new[] {
                NamedBuildTarget.Standalone,
                // NamedBuildTarget.Android,
                // NamedBuildTarget.iOS
            };

            foreach (var group in targetGroups)
            {
                var current = PlayerSettings.GetScriptingDefineSymbols(group);
                var list = current.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

                if (addSymbols != null)
                {
                    foreach (var s in addSymbols.Where(s => !string.IsNullOrWhiteSpace(s)))
                        if (!list.Contains(s)) list.Add(s);
                }

                if (removeSymbols != null)
                {
                    foreach (var s in removeSymbols.Where(s => !string.IsNullOrWhiteSpace(s)))
                        list.RemoveAll(x => x == s);
                }

                var joined = string.Join(";", list);
                PlayerSettings.SetScriptingDefineSymbols(group, joined);
                ctx.Logger.Info($"[Defines:{group}] {joined}");
            }
        }
    }
}
#endif