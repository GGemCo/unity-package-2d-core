using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 컨텐츠(스킬/아이템) 아이콘 제공자 레지스트리.
    /// - Core 는 Skill 패키지를 직접 참조하지 않으며, Skill 패키지는 Provider 로 등록한다.
    /// </summary>
    public static class QuickSlotContentProviderRegistry
    {
        private static readonly List<IQuickSlotContentProvider> _providers = new List<IQuickSlotContentProvider>();

        public static void Register(IQuickSlotContentProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            // 동일 타입 중복 등록 방지(핫리로드/도메인 리로드 등)
            for (int i = _providers.Count - 1; i >= 0; i--)
            {
                if (_providers[i] != null && _providers[i].GetType() == provider.GetType())
                {
                    _providers.RemoveAt(i);
                    break;
                }
            }

            _providers.Add(provider);

            // 우선순위 높은 순으로 정렬
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public static Sprite TryGetIconSprite(QuickSlotContentKind kind, int uid, int level, long instanceId)
        {
            for (int i = 0; i < _providers.Count; i++)
            {
                var p = _providers[i];
                if (p == null) continue;
                if (!p.CanProvide(kind)) continue;

                var sprite = p.GetIconSprite(kind, uid, level, instanceId);
                if (sprite != null)
                    return sprite;
            }

            return null;
        }
    }
}
