using System.Collections;
using System.IO;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 게임 데이터 저장시 만드는 썸네일 매니저
    /// </summary>
    public class ThumbnailController
    {
        private readonly string thumbnailDirectory;
        private readonly int newWidth;
        private readonly Camera mainCamera;

        public ThumbnailController(string pthumbnailDirectory, int pnewWidth)
        {
            thumbnailDirectory = pthumbnailDirectory;
            newWidth = pnewWidth;
            Directory.CreateDirectory(thumbnailDirectory);
            if (SceneGame.Instance != null)
            {
                mainCamera = SceneGame.Instance.mainCamera;
            }
        }
        /// <summary>
        /// 캡쳐하고 파일로 저장하기
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator CaptureThumbnail(int slot, System.Action onComplete = null)
        {
            yield return new WaitForEndOfFrame();

            if (mainCamera == null)
            {
                GcLogger.LogError("메인 카메라를 찾을 수 없습니다.");
                yield break;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;

            RenderTexture rt = new RenderTexture(screenWidth, screenHeight, 24);
            mainCamera.targetTexture = rt;
            mainCamera.Render();

            RenderTexture.active = rt;
            Texture2D screenshot = new Texture2D(screenWidth, screenHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, screenWidth, screenHeight), 0, 0);
            screenshot.Apply();

            int newHeight = Mathf.RoundToInt((float)screenHeight / screenWidth * newWidth);
            Texture2D resizedScreenshot = ResizeTexture(screenshot, newHeight);

            string savePath = GetThumbnailPath(slot);
            File.WriteAllBytes(savePath, resizedScreenshot.EncodeToPNG());

            // GcLogger.Log($"썸네일 저장 완료: {savePath}");

            mainCamera.targetTexture = null;
            RenderTexture.active = null;
            Object.Destroy(rt);
            Object.Destroy(screenshot);
            Object.Destroy(resizedScreenshot);

            onComplete?.Invoke();  // 완료 후 콜백 실행
        }
        /// <summary>
        /// 썸네일 삭제하기
        /// </summary>
        /// <param name="slot"></param>
        public void DeleteThumbnail(int slot)
        {
            string path = GetThumbnailPath(slot);
            if (File.Exists(path)) File.Delete(path);
            GcLogger.Log($"썸네일 삭제 완료: 슬롯 {slot}");
        }
        /// <summary>
        /// 썸네일 리사이즈 하기
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        private Texture2D ResizeTexture(Texture2D source, int newHeight)
        {
            RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
            resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            resizedTexture.Apply();

            RenderTexture.active = null;
            rt.Release();
            Object.Destroy(rt);

            return resizedTexture;
        }

        public string GetThumbnailPath(int slot) => Path.Combine(thumbnailDirectory, $"Thumbnail_{slot}.png");
    }
}
