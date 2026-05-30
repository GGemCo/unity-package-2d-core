#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Slice 좌우 반전 툴의 출력 및 처리 옵션을 보관합니다.
    /// </summary>
    [Serializable]
    internal sealed class SpriteSliceFlipSettings
    {
        /// <summary>
        /// 출력 폴더 기본 경로입니다.
        /// </summary>
        private const string DefaultOutputFolder = "Assets/FlippedSprites";

        /// <summary>
        /// 출력 파일명에 붙일 기본 접미사입니다.
        /// </summary>
        private const string DefaultSuffix = "_flip";

        /// <summary>
        /// 출력 폴더입니다.
        /// 프로젝트 내부 Assets 경로만 허용합니다.
        /// </summary>
        [Tooltip("좌우 반전된 PNG Atlas를 저장할 프로젝트 내부 Assets 폴더입니다.")]
        public string outputFolder = DefaultOutputFolder;

        /// <summary>
        /// 출력 파일명 접미사입니다.
        /// </summary>
        [Tooltip("원본 텍스처 이름 뒤에 붙일 접미사입니다.")]
        public string outputNameSuffix = DefaultSuffix;

        /// <summary>
        /// 생성되는 Sprite 이름에 접미사를 붙일지 여부입니다.
        /// </summary>
        [Tooltip("생성되는 Sub Sprite 이름 뒤에도 접미사를 붙입니다.")]
        public bool appendSuffixToSpriteNames = true;

        /// <summary>
        /// Sprite Pivot X 값을 좌우 반전할지 여부입니다.
        /// </summary>
        [Tooltip("Sub Sprite의 Pivot X 값을 좌우 반전합니다. 발 위치와 기준점을 유지하려면 활성화하는 것을 권장합니다.")]
        public bool mirrorPivot = true;

        /// <summary>
        /// Sprite Border의 Left/Right 값을 교체할지 여부입니다.
        /// </summary>
        [Tooltip("9-Slice Border의 Left/Right 값을 교체합니다.")]
        public bool mirrorBorder = true;

        /// <summary>
        /// 기존 파일을 덮어쓸지 여부입니다.
        /// </summary>
        [Tooltip("같은 이름의 PNG가 이미 있으면 덮어씁니다. 비활성화 시 Unity가 고유 경로를 생성합니다.")]
        public bool overwriteExisting = true;

        /// <summary>
        /// 투명한 Slice 영역도 그대로 처리할지 여부입니다.
        /// </summary>
        [Tooltip("완전히 투명한 Slice 영역도 좌우 반전 처리 대상에 포함합니다.")]
        public bool includeFullyTransparentSprites = true;

        /// <summary>
        /// TextureImporter의 Read/Write Enabled 값을 작업 후 원래 상태로 복구할지 여부입니다.
        /// </summary>
        [Tooltip("원본 텍스처의 Read/Write Enabled 값을 임시로 켠 경우, 작업 완료 후 원래 상태로 복구합니다.")]
        public bool restoreSourceReadable = true;

        /// <summary>
        /// 설정 값을 유효 범위로 보정합니다.
        /// </summary>
        public void Normalize()
        {
            outputFolder = NormalizeAssetFolder(outputFolder);
            outputNameSuffix = string.IsNullOrEmpty(outputNameSuffix) ? DefaultSuffix : outputNameSuffix.Trim();
        }

        /// <summary>
        /// 출력 PNG 파일명을 생성합니다.
        /// </summary>
        /// <param name="sourceName">원본 텍스처 이름입니다.</param>
        /// <returns>확장자를 제외한 안전한 출력 파일명입니다.</returns>
        public string BuildSafeOutputFileNameWithoutExtension(string sourceName)
        {
            var baseName = string.IsNullOrWhiteSpace(sourceName) ? "FlippedSprite" : sourceName.Trim();
            return ToSafeFileName(baseName + outputNameSuffix);
        }

        /// <summary>
        /// 생성될 Sub Sprite 이름을 반환합니다.
        /// </summary>
        /// <param name="sourceSpriteName">원본 Sub Sprite 이름입니다.</param>
        /// <returns>출력 Sub Sprite 이름입니다.</returns>
        public string BuildOutputSpriteName(string sourceSpriteName)
        {
            if (!appendSuffixToSpriteNames)
            {
                return sourceSpriteName;
            }

            return string.IsNullOrWhiteSpace(sourceSpriteName)
                ? "Sprite" + outputNameSuffix
                : sourceSpriteName + outputNameSuffix;
        }

        /// <summary>
        /// 파일명에 사용할 수 없는 문자를 치환합니다.
        /// </summary>
        /// <param name="value">치환할 문자열입니다.</param>
        /// <returns>파일명으로 안전한 문자열입니다.</returns>
        private static string ToSafeFileName(string value)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            return Regex.Replace(value, $"[{invalidChars}]", "_");
        }

        /// <summary>
        /// Unity 프로젝트 내부 Assets 폴더 경로로 보정합니다.
        /// </summary>
        /// <param name="folder">보정할 폴더 경로입니다.</param>
        /// <returns>정규화된 Assets 상대 경로입니다.</returns>
        private static string NormalizeAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return DefaultOutputFolder;
            }

            var normalized = folder.Replace('\\', '/').Trim().TrimEnd('/');
            return string.IsNullOrEmpty(normalized) ? DefaultOutputFolder : normalized;
        }
    }
}
#endif
