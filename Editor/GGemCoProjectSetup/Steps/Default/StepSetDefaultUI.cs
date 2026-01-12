using GGemCo2DCore;
using GGemCo2DCoreEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    public class StepSetDefaultUI : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            msg = null;
            return true;
        }
        public override void Execute(EditorSetupContext ctx)
        {
            SetCanvasBlackImage(ctx);
            
        }
        /// <summary>
        /// 맵 로딩중 Interaction을 막는 GGemCo_Core_CanvasBlack 을 투명 처리 합니다.
        /// 카드 명령어 사용 후, UI 연출 중 Interaction을 막는 용도로 사용 합니다.
        /// </summary>
        /// <param name="ctx"></param>
        private void SetCanvasBlackImage(EditorSetupContext ctx)
        {
            var canvasBlack = CreateUIComponent.Find("CanvasBlack", ConfigPackageInfo.PackageType.Core);
            if (canvasBlack == null)
            {
                HelperLog.Warn($"[StepSetDefaultUI] CanvasBlack 오브젝트가 없습니다.", ctx);
                return;
            }

            var image = canvasBlack.transform.GetChild(0).gameObject;
            if (image == null)
            {
                HelperLog.Warn($"[StepSetDefaultUI] CanvasBlack 하위에 Image 오브젝트가 없습니다.", ctx);
                return;
            }
            var imageComponent = image.GetComponent<Image>();
            imageComponent.color = new Color(0, 0, 0, 0);
        }
    }
}