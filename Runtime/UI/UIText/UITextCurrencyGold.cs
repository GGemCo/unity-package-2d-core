using R3;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 골드 재화 데이터 변경 시 UI 업데이트
    /// </summary>
    public class UITextCurrencyGold : MonoBehaviour
    {
        private TextMeshProUGUI textGold;
        private PlayerData playerData;

        private void Awake()
        {
            textGold = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            playerData = SceneGame.Instance.saveDataManager.Player;
            playerData.OnCurrentGoldChanged()
                .Subscribe(UpdateText) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        private void UpdateText(long newLevel)
        {
            if (textGold == null)
            {
                GcLogger.LogError("TextMeshProUGUI 컴포넌트가 없습니다.");
                return;
            }
            textGold.text = $"골드 {newLevel}";
        }
    }
}