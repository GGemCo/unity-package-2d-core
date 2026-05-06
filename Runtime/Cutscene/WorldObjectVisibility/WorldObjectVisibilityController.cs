using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 월드 오브젝트의 표시 상태를 제어하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// 캐릭터보다 위에 그려지는 전경 오브젝트를 특정 연출 동안 숨기는 용도로 사용할 수 있으며,
    /// 저장된 원래 상태는 클립 종료 또는 컷신 종료 시 복원할 수 있습니다.
    /// </remarks>
    public sealed class WorldObjectVisibilityController : CutsceneDefaultController, ICutsceneController
    {
        /// <summary>
        /// Renderer 표시 상태 복원을 위한 스냅샷입니다.
        /// </summary>
        private readonly struct RendererVisibilitySnapshot
        {
            public readonly Renderer Renderer;
            public readonly bool Enabled;

            /// <summary>
            /// Renderer 표시 상태 스냅샷을 생성합니다.
            /// </summary>
            /// <param name="renderer">상태를 저장할 Renderer입니다.</param>
            /// <param name="enabled">저장된 Renderer.enabled 값입니다.</param>
            public RendererVisibilitySnapshot(Renderer renderer, bool enabled)
            {
                Renderer = renderer;
                Enabled = enabled;
            }
        }

        /// <summary>
        /// GameObject 활성 상태 복원을 위한 스냅샷입니다.
        /// </summary>
        private readonly struct GameObjectVisibilitySnapshot
        {
            public readonly GameObject GameObject;
            public readonly bool ActiveSelf;

            /// <summary>
            /// GameObject 활성 상태 스냅샷을 생성합니다.
            /// </summary>
            /// <param name="gameObject">상태를 저장할 GameObject입니다.</param>
            /// <param name="activeSelf">저장된 activeSelf 값입니다.</param>
            public GameObjectVisibilitySnapshot(GameObject gameObject, bool activeSelf)
            {
                GameObject = gameObject;
                ActiveSelf = activeSelf;
            }
        }

        private readonly List<RendererVisibilitySnapshot> _rendererSnapshots = new();
        private readonly List<GameObjectVisibilitySnapshot> _gameObjectSnapshots = new();

        private WorldObjectVisibilityData _data;
        private float _duration;
        private float _elapsed;
        private bool _isWaitingForStop;

        /// <summary>
        /// 월드 오브젝트 표시 상태 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public WorldObjectVisibilityController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 사전 준비 단계입니다. 월드 오브젝트는 실행 시점에 검색하므로 별도 준비가 필요하지 않습니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
        }

        /// <summary>
        /// 코루틴 기반 사전 준비 단계입니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>준비 완료까지 진행되는 코루틴입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 월드 오브젝트 표시 상태 이벤트를 시작하고 대상의 원래 상태를 저장한 뒤 새 상태를 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.WorldObjectVisibility)
            {
                return;
            }

            RestoreSnapshot();

            _data = evt.worldObjectVisibility ?? new WorldObjectVisibilityData();
            _duration = Mathf.Max(0f, evt.duration);
            _elapsed = 0f;
            _isWaitingForStop = _data.restoreOnStop && _duration > 0f;

            var targets = ResolveTargets(_data);
            ApplyVisibility(targets, _data);
        }

        /// <summary>
        /// 클립 지속 시간 기반 복원이 필요한 경우 경과 시간을 갱신합니다.
        /// </summary>
        public void Update()
        {
            if (!_isWaitingForStop)
            {
                return;
            }

            _elapsed += CutsceneManager.GetTimelineDeltaTime();
            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 이벤트를 중단하고 설정에 따라 저장된 표시 상태를 복원합니다.
        /// </summary>
        public void Stop()
        {
            _isWaitingForStop = false;

            if (_data is { restoreOnStop: true })
            {
                RestoreSnapshot();
            }
        }

        /// <summary>
        /// 컷신 종료 시 설정에 따라 저장된 표시 상태를 복원합니다.
        /// </summary>
        public void End()
        {
            _isWaitingForStop = false;

            if (_data is { restoreOnCutsceneEnd: true })
            {
                RestoreSnapshot();
                return;
            }

            ClearSnapshots();
        }

        /// <summary>
        /// 데이터 설정에 맞는 CutsceneVisibilityTarget 목록을 찾고 필터링합니다.
        /// </summary>
        /// <param name="data">대상 검색과 필터링에 사용할 설정입니다.</param>
        /// <returns>표시 상태를 변경할 대상 목록입니다.</returns>
        private static List<CutsceneVisibilityTarget> ResolveTargets(WorldObjectVisibilityData data)
        {
            var result = new List<CutsceneVisibilityTarget>();
            if (data == null)
            {
                return result;
            }

            var candidates = FindCandidateTargets(data);
            for (int i = 0; i < candidates.Count; i++)
            {
                var target = candidates[i];
                if (target == null || !IsTargetSelected(target, data))
                {
                    continue;
                }

                result.Add(target);
            }

            return result;
        }

        /// <summary>
        /// 현재 맵 또는 씬 전체에서 컷신 표시 제어 대상 후보를 수집합니다.
        /// </summary>
        /// <param name="data">검색 범위를 결정할 설정입니다.</param>
        /// <returns>검색 범위 안의 CutsceneVisibilityTarget 후보 목록입니다.</returns>
        private static List<CutsceneVisibilityTarget> FindCandidateTargets(WorldObjectVisibilityData data)
        {
            var result = new List<CutsceneVisibilityTarget>();
            if (data.searchEntireScene)
            {
                var inactiveMode = data.includeInactiveTargets
                    ? FindObjectsInactive.Include
                    : FindObjectsInactive.Exclude;
                result.AddRange(Object.FindObjectsByType<CutsceneVisibilityTarget>(inactiveMode, FindObjectsSortMode.None));
                return result;
            }

            Transform currentMap = SceneGame.Instance != null && SceneGame.Instance.mapManager != null
                ? SceneGame.Instance.mapManager.GetCurrentMap()
                : null;

            if (currentMap == null)
            {
                return result;
            }

            result.AddRange(currentMap.GetComponentsInChildren<CutsceneVisibilityTarget>(data.includeInactiveTargets));
            return result;
        }

        /// <summary>
        /// 대상 마커가 현재 데이터의 선택 조건에 포함되는지 확인합니다.
        /// </summary>
        /// <param name="target">검사할 컷신 표시 대상입니다.</param>
        /// <param name="data">선택 조건을 가진 데이터입니다.</param>
        /// <returns>대상이 선택 조건에 포함되면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsTargetSelected(CutsceneVisibilityTarget target, WorldObjectVisibilityData data)
        {
            switch (data.targetMode)
            {
                case WorldObjectVisibilityTargetMode.All:
                    return true;

                case WorldObjectVisibilityTargetMode.AllExcept:
                    return !target.BelongsToAny(data.exceptGroupKeys);

                case WorldObjectVisibilityTargetMode.IncludeOnly:
                default:
                    return target.BelongsToAny(data.targetGroupKeys);
            }
        }

        /// <summary>
        /// 선택된 대상에 표시 상태를 적용하고 복원에 필요한 원래 상태를 저장합니다.
        /// </summary>
        /// <param name="targets">표시 상태를 변경할 대상 목록입니다.</param>
        /// <param name="data">적용할 표시 상태 설정입니다.</param>
        private void ApplyVisibility(List<CutsceneVisibilityTarget> targets, WorldObjectVisibilityData data)
        {
            if (targets == null || targets.Count <= 0 || data == null)
            {
                return;
            }

            switch (data.applyMode)
            {
                case WorldObjectVisibilityApplyMode.GameObjectActive:
                    ApplyGameObjectVisibility(targets, data.show);
                    break;

                case WorldObjectVisibilityApplyMode.RendererOnly:
                default:
                    ApplyRendererVisibility(targets, data.show, data.includeInactiveTargets);
                    break;
            }
        }

        /// <summary>
        /// 대상 GameObject의 활성 상태를 변경하고 기존 상태를 저장합니다.
        /// </summary>
        /// <param name="targets">상태를 변경할 대상 목록입니다.</param>
        /// <param name="show">적용할 활성 상태입니다.</param>
        private void ApplyGameObjectVisibility(List<CutsceneVisibilityTarget> targets, bool show)
        {
            var captured = new HashSet<GameObject>();
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || !captured.Add(target.gameObject))
                {
                    continue;
                }

                _gameObjectSnapshots.Add(new GameObjectVisibilitySnapshot(target.gameObject, target.gameObject.activeSelf));
                target.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// 대상 Renderer의 표시 상태를 변경하고 기존 상태를 저장합니다.
        /// </summary>
        /// <param name="targets">Renderer를 수집할 대상 목록입니다.</param>
        /// <param name="show">적용할 Renderer.enabled 값입니다.</param>
        /// <param name="includeInactive">비활성 자식의 Renderer도 포함할지 여부입니다.</param>
        private void ApplyRendererVisibility(
            List<CutsceneVisibilityTarget> targets,
            bool show,
            bool includeInactive)
        {
            var captured = new HashSet<Renderer>();
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null)
                {
                    continue;
                }

                var renderers = target.GetTargetRenderers(includeInactive);
                for (int j = 0; j < renderers.Length; j++)
                {
                    var renderer = renderers[j];
                    if (renderer == null || !captured.Add(renderer))
                    {
                        continue;
                    }

                    _rendererSnapshots.Add(new RendererVisibilitySnapshot(renderer, renderer.enabled));
                    renderer.enabled = show;
                }
            }
        }

        /// <summary>
        /// 저장된 Renderer 또는 GameObject 표시 상태를 원래 값으로 되돌립니다.
        /// </summary>
        private void RestoreSnapshot()
        {
            for (int i = _rendererSnapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = _rendererSnapshots[i];
                if (snapshot.Renderer != null)
                {
                    snapshot.Renderer.enabled = snapshot.Enabled;
                }
            }

            for (int i = _gameObjectSnapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = _gameObjectSnapshots[i];
                if (snapshot.GameObject != null)
                {
                    snapshot.GameObject.SetActive(snapshot.ActiveSelf);
                }
            }

            ClearSnapshots();
        }

        /// <summary>
        /// 저장된 표시 상태 스냅샷을 모두 제거합니다.
        /// </summary>
        private void ClearSnapshots()
        {
            _rendererSnapshots.Clear();
            _gameObjectSnapshots.Clear();
        }
    }
}
