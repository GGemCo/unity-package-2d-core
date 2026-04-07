using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 UI 아이콘에 겹쳐 표시하는 보조 데코레이터 데이터입니다.
    /// </summary>
    [Serializable]
    public readonly struct AffectUiDecoratorData
    {
        public readonly bool Visible;
        public readonly Sprite Sprite;
        public readonly Vector2 Size;
        public readonly AffectUiDecoratorAnchor Anchor;
        public readonly Vector2 Offset;

        public AffectUiDecoratorData(bool visible, Sprite sprite, Vector2 size, AffectUiDecoratorAnchor anchor, Vector2 offset)
        {
            Visible = visible;
            Sprite = sprite;
            Size = size;
            Anchor = anchor;
            Offset = offset;
        }

        public static AffectUiDecoratorData Hidden => new(false, null, Vector2.zero, AffectUiDecoratorAnchor.RightBottom, Vector2.zero);
    }

    /// <summary>
    /// 보조 데코레이터 아이콘의 기준 위치입니다.
    /// 현재는 버프/디버프 타입 표시에 사용하지만, 추후 다른 데코레이터에도 재사용할 수 있습니다.
    /// </summary>
    public enum AffectUiDecoratorAnchor
    {
        LeftBottom = 0,
        RightBottom = 1,
        LeftTop = 2,
        RightTop = 3,
    }
}
