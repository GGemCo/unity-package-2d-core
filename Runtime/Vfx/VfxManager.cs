using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxManager
    {
        private SceneGame _sceneGame;
        private AnimationEventMediator _animationEventMediator;
        private VfxPoolService _poolService;
        private bool _didInitialPrewarm;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            _poolService = new VfxPoolService();
            TryPrewarmAllConfiguredVfx();
        }

        public void OnStartBySceneGame()
        {
            TryPrewarmAllConfiguredVfx();
        }

        public VfxBehaviourBase CreateVfx(int vfxUid, float duration = 0f)
        {
            return CreateVfx(new VfxSpawnRequest { VfxUid = vfxUid, DurationOverride = duration });
        }

        public VfxBehaviourBase CreateVfx(StruckAnimationEventVfx struckAnimationEventVfx)
        {
            if (struckAnimationEventVfx == null)
                return null;

            return CreateVfx(VfxSpawnRequest.FromAnimationEvent(struckAnimationEventVfx));
        }

        /// <summary>
        /// VFX 생성 요청을 기준으로 프리팹 인스턴스를 풀에서 가져와 초기화하고 활성화합니다.
        /// </summary>
        /// <param name="request">VFX Uid, 위치, 소유자, 지속 시간 등 생성 요청 데이터입니다.</param>
        /// <returns>생성 및 초기화된 VFX Behaviour입니다. 생성할 수 없으면 null을 반환합니다.</returns>
        /// <remarks>
        /// 풀에서 재사용되는 인스턴스는 이전 enabled 상태가 남을 수 있으므로,
        /// 활성화 전에 필수 컴포넌트의 enabled 상태를 복구합니다.
        /// </remarks>
        public VfxBehaviourBase CreateVfx(VfxSpawnRequest request)
        {
            if (request.VfxUid <= 0)
                return null;

            var info = TableLoaderManager.Instance.GetVfxData(request.VfxUid);
            if (info == null)
            {
                GcLogger.LogError("vfx 테이블에 없는 데이터 입니다. vfx Uid: " + request.VfxUid);
                return null;
            }

            GameObject prefab = ResolvePrefab(info);
            if (prefab == null)
                return null;

            _poolService.Configure(info, prefab);
            GameObject instance = _poolService.Acquire(info.Uid, prefab);
            if (instance == null)
                return null;

            var behaviour = EnsureBehaviour(instance, info);
            if (behaviour == null)
                return null;

            var spawnPolicy = ResolveSpawnPolicy(info, request);
            IVfxAnimationController animationController = EnsureAnimationController(instance, behaviour, info);
            EnsureRequiredComponentsEnabled(instance, behaviour, animationController);
            behaviour.Initialize(info, spawnPolicy, ReleaseToPool);
            EnsureRequiredComponentsEnabled(instance, behaviour, animationController);
            ApplyRequest(instance, behaviour, spawnPolicy, request);
            instance.SetActive(true);
            return behaviour;
        }

        private void TryPrewarmAllConfiguredVfx()
        {
            if (_didInitialPrewarm)
                return;

            if (_poolService == null)
                return;

            var tableLoader = TableLoaderManager.Instance;
            var prefabLoader = AddressableLoaderPrefabVfx.Instance;
            if (tableLoader == null || prefabLoader == null)
                return;

            var allVfxData = tableLoader.GetAllVfxData();
            if (allVfxData == null || allVfxData.Count == 0)
            {
                _didInitialPrewarm = true;
                return;
            }

            foreach (var pair in allVfxData)
            {
                var info = pair.Value;
                if (info == null || info.Uid <= 0 || info.PoolPrewarmCount <= 0)
                    continue;

                var prefab = ResolvePrefab(info);
                if (prefab == null)
                    continue;

                _poolService.Configure(info, prefab);
            }

            _didInitialPrewarm = true;
        }

        private static GameObject ResolvePrefab(VfxRuntimeData info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.PrefabPath))
                return null;

            var prefabLoader = AddressableLoaderPrefabVfx.Instance;
            if (prefabLoader == null)
                return null;

            string key = $"{ConfigAddressableGroupName.Vfx}_{info.PrefabPath}";
            if (prefabLoader.TryGetPrefabByName(key, out GameObject prefab))
                return prefab;

            // 시작 로딩에서는 번들 종속성만 준비하므로, 첫 요청 시 실제 프리팹 캐시를 비동기로 채웁니다.
            prefabLoader.RequestPrefabLoad(key);
            return null;
        }

        private void ApplyRequest(GameObject instance, VfxBehaviourBase behaviour, VfxSpawnPolicy spawnPolicy, VfxSpawnRequest request)
        {
            if (request.Parent != null)
                instance.transform.SetParent(request.Parent, false);
            else if (request.ForceUiCanvasParent && _sceneGame != null && _sceneGame.canvasUI != null)
                instance.transform.SetParent(_sceneGame.canvasUI.transform, false);

            if (request.WorldPosition.HasValue)
                instance.transform.position = request.WorldPosition.Value;

            var owner = request.Owner;
            if (owner == null && request.OwnerGameObject != null)
                owner = request.OwnerGameObject.GetComponent<CharacterBase>();
            if (owner != null)
                behaviour.SetCreateCharacter(owner);

            if (request.FollowTarget != null)
                behaviour.SetFollowCharacter(request.FollowTarget, ResolveFollowMode(spawnPolicy, true));

            behaviour.SetForceOneShot(request.ForceOneShot);

            if (request.DurationOverride != 0f)
                behaviour.SetDuration(request.DurationOverride);

            if (request.ScaleOverride > 0f)
                behaviour.SetScale(request.ScaleOverride);

            if (!string.IsNullOrWhiteSpace(request.ColorOverride))
                behaviour.SetColor(request.ColorOverride);

            if (request.SortingLayerOverride.HasValue)
                behaviour.SetSortingLayer(request.SortingLayerOverride.Value);

            if (request.SortingOrderOverride.HasValue)
                behaviour.SetSortingOrder(request.SortingOrderOverride.Value);

            if (request.PositionY != 0f)
                behaviour.SetPositionY(request.PositionY);
            if (request.PositionYType != ConfigCommon.PositionYType.None)
                behaviour.SetPositionYType(request.PositionYType);

            switch (spawnPolicy.AttachType)
            {
                case VfxConstants.AttachType.Owner:
                    if (owner != null)
                        behaviour.SetFollowCharacter(owner, ResolveFollowMode(spawnPolicy, false));
                    break;
                case VfxConstants.AttachType.Target:
                    if (request.Target != null)
                        behaviour.SetFollowCharacter(request.Target, ResolveFollowMode(spawnPolicy, false));
                    break;
                case VfxConstants.AttachType.UI:
                    if (_sceneGame != null && _sceneGame.canvasUI != null)
                        instance.transform.SetParent(_sceneGame.canvasUI.transform, false);
                    instance.transform.localPosition = Vector3.zero;
                    break;
            }
        }

        private static VfxBehaviourBase EnsureBehaviour(GameObject instance, VfxRuntimeData info)
        {
            if (info == null)
                return null;

            if (info is VfxParticleRuntimeData || info.PlaybackType == VfxConstants.PlaybackType.ParticleSystem)
                return GetOrAdd<VfxBehaviourParticle>(instance);

            if (info.EffectType == VfxConstants.EffectType.Laser || info.PlaybackType == VfxConstants.PlaybackType.Laser)
                return GetOrAdd<VfxEffectLaser>(instance);

            return GetOrAdd<VfxBehaviourEffect>(instance);
        }

        private static VfxConstants.FollowMode ResolveFollowMode(VfxSpawnPolicy spawnPolicy, bool isExplicitFollowRequest)
        {
            if (spawnPolicy == null)
                return isExplicitFollowRequest ? VfxConstants.FollowMode.Position : VfxConstants.FollowMode.None;

            if (spawnPolicy.FollowMode != VfxConstants.FollowMode.None)
                return spawnPolicy.FollowMode;

            return VfxConstants.FollowMode.Position;
        }

        private static VfxSpawnPolicy ResolveSpawnPolicy(VfxRuntimeData info, VfxSpawnRequest request)
        {
            var resolved = info?.DefaultSpawnPolicy?.Clone() ?? new VfxSpawnPolicy();

            if (request.LifecycleTypeOverride.HasValue)
                resolved.LifecycleType = request.LifecycleTypeOverride.Value;

            if (request.AttachTypeOverride.HasValue)
                resolved.AttachType = request.AttachTypeOverride.Value;

            if (request.FollowModeOverride.HasValue)
                resolved.FollowMode = request.FollowModeOverride.Value;

            return resolved;
        }

        /// <summary>
        /// VFX 종류에 맞는 애니메이션 컨트롤러를 보장하고 이벤트 리스너를 연결합니다.
        /// </summary>
        /// <param name="instance">VFX 인스턴스 GameObject입니다.</param>
        /// <param name="behaviour">VFX 생명주기를 담당하는 Behaviour입니다.</param>
        /// <param name="info">VFX 런타임 데이터입니다.</param>
        /// <returns>연결된 VFX 애니메이션 컨트롤러입니다. 애니메이션 컨트롤러가 필요 없으면 null을 반환합니다.</returns>
        private IVfxAnimationController EnsureAnimationController(GameObject instance, VfxBehaviourBase behaviour, VfxRuntimeData info)
        {
            if (!(behaviour is VfxBehaviourEffect effectBehaviour) || info == null || info is VfxParticleRuntimeData)
                return null;

            IVfxAnimationController vfxAnimationController = null;
#if GGEMCO_USE_SPINE
            if (info.PlaybackType == VfxConstants.PlaybackType.SpineSequence ||
                info.AnimationController == ConfigCommon.AnimationController.Spine)
            {
                vfxAnimationController = GetOrAdd<VfxAnimationControllerSpine>(instance);
                var spineController = instance.GetComponent<Spine2dController>();
                if (spineController != null && _animationEventMediator != null)
                    spineController.EventListener = _animationEventMediator;
            }
#endif
            if (vfxAnimationController == null)
            {
                vfxAnimationController = GetOrAdd<VfxAnimationControllerSprite>(instance);
                var animatorController = instance.GetComponent<Animation2dController>();
                if (animatorController != null && _animationEventMediator != null)
                    animatorController.EventListener = _animationEventMediator;
            }

            effectBehaviour.VfxAnimationController = vfxAnimationController;
            return vfxAnimationController;
        }

        /// <summary>
        /// VFX 재생에 필요한 필수 컴포넌트가 비활성화되어 있으면 다시 활성화합니다.
        /// </summary>
        /// <param name="instance">VFX 인스턴스 GameObject입니다.</param>
        /// <param name="behaviour">VFX 생명주기를 담당하는 Behaviour입니다.</param>
        /// <param name="animationController">VFX 애니메이션 컨트롤러입니다.</param>
        /// <remarks>
        /// 풀에서 꺼낸 인스턴스나 프리팹에 남아 있는 disabled 상태를 복구하지 않으면,
        /// VfxBehaviourBase.OnEnable이 호출되지 않아 시작 애니메이션이 재생되지 않습니다.
        /// </remarks>
        private static void EnsureRequiredComponentsEnabled(
            GameObject instance,
            VfxBehaviourBase behaviour,
            IVfxAnimationController animationController)
        {
            SetBehaviourEnabled(instance != null ? instance.GetComponent<Animator>() : null);
            SetBehaviourEnabled(behaviour);

            if (animationController is Behaviour controllerBehaviour)
                SetBehaviourEnabled(controllerBehaviour);

            SetBehaviourEnabled(instance != null ? instance.GetComponent<VfxFadeController>() : null);
        }

        /// <summary>
        /// Behaviour 컴포넌트가 꺼져 있으면 켭니다.
        /// </summary>
        /// <param name="behaviour">활성화할 Behaviour 컴포넌트입니다.</param>
        private static void SetBehaviourEnabled(Behaviour behaviour)
        {
            if (behaviour == null || behaviour.enabled)
                return;

            behaviour.enabled = true;
        }

        private static T GetOrAdd<T>(GameObject instance) where T : Component
        {
            var found = instance.GetComponent<T>();
            return found != null ? found : instance.AddComponent<T>();
        }

        private void ReleaseToPool(int vfxUid, GameObject instance)
        {
            _poolService.Release(vfxUid, instance);
        }

        public void SetAnimationEventMediator(AnimationEventMediator mediator)
        {
            _animationEventMediator = mediator;
        }
    }
}
