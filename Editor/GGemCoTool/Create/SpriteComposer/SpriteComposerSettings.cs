using System;
using System.IO;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Composer 에디터 툴에서 사용하는 출력 및 렌더링 옵션을 보관합니다.
    /// </summary>
    [Serializable]
    internal sealed class SpriteComposerSettings
    {
        /// <summary>
        /// 생성된 PNG/Sprite 에셋을 저장할 프로젝트 내부 폴더 경로입니다.
        /// </summary>
        public string OutputFolder = "Assets/GGemCoGenerated/Sprites";

        /// <summary>
        /// 저장될 파일 이름입니다. 확장자는 자동으로 png가 사용됩니다.
        /// </summary>
        public string FileName = "ComposedSprite";

        /// <summary>
        /// 월드 1유닛에 대응되는 픽셀 수입니다.
        /// </summary>
        public float PixelsPerUnit = 100f;

        /// <summary>
        /// 합성 결과 가장자리 여백 픽셀 수입니다.
        /// </summary>
        public int Padding = 8;

        /// <summary>
        /// 생성할 텍스처의 최대 한 변 크기입니다.
        /// </summary>
        public int MaxTextureSize = 4096;

        /// <summary>
        /// RenderTexture에 적용할 안티앨리어싱 샘플 수입니다.
        /// </summary>
        public int AntiAliasing = 1;

        /// <summary>
        /// 비활성 GameObject 또는 비활성 SpriteRenderer도 합성 대상에 포함할지 여부입니다.
        /// </summary>
        public bool IncludeInactive;

        /// <summary>
        /// 같은 이름의 파일이 있을 때 덮어쓸지 여부입니다.
        /// </summary>
        public bool OverwriteExisting;

        /// <summary>
        /// 생성된 텍스처와 임포트된 Sprite에 적용할 필터 모드입니다.
        /// </summary>
        public FilterMode FilterMode = FilterMode.Bilinear;

        /// <summary>
        /// 설정값이 유효 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(OutputFolder))
            {
                OutputFolder = "Assets";
            }

            OutputFolder = OutputFolder.Replace('\\', '/').TrimEnd('/');

            if (!OutputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                OutputFolder = "Assets";
            }

            if (string.IsNullOrWhiteSpace(FileName))
            {
                FileName = "ComposedSprite";
            }

            PixelsPerUnit = Mathf.Max(1f, PixelsPerUnit);
            Padding = Mathf.Clamp(Padding, 0, 4096);
            MaxTextureSize = Mathf.Clamp(MaxTextureSize, 16, 8192);
            AntiAliasing = NormalizeAntiAliasing(AntiAliasing);
        }

        /// <summary>
        /// 파일 시스템에서 사용할 수 없는 문자를 제거한 안전한 파일 이름을 반환합니다.
        /// </summary>
        /// <returns>확장자를 제외한 정규화된 파일 이름입니다.</returns>
        public string GetSafeFileNameWithoutExtension()
        {
            Normalize();

            var safeName = FileName.Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar.ToString(), string.Empty);
            }

            return string.IsNullOrWhiteSpace(safeName) ? "ComposedSprite" : safeName;
        }

        /// <summary>
        /// RenderTexture가 허용하는 안티앨리어싱 샘플 값으로 보정합니다.
        /// </summary>
        /// <param name="value">사용자가 입력한 샘플 값입니다.</param>
        /// <returns>1, 2, 4, 8 중 하나로 보정된 값입니다.</returns>
        private static int NormalizeAntiAliasing(int value)
        {
            if (value <= 1)
            {
                return 1;
            }

            if (value <= 2)
            {
                return 2;
            }

            if (value <= 4)
            {
                return 4;
            }

            return 8;
        }
    }
}
