using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 버프(어펙트) 표시용 윈도우(View).
    /// - 적용/만료/스택 규칙은 AffectComponent가 관리한다.
    /// - 본 클래스는 Presenter로부터 스냅샷을 받아 렌더링만 수행한다.
    /// </summary>
    public class UIWindowPlayerBuffInfo : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("버프 슬롯 프리팹")]
        public GameObject prefabSlotBuff;

        private readonly Dictionary<int, GameObject> _activeSlotsByAffectUid = new();
        private readonly Stack<GameObject> _slotPool = new();

        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.PlayerBuffInfo;
            base.Awake();

            _activeSlotsByAffectUid.Clear();
            _slotPool.Clear();
        }

        /// <summary>
        /// Presenter가 전달하는 스냅샷을 기반으로 UI를 갱신한다.
        /// </summary>
        public void Render(IReadOnlyList<AffectUiItem> items)
        {
            if (prefabSlotBuff == null || containerIcon == null)
                return;

            // 1) 필요없는 슬롯 회수
            s_tmpKeysToRemove.Clear();
            foreach (var kv in _activeSlotsByAffectUid)
            {
                int affectUid = kv.Key;
                bool exists = false;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].AffectUid == affectUid)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    s_tmpKeysToRemove.Add(affectUid);
            }

            for (int i = 0; i < s_tmpKeysToRemove.Count; i++)
            {
                int affectUid = s_tmpKeysToRemove[i];
                if (_activeSlotsByAffectUid.TryGetValue(affectUid, out var slot) && slot != null)
                    RecycleSlot(slot);
                _activeSlotsByAffectUid.Remove(affectUid);
            }

            // 2) 스냅샷 렌더
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.AffectUid <= 0)
                    continue;

                if (!_activeSlotsByAffectUid.TryGetValue(item.AffectUid, out var slot) || slot == null)
                {
                    slot = GetOrCreateSlot();
                    _activeSlotsByAffectUid[item.AffectUid] = slot;
                }

                var icon = slot.GetComponentInChildren<UIIconBuff>();
                if (icon != null)
                    icon.Bind(item);
            }
        }

        private static readonly List<int> s_tmpKeysToRemove = new(64);

        private GameObject GetOrCreateSlot()
        {
            GameObject slot;
            if (_slotPool.Count > 0)
            {
                slot = _slotPool.Pop();
                if (slot != null)
                {
                    slot.transform.SetParent(containerIcon.transform, false);
                    slot.SetActive(true);
                    return slot;
                }
            }

            slot = Instantiate(prefabSlotBuff, containerIcon.transform);
            return slot;
        }

        private void RecycleSlot(GameObject slot)
        {
            if (slot == null) return;

            // 쿨타임 핸들러 정리
            var icon = slot.GetComponentInChildren<UIIconBuff>();
            if (icon != null)
            {
                icon.ClearCoolTime();
                // 풀링 재사용 시, 이전 바인딩 캐시가 남아있으면
                // 새로운 어펙트가 동일한 슬롯에 들어왔을 때 정적 UI가 갱신되지 않을 수 있다.
                icon.ResetBindingCache();
            }

            slot.SetActive(false);
            slot.transform.SetParent(transform, false); // hierarchy 정리(원하면 별도 PoolRoot로 이동)
            _slotPool.Push(slot);
        }
    }
}
