#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// WindowTable을 읽어, 지정 Canvas 하위에 UIWindow 프리팹을 일괄 인스턴스합니다.
    /// - 이미 동일 이름의 오브젝트가 있으면 재사용 또는 덮어쓰기(옵션)
    /// - 프리팹 인스턴스 시 '완전 언팩'(옵션) 혹은 프리팹 링크 유지 선택
    /// - 마지막에 UIWindowManager(SetUIWindow)에 등록(옵션)
    /// - 성능: 에셋 검색은 사전 인덱싱(경로 필터), 반복 할당 최소화
    /// </summary>
    public sealed class StepInstantiateUIWindowsFromTable : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var sceneEditorGame = ScriptableObject.CreateInstance<SceneEditorGame>();
            sceneEditorGame.SetupAllTestWindow(ctx);
            
            // Npc 네임 태그 폰트 크기 조절
            PrefabPropertyEditorUtil.SetPrefabPropertyValue<TagNameNpc, int>(
                prefabName: "TextNpcNameTag",
                propertyName: "fontSize",
                value: 10,
                false
            );
            
            // 드랍 아이템 네임 태그 폰트 크기 조절
            PrefabPropertyEditorUtil.SetPrefabPropertyValue<TagNameItem, int>(
                prefabName: "TextDropItemNameTag",
                propertyName: "fontSize",
                value: 10,
                false
            );
        }
    }
}
#endif