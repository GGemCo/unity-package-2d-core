using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public static class Physics2DUtil
    {
        // 재사용 버퍼(할당 최소화)
        // 장면 규모에 맞게 적절히 조절하세요.
        private static Collider2D[] _overlapBuffer = new Collider2D[256];

        /// <summary>
        /// 지정한 콜라이더와 겹치는 '몬스터'를 CharacterBase로 수집.
        /// - LayerMask/Trigger/Transform 동기화까지 옵션화
        /// - 내부 버퍼 길이를 초과하면 자동으로 확장 시도
        /// </summary>
        public static List<CharacterBase> GetMonstersInArea(
            Collider2D areaCollider,
            LayerMask targetMask,
            bool includeTriggers = true,
            bool syncTransforms = false,
            string monsterTag = null, // null이면 태그 체크 생략
            Func<CharacterHitArea, bool> extraPredicate = null // 추가 필터(선택)
        )
        {
            var result = new List<CharacterBase>();
            if (areaCollider == null) return result;

            if (syncTransforms)
            {
                // Transform 변경 직후 물리 갱신 필요 시 사용
                Physics2D.SyncTransforms(); // :contentReference[oaicite:1]{index=1}
            }

            var filter = new ContactFilter2D
            {
                useTriggers = includeTriggers,
                useLayerMask = true
            };
            filter.SetLayerMask(targetMask); // 레이어 필터링 적용. :contentReference[oaicite:2]{index=2}

            // 1차 시도
            int written = Physics2D.OverlapCollider(areaCollider, filter, _overlapBuffer); // :contentReference[oaicite:3]{index=3}

            // 버퍼가 꽉 찼다면(더 많은 결과가 있을 수 있음) 버퍼 확장 후 한 번 더 시도
            if (written >= _overlapBuffer.Length)
            {
                // 2배씩 확장 (상황에 따라 상한선 도입 권장)
                int newSize = _overlapBuffer.Length * 2;
                _overlapBuffer = new Collider2D[newSize];
                written = Physics2D.OverlapCollider(areaCollider, filter, _overlapBuffer); // 재시도
            }

            for (int i = 0; i < written && i < _overlapBuffer.Length; i++)
            {
                var col = _overlapBuffer[i];
                if (!col) continue;

                // CharacterHitArea가 붙어있는지 체크(할당 없는 TryGetComponent 권장)
                if (!col.TryGetComponent<CharacterHitArea>(out var hitArea) || hitArea.target == null)
                    continue;

                // 태그 필터(원하시면 ConfigTags 사용으로 교체)
                if (!string.IsNullOrEmpty(monsterTag) && !col.CompareTag(monsterTag))
                    continue;

                // 추가 사용자 정의 필터
                if (extraPredicate != null && !extraPredicate(hitArea))
                    continue;

                result.Add(hitArea.target);
            }

            return result;
        }
    }
}
