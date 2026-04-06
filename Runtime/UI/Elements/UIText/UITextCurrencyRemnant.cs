using R3;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    public class UITextCurrencyRemnant : MonoBehaviour
    {
        private TextMeshProUGUI _textRemnant;
        private PlayerData _playerData;

        private void Awake()
        {
            _textRemnant = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            GameEventManager.ItemCollectedEvent += OnItemCollected;
        }
        private void OnDisable()
        {
            GameEventManager.ItemCollectedEvent -= OnItemCollected;
        }

        private void OnItemCollected(ItemCollectedEventData e)
        {
            int itemUid = e.ItemUid;
            int count = e.Count;
            GcLogger.Log($"ItemCollectedEvent: {itemUid} / {count}");
        }

        private void Start()
        {
            _playerData = SceneGame.Instance.saveDataManager.Player;
            _playerData.OnCurrentGoldChanged()
                .Subscribe(UpdateText) // 값이 변경될 때마다 UI 업데이트
                .AddTo(this);
        }

        private void UpdateText(long newLevel)
        {
            if (_textRemnant == null)
            {
                GcLogger.LogError("TextMeshProUGUI 컴포넌트가 없습니다.");
                return;
            }
            _textRemnant.text = $"x{newLevel}";
        }
    }
}