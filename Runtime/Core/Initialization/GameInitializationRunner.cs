using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core 게임 씬 초기화를 명시적인 Initialize/Activate 단계로 실행하는 러너입니다.
    /// 기존 Awake/Start에 분산된 게임 필수 초기화를 점진적으로 이관하기 위한 진입점입니다.
    /// </summary>
    public sealed class GameInitializationRunner : MonoBehaviour
    {
        private bool _isInitialized;
        private bool _isActivated;
        private GameInitContext _context;

        /// <summary>
        /// 현재 초기화 컨텍스트입니다.
        /// </summary>
        public GameInitContext Context => _context;

        /// <summary>
        /// Core 게임 씬 초기화를 실행합니다.
        /// </summary>
        /// <param name="sceneGame">초기화할 게임 씬 진입점입니다.</param>
        /// <returns>초기화와 활성화가 정상적으로 완료되면 true입니다.</returns>
        public bool RunCoreScene(SceneGame sceneGame)
        {
            if (_isInitialized && _isActivated)
            {
                return true;
            }

            _context = CreateCoreContext(sceneGame);
            if (!_context.ValidateCoreDependencies(nameof(GameInitializationRunner)))
            {
                return false;
            }

            Initialize(sceneGame, _context);
            Activate(sceneGame, _context);
            return _isInitialized && _isActivated;
        }

        /// <summary>
        /// 현재 싱글톤 상태를 기준으로 Core 초기화 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="sceneGame">초기화할 게임 씬 진입점입니다.</param>
        /// <returns>생성된 초기화 컨텍스트입니다.</returns>
        private static GameInitContext CreateCoreContext(SceneGame sceneGame)
        {
            return new GameInitContext(
                sceneGame,
                TableLoaderManager.Instance,
                AddressableLoaderSettings.Instance);
        }

        /// <summary>
        /// 초기화 계약을 실행합니다.
        /// </summary>
        /// <param name="target">초기화 대상입니다.</param>
        /// <param name="context">초기화 컨텍스트입니다.</param>
        private void Initialize(IGameInitializable target, GameInitContext context)
        {
            if (_isInitialized)
            {
                return;
            }

            target.Initialize(context);
            _isInitialized = true;
        }

        /// <summary>
        /// 활성화 계약을 실행합니다.
        /// </summary>
        /// <param name="target">활성화 대상입니다.</param>
        /// <param name="context">초기화 컨텍스트입니다.</param>
        private void Activate(IGameActivatable target, GameInitContext context)
        {
            if (_isActivated)
            {
                return;
            }

            target.Activate(context);
            _isActivated = true;
        }
    }
}
