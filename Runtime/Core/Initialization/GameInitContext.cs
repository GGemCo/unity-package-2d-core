using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 초기화 단계에서 각 시스템에 전달되는 공통 컨텍스트입니다.
    /// 싱글톤 직접 접근을 줄이고, 초기화 순서를 명시적으로 관리하기 위해 사용합니다.
    /// </summary>
    public sealed class GameInitContext
    {
        /// <summary>
        /// 현재 게임 씬의 진입점입니다.
        /// </summary>
        public SceneGame SceneGame { get; }

        /// <summary>
        /// Core 테이블 로더입니다.
        /// </summary>
        public TableLoaderManager TableLoader { get; }

        /// <summary>
        /// Core 설정 Addressables 로더입니다.
        /// </summary>
        public AddressableLoaderSettings SettingsLoader { get; }

        /// <summary>
        /// 저장 데이터 매니저입니다.
        /// SceneGame의 매니저 생성 이후 설정됩니다.
        /// </summary>
        public SaveDataManagerBase SaveDataManager { get; private set; }

        /// <summary>
        /// 컨텍스트가 유효한 Core 설정을 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasSettings => SettingsLoader != null && SettingsLoader.settings != null;

        /// <summary>
        /// 컨텍스트가 유효한 테이블 로더를 가지고 있는지 확인합니다.
        /// </summary>
        public bool HasTableLoader => TableLoader != null;

        /// <summary>
        /// 게임 초기화 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="sceneGame">현재 게임 씬 진입점입니다.</param>
        /// <param name="tableLoader">Core 테이블 로더입니다.</param>
        /// <param name="settingsLoader">Core 설정 로더입니다.</param>
        public GameInitContext(SceneGame sceneGame, TableLoaderManager tableLoader, AddressableLoaderSettings settingsLoader)
        {
            SceneGame = sceneGame;
            TableLoader = tableLoader;
            SettingsLoader = settingsLoader;
        }

        /// <summary>
        /// SceneGame에서 생성한 저장 데이터 매니저를 컨텍스트에 연결합니다.
        /// </summary>
        /// <param name="saveDataManager">저장 데이터 매니저입니다.</param>
        public void SetSaveDataManager(SaveDataManagerBase saveDataManager)
        {
            SaveDataManager = saveDataManager;
        }

        /// <summary>
        /// 초기화에 필요한 핵심 의존성이 준비되었는지 검사하고, 누락된 항목을 로그로 출력합니다.
        /// </summary>
        /// <param name="logPrefix">로그 앞에 붙일 시스템 이름입니다.</param>
        /// <returns>필수 의존성이 모두 준비되어 있으면 true입니다.</returns>
        public bool ValidateCoreDependencies(string logPrefix)
        {
            bool isValid = true;

            if (SceneGame == null)
            {
                GcLogger.LogError($"[{logPrefix}] SceneGame 참조가 없습니다.");
                isValid = false;
            }

            if (TableLoader == null)
            {
                GcLogger.LogError($"[{logPrefix}] TableLoaderManager 참조가 없습니다.");
                isValid = false;
            }

            if (SettingsLoader == null || SettingsLoader.settings == null)
            {
                GcLogger.LogError($"[{logPrefix}] AddressableLoaderSettings 또는 GGemCoSettings가 준비되지 않았습니다.");
                isValid = false;
            }

            return isValid;
        }
    }
}
