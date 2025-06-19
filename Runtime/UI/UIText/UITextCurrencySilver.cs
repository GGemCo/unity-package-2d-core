using R3;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 실버 재화 데이터 변경 시 UI 업데이트
    /// </summary>
    public class UITextCurrencySilver : MonoBehaviour
    {
        private TextMeshProUGUI textSilver;
        private PlayerData playerData;

        private void Awake()
        {
            textSilver = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            playerData = SceneGame.Instance.saveDataManager.Player;
            playerData.OnCurrentSilverChanged()
                .Subscribe(UpdateText) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        private void UpdateText(long newLevel)
        {
            if (textSilver == null)
            {
                GcLogger.LogError("TextMeshProUGUI 컴포넌트가 없습니다.");
                return;
            }
            textSilver.text = $"실버 {newLevel}";
        }
    }
}