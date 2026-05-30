#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 원본 Texture2D에 포함된 Sub Sprite의 Slice 메타데이터입니다.
    /// </summary>
    internal readonly struct SpriteSliceInfo
    {
        /// <summary>
        /// 원본 Sprite 이름입니다.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// 원본 Texture 내부 Sprite 영역입니다.
        /// </summary>
        public readonly Rect Rect;

        /// <summary>
        /// 원본 Sprite Pivot 값입니다.
        /// </summary>
        public readonly Vector2 Pivot;

        /// <summary>
        /// 원본 Sprite Border 값입니다.
        /// </summary>
        public readonly Vector4 Border;

        /// <summary>
        /// 원본 Sprite 정렬 방식입니다.
        /// </summary>
        public readonly SpriteAlignment Alignment;

        /// <summary>
        /// Slice 메타데이터를 초기화합니다.
        /// </summary>
        /// <param name="name">원본 Sprite 이름입니다.</param>
        /// <param name="rect">원본 Texture 내부 Sprite 영역입니다.</param>
        /// <param name="pivot">원본 Sprite Pivot 값입니다.</param>
        /// <param name="border">원본 Sprite Border 값입니다.</param>
        /// <param name="alignment">원본 Sprite 정렬 방식입니다.</param>
        public SpriteSliceInfo(string name, Rect rect, Vector2 pivot, Vector4 border, SpriteAlignment alignment)
        {
            Name = name;
            Rect = rect;
            Pivot = pivot;
            Border = border;
            Alignment = alignment;
        }

        /// <summary>
        /// Rect가 실제 픽셀 처리 가능한 크기인지 확인합니다.
        /// </summary>
        public bool IsValid => Rect.width >= 1f && Rect.height >= 1f;
    }
}
#endif
