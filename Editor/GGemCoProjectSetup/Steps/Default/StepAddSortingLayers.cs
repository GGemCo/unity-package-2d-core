#if UNITY_EDITOR
using System.Linq;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// ConfigSortingLayer.GetValues()를 기준으로 Sorting Layer를 추가한다.
    /// - 이미 존재하는 이름은 스킵
    /// - uniqueID는 현재 최댓값+1부터 순차 배정
    /// - 결과/진행은 EditorSetupLogger로 출력
    /// </summary>
    public sealed class StepAddSortingLayers : SetupStepBase
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            // TagManager 존재 여부
            var objs = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (objs == null || objs.Length == 0)
            {
                message = $"TagManager.asset을 찾을 수 없습니다: {TagManagerPath}";
                return false;
            }

            // 구성 데이터 존재 여부 (ConfigSortingLayer)
            // 기존 구현은 ConfigSortingLayer.GetValues() 순회 사용
            // (ref: SettingSortingLayers.cs) 
            var hasAny = ConfigSortingLayer.GetValues().Any(); // 빈 목록이면 경고 수준
            if (!hasAny)
            {
                message = "ConfigSortingLayer.GetValues()가 비어 있습니다.";
                // 빈 목록 자체는 실행 불필요하므로 성공으로 간주해도 되지만,
                // Runner 정책에 맞춰 실패로 처리하려면 false로 반환하세요.
                return true;
            }

            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var settingSortingLayers = new SettingSortingLayers();
            settingSortingLayers.AddSortingLayers(ctx);
        }
    }
}
#endif
