using R3;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// EXP 데이터 변경 시 UI 업데이트
    /// </summary>
    public class UITextPlayerExp : MonoBehaviour
    {
        private TextMeshProUGUI textExp;
        private PlayerData playerData;

        private void Awake()
        {
            textExp = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            playerData = SceneGame.Instance.saveDataManager.Player;
            playerData.OnCurrentExpChanged()
                .Merge(playerData.OnCurrentNeedExpChanged()) // 두 개의 Observable을 합침
                .Subscribe(_ => UpdateExpText()) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        /// <summary>
        /// 경험치 UI를 업데이트하는 메서드
        /// </summary>
        private void UpdateExpText()
        {
            if (textExp == null)
            {
                GcLogger.LogError("TextMeshProUGUI 컴포넌트가 없습니다.");
                return;
            }
            textExp.text = $"{playerData.CurrentExp}/{playerData.CurrentNeedExp()}";
        }
    }
}