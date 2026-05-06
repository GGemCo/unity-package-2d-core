using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트가 표시 상태를 제어할 수 있는 월드 오브젝트 마커입니다.
    /// </summary>
    /// <remarks>
    /// 사망 연출의 전경 오브젝트처럼 특정 컷신 동안만 숨기거나 다시 보이게 할 대상을
    /// 그룹 키로 식별합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CutsceneVisibilityTarget : MonoBehaviour
    {
        [Header("Cutscene Visibility")]
        [Tooltip("컷신 표시 제어 이벤트에서 대상을 찾을 때 사용하는 그룹 키 목록입니다.")]
        [SerializeField] private List<string> groupKeys = new List<string> { "Default" };

        [Tooltip("자식 Renderer까지 함께 표시 상태를 제어할지 여부입니다.")]
        [SerializeField] private bool includeChildRenderers = true;

        /// <summary>
        /// 이 마커에 등록된 그룹 키 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<string> GroupKeys => groupKeys;

        /// <summary>
        /// 자식 Renderer까지 제어 대상으로 포함할지 여부를 반환합니다.
        /// </summary>
        public bool IncludeChildRenderers => includeChildRenderers;

        /// <summary>
        /// 지정한 그룹 키 중 하나라도 현재 대상에 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="keys">비교할 그룹 키 목록입니다.</param>
        /// <returns>하나 이상의 그룹 키가 일치하면 <see langword="true"/>를 반환합니다.</returns>
        public bool BelongsToAny(IReadOnlyList<string> keys)
        {
            if (keys == null || keys.Count <= 0)
            {
                return false;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (BelongsTo(keys[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 그룹 키가 현재 대상의 그룹 키와 일치하는지 확인합니다.
        /// </summary>
        /// <param name="key">비교할 그룹 키입니다.</param>
        /// <returns>그룹 키가 일치하면 <see langword="true"/>를 반환합니다.</returns>
        public bool BelongsTo(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || groupKeys == null)
            {
                return false;
            }

            string normalizedKey = key.Trim();
            for (int i = 0; i < groupKeys.Count; i++)
            {
                string groupKey = groupKeys[i];
                if (string.IsNullOrWhiteSpace(groupKey))
                {
                    continue;
                }

                if (string.Equals(groupKey.Trim(), normalizedKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 대상에서 표시 상태를 변경할 Renderer 목록을 수집합니다.
        /// </summary>
        /// <param name="includeInactive">비활성 자식에 포함된 Renderer도 수집할지 여부입니다.</param>
        /// <returns>표시 상태 제어 대상 Renderer 배열입니다.</returns>
        public Renderer[] GetTargetRenderers(bool includeInactive)
        {
            return includeChildRenderers
                ? GetComponentsInChildren<Renderer>(includeInactive)
                : GetComponents<Renderer>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터에서 그룹 키 목록이 비어 있는 상태로 저장되지 않도록 기본 값을 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            groupKeys ??= new List<string>();
            if (groupKeys.Count <= 0)
            {
                groupKeys.Add("Default");
            }
        }
#endif
    }
}
