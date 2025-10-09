using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    [Serializable]
    public struct TileTransition
    {
        [Header("전이 조건")]
        public TileState from;
        public TileAction action;

        [Header("결과")]
        public TileState to;

        [Tooltip("전이 시 교체할 타일 (RuleTile/Tile 가능). 비우면 외형 유지")]
        public TileBase nextTile;

        [Tooltip("해당 상태 유지 시간(게임 초). 0 이하이면 타이머 미사용")]
        public float durationGameSeconds;

        [Header("FX (선택)")]
        public string sfxKey;
        public string vfxKey;
    }

    /// <summary>
    /// (state, action) → 전이 정의 DB
    /// - 디자이너 친화적: 코드 수정 없이 데이터만 추가/수정
    /// - 런타임에서는 Dictionary로 빠른 룩업
    /// </summary>
    [CreateAssetMenu(fileName = "TileStateDB", menuName = "GGemCo/Tile/TileStateDB", order = 10)]
    public class TileStateDB : ScriptableObject
    {
        [SerializeField] private TileTransition[] transitions;

        // from|action → index
        private Dictionary<(TileState, TileAction), int> _map;

        private void OnEnable()
        {
            _map = new Dictionary<(TileState, TileAction), int>(transitions?.Length ?? 0);
            if (transitions == null) return;

            for (int i = 0; i < transitions.Length; i++)
            {
                var t = transitions[i];
                _map[(t.from, t.action)] = i;
            }
        }

        public bool TryGet(TileState from, TileAction action, out TileTransition t)
        {
            if (_map != null && _map.TryGetValue((from, action), out var idx))
            {
                t = transitions[idx];
                return true;
            }

            t = default;
            return false;
        }

        /// <summary>전이 원본 배열(디버그/툴용)</summary>
        public IReadOnlyList<TileTransition> RawTransitions => transitions;
    }
}