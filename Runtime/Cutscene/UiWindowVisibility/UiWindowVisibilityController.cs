using System.Collections;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트에 따라 UIWindowManager에 등록된 UI 창의 표시 상태를 제어합니다.
    /// 필요 시 변경 전 상태를 저장한 뒤 Stop 또는 End 시점에 복원합니다.
    /// </summary>
    public sealed class UiWindowVisibilityController : CutsceneDefaultController, ICutsceneController
    {
        /// <summary>
        /// 현재 처리 중인 UI 창 가시성 이벤트 데이터입니다.
        /// </summary>
        private UiWindowVisibilityData _data;

        /// <summary>
        /// UI 창의 표시 상태를 조회하고 변경하는 매니저입니다.
        /// </summary>
        private UIWindowManager _windowManager;

        /// <summary>
        /// UIWindowManager 스택에 복원 가능한 표시 상태를 저장했는지 여부입니다.
        /// </summary>
        private bool _hasPushedVisibilityState;

        /// <summary>
        /// UI 창 표시 상태를 제어하는 컷신 컨트롤러를 초기화합니다.
        /// </summary>
        /// <param name="manager">이 컨트롤러를 관리하는 컷신 매니저입니다.</param>
        public UiWindowVisibilityController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;


        /// <summary>
        /// 컷신 이벤트 실행 전 필요한 준비를 수행합니다.
        /// 현재 구현에서는 별도 준비 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>준비 완료까지의 코루틴입니다.</returns>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            _windowManager = SceneGame.Instance != null ? SceneGame.Instance.uIWindowManager : null;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// UI 창 표시 상태 변경 이벤트를 실행합니다.
        /// 대상 창의 현재 상태를 저장한 뒤 지정된 표시 여부를 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.UiWindowVisibility)
            {
                return;
            }

            _data = evt.uiWindowVisibility ?? new UiWindowVisibilityData();
            _windowManager = SceneGame.Instance != null ? SceneGame.Instance.uIWindowManager : null;
            if (_windowManager == null)
            {
                GcLogger.LogError("UiWindowVisibilityController: UIWindowManager를 찾을 수 없습니다.");
                return;
            }

            var targetWindows = ResolveTargetWindows(_windowManager, _data);
            if (targetWindows.Count <= 0)
            {
                return;
            }

            _hasPushedVisibilityState = false;
            if (_data.restoreOnStop || _data.restoreOnCutsceneEnd)
            {
                _hasPushedVisibilityState = _windowManager.PushVisibilityState(targetWindows);
            }

            _windowManager.SetWindowsVisible(targetWindows, _data.show);
        }

        /// <summary>
        /// 매 프레임 호출되는 업데이트를 처리합니다.
        /// 현재 컨트롤러는 프레임 기반 추가 동작을 수행하지 않습니다.
        /// </summary>
        public void Update()
        {
        }

        /// <summary>
        /// 컨트롤러 중지 시점에 필요한 정리를 수행합니다.
        /// 설정에 따라 저장된 UI 창 표시 상태를 복원합니다.
        /// </summary>
        public void Stop()
        {
            if (_data is { restoreOnStop: true })
            {
                RestoreSnapshot();
            }
        }

        /// <summary>
        /// 컷신 종료 시점에 필요한 정리를 수행합니다.
        /// 설정에 따라 저장된 UI 창 표시 상태를 복원합니다.
        /// </summary>
        public void End()
        {
            if (_data is { restoreOnCutsceneEnd: true })
            {
                RestoreSnapshot();
            }
        }

        /// <summary>
        /// 저장해 둔 UI 창 표시 상태 스냅샷을 복원합니다.
        /// 복원 후에는 동일 스냅샷이 다시 사용되지 않도록 상태를 초기화합니다.
        /// </summary>
        private void RestoreSnapshot()
        {
            if (!_hasPushedVisibilityState || _windowManager == null)
            {
                return;
            }

            if (_windowManager.PopVisibilityState())
            {
                _hasPushedVisibilityState = false;
            }
        }

        /// <summary>
        /// 설정된 모드에 따라 실제로 표시 상태를 변경할 대상 UI 창 목록을 계산합니다.
        /// </summary>
        /// <param name="windowManager">관리 중인 UI 창 정보를 제공하는 매니저입니다.</param>
        /// <param name="data">대상 선정 방식과 표시 옵션이 담긴 데이터입니다.</param>
        /// <returns>표시 상태 변경 대상이 되는 UI 창 식별자 목록입니다.</returns>
        private static List<UIWindowConstants.WindowUid> ResolveTargetWindows(UIWindowManager windowManager, UiWindowVisibilityData data)
        {
            var managedWindows = windowManager.GetManagedWindowUids();
            if (managedWindows == null || managedWindows.Count <= 0)
            {
                return new List<UIWindowConstants.WindowUid>();
            }

            switch (data.mode)
            {
                case UiWindowVisibilityMode.IncludeOnly:
                    return FilterExistingWindowUids(windowManager, data.targetWindows);

                case UiWindowVisibilityMode.AllExcept:
                {
                    var excepts = new HashSet<UIWindowConstants.WindowUid>(FilterExistingWindowUids(windowManager, data.exceptWindows));
                    var result = new List<UIWindowConstants.WindowUid>();
                    foreach (var windowUid in managedWindows)
                    {
                        if (!excepts.Contains(windowUid))
                        {
                            result.Add(windowUid);
                        }
                    }

                    return result;
                }

                case UiWindowVisibilityMode.All:
                default:
                    return managedWindows;
            }
        }

        /// <summary>
        /// 입력된 UI 창 식별자 목록에서 실제로 관리 중인 유효한 항목만 중복 없이 추출합니다.
        /// </summary>
        /// <param name="windowManager">UI 창 관리 여부를 확인할 매니저입니다.</param>
        /// <param name="source">필터링할 원본 UI 창 식별자 목록입니다.</param>
        /// <returns>유효하고 중복이 제거된 UI 창 식별자 목록입니다.</returns>
        private static List<UIWindowConstants.WindowUid> FilterExistingWindowUids(UIWindowManager windowManager, List<UIWindowConstants.WindowUid> source)
        {
            var result = new List<UIWindowConstants.WindowUid>();
            if (source == null || source.Count <= 0)
            {
                return result;
            }

            var seen = new HashSet<UIWindowConstants.WindowUid>();
            foreach (var windowUid in source)
            {
                if (windowUid == UIWindowConstants.WindowUid.None || seen.Contains(windowUid))
                {
                    continue;
                }

                if (!windowManager.HasManagedWindow(windowUid))
                {
                    continue;
                }

                seen.Add(windowUid);
                result.Add(windowUid);
            }

            return result;
        }
    }
}