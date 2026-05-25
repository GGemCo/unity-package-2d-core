#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Text;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 디버그 정보를 HUD 문자열로 구성하는 Provider입니다.
    /// </summary>
    [DebugHudProvider(500)]
    public sealed class CutsceneHud : IDebugHudProvider
    {
        private readonly StringBuilder _builder = new(256);
        private bool _hasSnapshot;

        /// <summary>
        /// 컷신 HUD 사용 가능 여부를 확인합니다.
        /// </summary>
        /// <param name="settings">전역 디버그 HUD 설정입니다.</param>
        /// <returns>전역 HUD와 컷신 HUD가 모두 활성화되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsEnabled(GGemCoSettings settings)
        {
            if (settings == null || !settings.EnableDebugHud)
            {
                return false;
            }

            GGemCoCutsceneSettings cutsceneSettings = GetCutsceneSettings();
            return cutsceneSettings != null && cutsceneSettings.EnableCutsceneDebugHud;
        }

        /// <summary>
        /// 컷신 HUD의 갱신 주기를 반환합니다.
        /// </summary>
        /// <param name="settings">전역 디버그 HUD 설정입니다.</param>
        /// <returns>컷신 설정에서 지정한 갱신 주기(최소 0.05초)입니다.</returns>
        public float GetUpdateInterval(GGemCoSettings settings)
        {
            GGemCoCutsceneSettings cutsceneSettings = GetCutsceneSettings();
            return cutsceneSettings != null
                ? Mathf.Max(0.05f, cutsceneSettings.cutsceneDebugHudUpdateInterval)
                : 0.1f;
        }

        /// <summary>
        /// 내부 캐시 상태를 초기화합니다.
        /// </summary>
        public void Reset()
        {
            _builder.Clear();
            _hasSnapshot = false;
        }

        /// <summary>
        /// 컷신 매니저의 최신 스냅샷을 읽어 HUD 문자열을 갱신합니다.
        /// </summary>
        /// <param name="elapsedSeconds">Provider 호출 간 경과 시간(초)입니다.</param>
        public void Tick(float elapsedSeconds)
        {
            _builder.Clear();
            _hasSnapshot = false;

            GGemCoCutsceneSettings cutsceneSettings = GetCutsceneSettings();
            SceneGame sceneGame = SceneGame.Instance;
            CutsceneManager manager = sceneGame != null ? sceneGame.CutsceneManager : null;
            if (cutsceneSettings == null || manager == null)
            {
                return;
            }

            if (!manager.TryGetDebugInfo(out CutsceneManager.CutsceneDebugInfo debugInfo))
            {
                return;
            }

            if (cutsceneSettings.ShowHudOnlyWhilePlaying && !debugInfo.IsPlaying)
            {
                return;
            }

            BuildHudText(debugInfo, cutsceneSettings);
            _hasSnapshot = _builder.Length > 0;
        }

        /// <summary>
        /// 누적한 HUD 문자열을 디버그 매니저 버퍼에 추가합니다.
        /// </summary>
        /// <param name="builder">디버그 HUD 매니저의 최종 출력 버퍼입니다.</param>
        /// <returns>출력할 내용이 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryBuildContent(StringBuilder builder)
        {
            if (!_hasSnapshot || _builder.Length <= 0)
            {
                return false;
            }

            builder.Append(_builder);
            return true;
        }

        /// <summary>
        /// 컷신 스냅샷과 설정을 바탕으로 표시 문자열을 구성합니다.
        /// </summary>
        /// <param name="debugInfo">컷신 매니저가 제공한 런타임 스냅샷입니다.</param>
        /// <param name="settings">컷신 디버그 HUD 표시 설정입니다.</param>
        private void BuildHudText(CutsceneManager.CutsceneDebugInfo debugInfo, GGemCoCutsceneSettings settings)
        {
            _builder.AppendLine("[Cutscene]");

            if (settings.EnableCutsceneUid)
            {
                _builder.Append("Uid: ").AppendLine(debugInfo.CutsceneUid > 0 ? debugInfo.CutsceneUid.ToString() : "-");
            }

            if (settings.EnableCutsceneJsonFileName)
            {
                string fileName = string.IsNullOrWhiteSpace(debugInfo.JsonFileName) ? "-" : debugInfo.JsonFileName;
                _builder.Append("Json: ").AppendLine(fileName);
            }

            if (settings.EnableCutsceneTime)
            {
                float totalDuration = Mathf.Max(0f, debugInfo.TotalDuration);
                float elapsedTime = Mathf.Clamp(debugInfo.ElapsedTime, 0f, totalDuration > 0f ? totalDuration : float.MaxValue);
                float progressPercent = totalDuration > Mathf.Epsilon ? (elapsedTime / totalDuration) * 100f : 0f;
                _builder.Append("Time: ")
                    .Append(elapsedTime.ToString("0.00"))
                    .Append(" / ")
                    .Append(totalDuration.ToString("0.00"))
                    .Append(" sec (")
                    .Append(progressPercent.ToString("0.0"))
                    .AppendLine("%)");
            }

            _builder.Append("State: ")
                .Append(debugInfo.IsPlaying ? "Playing" : debugInfo.IsSessionActive ? "SessionActive" : "Idle");
        }

        /// <summary>
        /// 런타임에 로드된 컷신 설정 에셋을 조회합니다.
        /// </summary>
        /// <returns>로드된 컷신 설정 에셋입니다. 없으면 <see langword="null"/>을 반환합니다.</returns>
        private static GGemCoCutsceneSettings GetCutsceneSettings()
        {
            if (AddressableLoaderSettings.Instance != null && AddressableLoaderSettings.Instance.cutsceneSettings != null)
            {
                return AddressableLoaderSettings.Instance.cutsceneSettings;
            }

            return AddressableLoaderSettingsRegist.Instance != null
                ? AddressableLoaderSettingsRegist.Instance.cutsceneSettings
                : null;
        }
    }
}
#endif
