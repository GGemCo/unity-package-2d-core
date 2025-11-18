#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 예제 RPG 설정으로 카메라 셋팅
    /// </summary>
    public class StepSetCamera : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                msg = "[StepSetCamera] MainCamera not found. Make sure a Camera with tag 'MainCamera' exists.";
                return false;
            }
            msg = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            // 1) MainCamera 획득
            Camera cam = Camera.main;
            if (cam == null)
            {
                HelperLog.Warn($"[StepSetCamera] MainCamera not found. Make sure a Camera with tag 'MainCamera' exists.", ctx);
                return;
            }

            // 2) Undo 기록
            Undo.RecordObject(cam, "Set RPG Camera Settings");

            // 3) 카메라 값 설정
            cam.orthographic = true;
            cam.orthographicSize = 350f;  // 예제 RPG 기준

            // 4) 변경 저장 처리
            EditorUtility.SetDirty(cam);
            EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);

            HelperLog.Info($"[StepSetCamera] MainCamera projection has been set to Orthographic (Size: 350).", ctx);
        }
    }
}
#endif