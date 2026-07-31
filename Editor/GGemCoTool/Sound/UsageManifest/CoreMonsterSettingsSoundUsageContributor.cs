using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 프로젝트에 등록된 몬스터 설정 에셋의 공용 피격 사운드를 전역 사운드 사용 범위에 추가합니다.
    /// </summary>
    public sealed class CoreMonsterSettingsSoundUsageContributor :
        ISoundUsageManifestContributor,
        ISoundUsageManifestSourceContributor
    {
        private const string AssetFilter = "t:GGemCoMonsterSettings";

        /// <summary>
        /// Core 기본 설정 사운드를 외부 패키지 분석보다 먼저 수집하도록 실행 순서를 반환합니다.
        /// </summary>
        public int Order => -100;

        /// <summary>
        /// 사운드 사용 매니페스트 진단에 표시할 분석기 이름입니다.
        /// </summary>
        public string DisplayName => "Core Monster Settings";

        /// <summary>
        /// 모든 몬스터 설정 에셋에서 유효한 공용 피격 SFX UID를 수집합니다.
        /// </summary>
        /// <param name="context">전역 사운드 사용처와 경고를 추가할 생성 컨텍스트입니다.</param>
        public void Collect(SoundUsageManifestBuildContext context)
        {
            if (context == null)
                return;

            string[] assetGuids = AssetDatabase.FindAssets(AssetFilter);
            for (int i = 0; i < assetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                GGemCoMonsterSettings settings =
                    AssetDatabase.LoadAssetAtPath<GGemCoMonsterSettings>(assetPath);
                if (settings == null || settings.IncomingHitSoundUid <= 0)
                    continue;

                int soundUid = settings.IncomingHitSoundUid;
                if (!context.TryGetSound(soundUid, out StruckTableSound sound))
                {
                    context.AddWarning(
                        $"몬스터 공용 피격 sound UID가 sound 테이블에 없습니다. " +
                        $"soundUid={soundUid}, source={assetPath}");
                    continue;
                }

                if (sound.Type != SoundConstants.Type.Sfx)
                {
                    context.AddWarning(
                        $"몬스터 공용 피격 사운드는 SFX 타입이어야 합니다. " +
                        $"soundUid={soundUid}, type={sound.Type}, source={assetPath}");
                    continue;
                }

                context.AddGlobalSoundUsage(
                    soundUid,
                    SoundUsageManifestSourceType.PackageSettings,
                    0,
                    assetPath,
                    nameof(GGemCoMonsterSettings.IncomingHitSoundUid));
            }
        }

        /// <summary>
        /// 몬스터 설정 변경 시 사운드 사용 매니페스트 재생성 필요 여부를 감지할 수 있도록 원본 경로를 등록합니다.
        /// </summary>
        /// <param name="context">원본 에셋 경로를 수집하는 컨텍스트입니다.</param>
        public void CollectSourcePaths(SoundUsageManifestSourceContext context)
        {
            if (context == null)
                return;

            string[] assetGuids = AssetDatabase.FindAssets(AssetFilter);
            for (int i = 0; i < assetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                context.AddPath(assetPath);
            }
        }
    }
}
