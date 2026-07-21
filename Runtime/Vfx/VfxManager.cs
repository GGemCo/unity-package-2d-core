using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxManager
    {
        private SceneGame _sceneGame;
        private AnimationEventMediator _animationEventMediator;
        private VfxPoolService _poolService;
        private VfxResolver _vfxResolver;
        private bool _didInitialPrewarm;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            _poolService = new VfxPoolService();
            _vfxResolver = new VfxResolver(TableLoaderManager.Instance);
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
        /// AnimationEvent VFX 데이터를 이벤트 발생 오브젝트 기준으로 해석하여 VFX를 생성합니다.
        /// </summary>
        /// <param name="struckAnimationEventVfx">AnimationEvent에서 전달된 VFX 데이터입니다.</param>
        /// <param name="fromObject">AnimationEvent를 발생시킨 오브젝트입니다.</param>
        /// <returns>생성 및 초기화된 VFX Behaviour입니다. 생성할 수 없으면 null을 반환합니다.</returns>
        public VfxBehaviourBase CreateVfx(StruckAnimationEventVfx struckAnimationEventVfx, GameObject fromObject)
        {
            if (struckAnimationEventVfx == null)
                return null;

            return CreateVfx(VfxSpawnRequest.FromAnimationEvent(struckAnimationEventVfx, fromObject));
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

            if (!TryResolveVfx(request.VfxUid, out ResolvedVfx resolved))
            {
                GcLogger.LogError("vfx 테이블에 없는 데이터 입니다. vfx Uid: " + request.VfxUid);
                return null;
            }

            if (!resolved.ShouldPlay)
                return null;

            ApplyResolvedOverrides(ref request, resolved);
            VfxRuntimeData info = resolved.RuntimeData;
            if (info == null)
                return null;

            GameObject prefab = ResolvePrefab(info);
            if (prefab == null)
                return null;

            int poolKey = ResolvePoolKey(info, request);
            _poolService.Configure(info, prefab, poolKey);
            GameObject instance = _poolService.Acquire(poolKey, prefab);
            if (instance == null)
                return null;

            var behaviour = EnsureBehaviour(instance, info, request);
            if (behaviour == null)
                return null;

            var spawnPolicy = ResolveSpawnPolicy(info, request);
            IVfxAnimationController animationController = EnsureAnimationController(instance, behaviour, info);
            EnsureRequiredComponentsEnabled(instance, behaviour, animationController);
            behaviour.Initialize(info, spawnPolicy, ReleaseToPool, poolKey);
            EnsureRequiredComponentsEnabled(instance, behaviour, animationController);
            ApplyRequest(instance, behaviour, spawnPolicy, request);
            instance.SetActive(true);
            return behaviour;
        }


        /// <summary>
        /// 대표 VFX UID를 실제 생성 대상 정보로 해석합니다.
        /// 커스텀 테스트 툴과 디버그 코드가 실제 게임 생성 경로와 같은 해석 결과를 확인할 때 사용합니다.
        /// </summary>
        /// <param name="vfxUid">외부 시스템이 사용하는 대표 VFX UID입니다.</param>
        /// <param name="resolved">해석된 최종 VFX 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다. 무출력 후보도 성공 결과로 반환될 수 있습니다.</returns>
        public bool TryResolveVfx(int vfxUid, out ResolvedVfx resolved)
        {
            resolved = default;
            TableLoaderManager tableLoader = TableLoaderManager.Instance;
            if (tableLoader == null)
                return false;

            if (_vfxResolver == null)
                _vfxResolver = new VfxResolver(tableLoader);

            return _vfxResolver.TryResolve(vfxUid, out resolved);
        }

        /// <summary>
        /// Variant 후보에서 지정한 요청 보정값을 실제 생성 요청에 병합합니다.
        /// 명시 요청 값이 이미 있으면 호출부의 요청을 우선합니다.
        /// </summary>
        /// <param name="request">생성 요청입니다.</param>
        /// <param name="resolved">VFX 해석 결과입니다.</param>
        private static void ApplyResolvedOverrides(ref VfxSpawnRequest request, ResolvedVfx resolved)
        {
            if (request.ScaleOverride <= 0f && resolved.ScaleOverride > 0f)
                request.ScaleOverride = resolved.ScaleOverride;

            if (request.DurationOverride == 0f && resolved.DurationOverride > 0f)
                request.DurationOverride = resolved.DurationOverride;

            if (string.IsNullOrWhiteSpace(request.ColorOverride) && !string.IsNullOrWhiteSpace(resolved.ColorOverride))
                request.ColorOverride = resolved.ColorOverride;
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

        /// <summary>
        /// 생성 요청에 포함된 소유자, Follow, 위치, 렌더링 옵션을 VFX 인스턴스에 적용합니다.
        /// </summary>
        /// <param name="instance">풀에서 가져온 VFX 인스턴스입니다.</param>
        /// <param name="behaviour">VFX 생명주기를 담당하는 Behaviour입니다.</param>
        /// <param name="spawnPolicy">이번 생성에 사용할 Spawn 정책입니다.</param>
        /// <param name="request">이번 VFX 생성 요청입니다.</param>
        /// <remarks>
        /// 위치 관련 옵션을 모두 적용한 뒤 SetActive(true) 전에 최종 위치를 확정하여,
        /// 생성 프레임에 이펙트가 기본 위치에서 보였다가 보정 위치로 이동하는 현상을 방지합니다.
        /// </remarks>
        private void ApplyRequest(GameObject instance, VfxBehaviourBase behaviour, VfxSpawnPolicy spawnPolicy, VfxSpawnRequest request)
        {
            if (request.Parent != null)
                instance.transform.SetParent(request.Parent, false);
            else if (request.ForceUiCanvasParent && _sceneGame != null && _sceneGame.canvasUI != null)
                instance.transform.SetParent(_sceneGame.canvasUI.transform, false);

            var owner = request.Owner;
            if (owner == null && request.OwnerGameObject != null)
                owner = request.OwnerGameObject.GetComponent<CharacterBase>();

            CharacterBase attachOwner = request.IgnoreOwnerAttachPolicy ? null : owner;
            CharacterBase followCharacter = null;
            CharacterBase heightOwner = owner != null ? owner : request.Target;

            behaviour.SetForceOneShot(request.ForceOneShot);
            behaviour.SetFadeInDisabled(request.DisableFadeIn);

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

            behaviour.SetPositionOffset(request.PositionOffset);
            behaviour.SetPositionY(request.PositionY);
            behaviour.SetPositionYType(request.PositionYType);

            if (owner != null)
                behaviour.SetCreateCharacter(owner, !request.DisableOwnerFlipOnSpawn);

            if (request.FollowTarget != null)
            {
                followCharacter = request.FollowTarget;
                heightOwner = followCharacter;
                behaviour.SetFollowCharacter(
                    followCharacter,
                    ResolveFollowMode(spawnPolicy, true),
                    ResolveFollowAnchorMode(spawnPolicy));
            }

            switch (spawnPolicy.AttachType)
            {
                case VfxConstants.AttachType.Owner:
                    if (attachOwner != null)
                    {
                        followCharacter = attachOwner;
                        heightOwner = followCharacter;
                        behaviour.SetFollowCharacter(
                            followCharacter,
                            ResolveFollowMode(spawnPolicy, false),
                            ResolveFollowAnchorMode(spawnPolicy));
                    }
                    break;
                case VfxConstants.AttachType.Target:
                    if (request.Target != null)
                    {
                        followCharacter = request.Target;
                        heightOwner = followCharacter;
                        behaviour.SetFollowCharacter(
                            followCharacter,
                            ResolveFollowMode(spawnPolicy, false),
                            ResolveFollowAnchorMode(spawnPolicy));
                    }
                    break;
                case VfxConstants.AttachType.UI:
                    if (_sceneGame != null && _sceneGame.canvasUI != null)
                        instance.transform.SetParent(_sceneGame.canvasUI.transform, false);
                    instance.transform.localPosition = Vector3.zero;
                    return;
            }

            Vector3 basePosition = ResolveInitialBasePosition(
                instance,
                owner,
                followCharacter,
                request,
                ResolveFollowAnchorMode(spawnPolicy));
            behaviour.ApplySpawnPositionImmediate(basePosition, heightOwner);
            ApplyDirectionVisualIfNeeded(behaviour, request);
        }

        /// <summary>
        /// 생성 요청에 방향 정보가 있으면 VFX의 DefaultDirection/NeedRotation 정책을 적용합니다.
        /// </summary>
        /// <param name="behaviour">방향 보정을 적용할 VFX Behaviour입니다.</param>
        /// <param name="request">이번 VFX 생성 요청입니다.</param>
        private static void ApplyDirectionVisualIfNeeded(VfxBehaviourBase behaviour, VfxSpawnRequest request)
        {
            if (behaviour == null || !request.UseDirection)
                return;

            behaviour.ApplyDirectionVisual(
                request.Direction,
                request.SourceDirection,
                !request.DisableDefaultDirectionFlip,
                !request.DisableDirectionRotation);
        }

        /// <summary>
        /// VFX 활성화 전에 사용할 기준 월드 위치를 결정합니다.
        /// </summary>
        /// <param name="instance">풀에서 가져온 VFX 인스턴스입니다.</param>
        /// <param name="owner">생성 요청의 소유 캐릭터입니다.</param>
        /// <param name="followCharacter">최종 Follow 대상으로 결정된 캐릭터입니다.</param>
        /// <param name="request">이번 VFX 생성 요청입니다.</param>
        /// <param name="followAnchorMode">Follow 대상이 있을 때 사용할 위치 기준 정책입니다.</param>
        /// <returns>Y 오프셋 적용 전 기준 월드 위치입니다.</returns>
        /// <remarks>
        /// 기본 Follow는 기존 호환성을 위해 Follow 대상 위치를 우선 사용합니다.
        /// SpawnPosition Follow는 명시 위치가 있으면 그 위치를 우선 사용해, HitArea 랜덤 위치처럼
        /// 스폰 시점에 계산된 위치를 Follow 오프셋으로 저장할 수 있게 합니다.
        /// </remarks>
        private static Vector3 ResolveInitialBasePosition(
            GameObject instance,
            CharacterBase owner,
            CharacterBase followCharacter,
            VfxSpawnRequest request,
            VfxConstants.FollowAnchorMode followAnchorMode)
        {
            if (followAnchorMode != VfxConstants.FollowAnchorMode.SpawnPosition && followCharacter != null)
                return followCharacter.transform.position;

            if (request.WorldPosition.HasValue)
                return request.WorldPosition.Value;

            if (followCharacter != null)
                return followCharacter.transform.position;

            if (owner != null)
                return owner.transform.position;

            if (request.Target != null)
                return request.Target.transform.position;

            return instance != null ? instance.transform.position : Vector3.zero;
        }

        /// <summary>
        /// 생성 요청과 VFX 데이터에 맞는 Behaviour 컴포넌트를 보장합니다.
        /// </summary>
        /// <param name="instance">VFX 인스턴스 GameObject입니다.</param>
        /// <param name="info">VFX 테이블에서 해석한 런타임 데이터입니다.</param>
        /// <param name="request">이번 VFX 생성 요청입니다.</param>
        /// <returns>생성 또는 조회된 VFX Behaviour입니다. 결정할 수 없으면 null을 반환합니다.</returns>
        private static VfxBehaviourBase EnsureBehaviour(GameObject instance, VfxRuntimeData info, VfxSpawnRequest request)
        {
            if (info == null)
                return null;

            if (request.ForceLaserEffectBehaviour)
                return GetOrAdd<VfxEffectLaser>(instance);

            if (info is VfxParticleRuntimeData || info.PlaybackType == VfxConstants.PlaybackType.ParticleSystem)
                return GetOrAdd<VfxBehaviourParticle>(instance);

            return GetOrAdd<VfxBehaviourEffect>(instance);
        }

        /// <summary>
        /// Behaviour 정책이 다른 동일 실제 VFX 리소스가 풀 인스턴스를 공유하지 않도록 풀 키를 계산합니다.
        /// </summary>
        /// <param name="info">VFX 테이블에서 해석한 런타임 데이터입니다.</param>
        /// <param name="request">이번 VFX 생성 요청입니다.</param>
        /// <returns>실제 리소스 UID 기본 풀 또는 레이저 전용 풀을 가리키는 키입니다.</returns>
        private static int ResolvePoolKey(VfxRuntimeData info, VfxSpawnRequest request)
        {
            if (info == null)
                return 0;

            return request.ForceLaserEffectBehaviour ? -info.Uid : info.Uid;
        }

        /// <summary>
        /// Spawn 정책과 명시 Follow 요청 여부를 기준으로 실제 Follow 모드를 해석합니다.
        /// </summary>
        /// <param name="spawnPolicy">이번 생성에 적용된 Spawn 정책입니다.</param>
        /// <param name="isExplicitFollowRequest">요청 객체가 직접 FollowTarget을 지정했는지 여부입니다.</param>
        /// <returns>런타임 Follow 모드입니다.</returns>
        private static VfxConstants.FollowMode ResolveFollowMode(VfxSpawnPolicy spawnPolicy, bool isExplicitFollowRequest)
        {
            if (spawnPolicy == null)
                return isExplicitFollowRequest ? VfxConstants.FollowMode.Position : VfxConstants.FollowMode.None;

            if (spawnPolicy.FollowMode != VfxConstants.FollowMode.None)
                return spawnPolicy.FollowMode;

            return VfxConstants.FollowMode.Position;
        }

        /// <summary>
        /// Spawn 정책에서 Follow 위치 기준 정책을 해석합니다.
        /// </summary>
        /// <param name="spawnPolicy">이번 생성에 적용된 Spawn 정책입니다.</param>
        /// <returns>Follow 위치 기준 정책입니다.</returns>
        private static VfxConstants.FollowAnchorMode ResolveFollowAnchorMode(VfxSpawnPolicy spawnPolicy)
        {
            return spawnPolicy != null
                ? spawnPolicy.FollowAnchorMode
                : VfxConstants.FollowAnchorMode.FollowTargetOrigin;
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

            if (request.FollowAnchorModeOverride.HasValue)
                resolved.FollowAnchorMode = request.FollowAnchorModeOverride.Value;

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

        /// <summary>
        /// 수명이 끝난 VFX 인스턴스를 생성 시점에 결정된 풀 버킷으로 반환합니다.
        /// </summary>
        /// <param name="poolKey">반환할 VFX 풀 버킷 키입니다.</param>
        /// <param name="instance">반환할 VFX 인스턴스입니다.</param>
        private void ReleaseToPool(int poolKey, GameObject instance)
        {
            _poolService.Release(poolKey, instance);
        }

        public void SetAnimationEventMediator(AnimationEventMediator mediator)
        {
            _animationEventMediator = mediator;
        }
    }
}
