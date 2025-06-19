using R3;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// Level 데이터 변경 시 UI 업데이트
    /// </summary>
    public class UITextPlayerLevel : MonoBehaviour
    {
        private TextMeshProUGUI levelText;
        private PlayerData playerData;

        private void Awake()
        {
            levelText = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            playerData = SceneGame.Instance.saveDataManager.Player;
            playerData.OnCurrentLevelChanged()
                .Subscribe(UpdateText) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        private void UpdateText(int newLevel)
        {
            if (levelText == null)
            {
                GcLogger.LogError("TextMeshProUGUI 컴포넌트가 없습니다.");
                return;
            }
            levelText.text = $"Lv. {newLevel}";
        }
    }
}