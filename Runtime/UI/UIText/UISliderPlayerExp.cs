using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// EXP 데이터 변경 시 UI 업데이트
    /// </summary>
    public class UISliderPlayerExp : MonoBehaviour
    {
        private Slider sliderExp;
        private PlayerData playerData;

        private void Awake()
        {
            sliderExp = GetComponent<Slider>();
        }

        private void Start()
        {
            playerData = SceneGame.Instance.saveDataManager.Player;
            playerData.OnCurrentExpChanged()
                .Merge(playerData.OnCurrentNeedExpChanged()) // 두 개의 Observable을 합침
                .Subscribe(_ => UpdateSlider()) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        /// <summary>
        /// 경험치 UI를 업데이트하는 메서드
        /// </summary>
        private void UpdateSlider()
        {
            if (sliderExp == null)
            {
                GcLogger.LogError("Slider 컴포넌트가 없습니다.");
                return;
            }
            sliderExp.value = (float)playerData.CurrentExp / playerData.CurrentNeedExp();
        }
    }
}