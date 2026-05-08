using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 글리치 렌더 패스와 컨트롤러 사이의 런타임 상태를 중계하는 싱글턴 서비스입니다.
    /// 효과를 시작한 컨트롤러를 소유자로 기록하여 오래된 컨트롤러가 최신 글리치 상태를 지우지 않도록 보호합니다.
    /// </summary>
    public sealed class CutsceneGlitchService : MonoBehaviour
    {
        private static CutsceneGlitchService _instance;

        private object _owner;
        private bool _hasActiveState;
        private ScreenGlitchState _currentState;

        /// <summary>
        /// 현재 글리치 효과가 렌더링될 수 있는 상태인지 안전하게 확인합니다.
        /// 싱글턴이 아직 생성되지 않은 경우에는 <see langword="false"/>를 반환합니다.
        /// </summary>
        public static bool HasActiveGlitchSafe => _instance != null && _instance._hasActiveState;

        /// <summary>
        /// 현재 렌더 패스가 사용할 글리치 상태를 반환합니다.
        /// </summary>
        public static ScreenGlitchState CurrentStateSafe =>
            _instance != null ? _instance._currentState : default;

        /// <summary>
        /// 싱글턴 인스턴스를 반환합니다.
        /// 플레이 중이 아니면 불필요한 오브젝트 생성을 막기 위해 <see langword="null"/>을 반환합니다.
        /// </summary>
        private static CutsceneGlitchService Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                if (!Application.isPlaying)
                {
                    return null;
                }

                CreateSingleton();
                return _instance;
            }
        }

        /// <summary>
        /// 글리치 상태 보관용 싱글턴 오브젝트를 생성합니다.
        /// 이미 존재하거나 플레이 중이 아니면 아무 작업도 하지 않습니다.
        /// </summary>
        private static void CreateSingleton()
        {
            if (_instance != null || !Application.isPlaying)
            {
                return;
            }

            var go = new GameObject(nameof(CutsceneGlitchService));
            _instance = go.AddComponent<CutsceneGlitchService>();
            DontDestroyOnLoad(go);
        }

        /// <summary>
        /// 씬에 직접 배치된 중복 서비스가 있을 경우 하나만 유지합니다.
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// 싱글턴이 제거될 때 렌더 상태와 소유권을 초기화합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _owner = null;
            _hasActiveState = false;
            _currentState = default;
            _instance = null;
        }

        /// <summary>
        /// 지정한 소유자의 글리치 상태를 현재 렌더 상태로 반영합니다.
        /// 새 소유자가 호출하면 기존 소유권을 넘겨받아 이후 해제 요청도 새 소유자 기준으로 처리됩니다.
        /// </summary>
        /// <param name="owner">글리치 상태를 제어하는 컨트롤러 인스턴스입니다.</param>
        /// <param name="state">렌더 패스에 전달할 글리치 상태입니다.</param>
        public static void ApplyState(object owner, ScreenGlitchState state)
        {
            if (owner == null)
            {
                return;
            }

            var service = Instance;
            if (service == null)
            {
                return;
            }

            service._owner = owner;
            service._currentState = state;
            service._hasActiveState = state.IsActive();
        }

        /// <summary>
        /// 지정한 소유자가 현재 글리치 소유자인 경우에만 렌더 상태를 해제합니다.
        /// 겹쳐 실행된 새 글리치가 이전 글리치의 종료 처리에 의해 꺼지는 상황을 방지합니다.
        /// </summary>
        /// <param name="owner">글리치 해제를 요청하는 컨트롤러 인스턴스입니다.</param>
        public static void ClearOwner(object owner)
        {
            if (_instance == null || owner == null || !ReferenceEquals(_instance._owner, owner))
            {
                return;
            }

            _instance._owner = null;
            _instance._hasActiveState = false;
            _instance._currentState = default;
        }
    }
}
