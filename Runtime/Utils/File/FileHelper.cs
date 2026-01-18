using System.IO;

namespace GGemCo2DCore
{
    public static class FileHelper
    {
        /// <summary>
        /// Unity Asset 경로에서 파일 이름을 추출합니다.
        /// </summary>
        /// <param name="assetPath">예: Assets/GGemCo/Images/Icon/Affect/Burn.png</param>
        /// <param name="includeExtension">
        /// true  → Burn.png  
        /// false → Burn
        /// </param>
        /// <returns>파일 이름. 유효하지 않은 경로일 경우 빈 문자열</returns>
        public static string GetFileName(string assetPath, bool includeExtension = true)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            return includeExtension
                ? Path.GetFileName(assetPath)
                : Path.GetFileNameWithoutExtension(assetPath);
        }
    }
}