using R3;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 골드 재화 데이터 변경 시 UI 업데이트
    /// </summary>
    public class UITextCurrencyGold : MonoBehaviour
    {
        private TextMeshProUGUI _textGold;
        private PlayerData _playerData;

        private void Awake()
        {
            _textGold = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            _playerData = SceneGame.Instance.saveDataManager.Player;
            _playerData.OnCurrentGoldChanged()
                .Subscribe(UpdateText) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        private void UpdateText(long value)
        {
            if (_textGold == null)
            {
                GcLogger.LogError("TextMeshProUGUI 컴포넌트가 없습니다.");
                return;
            }
            _textGold.text = $"x{value}";
        }
    }
}