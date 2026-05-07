using System;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Cutscene Editor에서 최신 JSON 파일을 직접 읽어 게임 실행 중 컷신을 미리 재생하는 Editor 전용 서비스입니다.
    /// </summary>
    internal static class CutsceneEditorPreviewPlayService
    {
        /// <summary>
        /// 지정된 JSON 경로에서 컷신 데이터를 새로 읽어 현재 게임 씬의 컷신 매니저로 재생합니다.
        /// </summary>
        /// <param name="sceneGame">컷신을 재생할 현재 게임 씬 인스턴스입니다.</param>
        /// <param name="jsonPath">프로젝트 기준 또는 절대 경로의 컷신 JSON 파일 경로입니다.</param>
        /// <param name="error">재생 준비에 실패한 경우 사용자에게 표시할 오류 메시지입니다.</param>
        /// <returns>최신 JSON 데이터를 읽고 재생 요청까지 완료했으면 <see langword="true"/>, 실패했으면 <see langword="false"/>를 반환합니다.</returns>
        public static bool TryPlayLatestJson(SceneGame sceneGame, string jsonPath, out string error)
        {
            error = string.Empty;

            if (sceneGame == null)
            {
                error = "게임을 실행해주세요.";
                return false;
            }

            if (sceneGame.CutsceneManager == null)
            {
                error = "CutsceneManager를 찾지 못했습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                error = "Json 경로가 비어 있습니다.";
                return false;
            }

            var fullPath = Path.GetFullPath(jsonPath);
            if (!File.Exists(fullPath))
            {
                error = $"Json 파일을 찾지 못했습니다.\n{jsonPath}";
                return false;
            }

            if (!TryLoadCutsceneData(fullPath, out var cutsceneData, out error))
            {
                return false;
            }

            sceneGame.CutsceneManager.PlayCutsceneForEditorPreview(cutsceneData);
            return true;
        }

        /// <summary>
        /// 컷신 JSON 파일을 읽어 <see cref="CutsceneData"/> 객체로 역직렬화합니다.
        /// </summary>
        /// <param name="fullPath">디스크에서 읽을 컷신 JSON 파일의 절대 경로입니다.</param>
        /// <param name="cutsceneData">역직렬화에 성공한 컷신 데이터입니다.</param>
        /// <param name="error">파일 읽기 또는 JSON 파싱에 실패한 경우의 오류 메시지입니다.</param>
        /// <returns>컷신 데이터 역직렬화에 성공했으면 <see langword="true"/>, 실패했으면 <see langword="false"/>를 반환합니다.</returns>
        private static bool TryLoadCutsceneData(string fullPath, out CutsceneData cutsceneData, out string error)
        {
            cutsceneData = null;
            error = string.Empty;

            try
            {
                var json = File.ReadAllText(fullPath);
                cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(json, CutsceneJsonSettingsUtility.CutsceneJsonSettings);

                if (cutsceneData == null)
                {
                    error = "Json 파일을 파싱하지 못했습니다.";
                    return false;
                }

                if (cutsceneData.events == null)
                {
                    error = "Json 파일에 이벤트 목록이 없습니다.";
                    return false;
                }

                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is JsonException)
            {
                error = $"Json 파일을 불러오지 못했습니다.\n{e.Message}";
                return false;
            }
        }
    }
}
