using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxManager
    {
        private SceneGame _sceneGame;
        private AnimationEventMediator _animationEventMediator;
        private VfxPoolService _poolService;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            _poolService = new VfxPoolService();
        }

        public VfxBehaviourBase CreateVfx(int vfxUid)
        {
            return CreateVfx(new VfxSpawnRequest { VfxUid = vfxUid });
        }

        public VfxBehaviourBase CreateVfx(StruckAnimationEventVfx struckAnimationEventVfx)
        {
            if (struckAnimationEventVfx == null)
                return null;

            return CreateVfx(VfxSpawnRequest.FromAnimationEvent(struckAnimationEventVfx));
        }

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

            string key = $"{ConfigAddressableGroupName.Vfx}_{info.PrefabPath}";
            GameObject prefab = AddressableLoaderPrefabVfx.Instance.GetPrefabByName(key);
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
            EnsureAnimationController(instance, behaviour, info);
            behaviour.Initialize(info, spawnPolicy, ReleaseToPool);
            ApplyRequest(instance, behaviour, info, spawnPolicy, request);
            instance.SetActive(true);
            return behaviour;
        }

        private void ApplyRequest(GameObject instance, VfxBehaviourBase behaviour, VfxRuntimeData info, VfxSpawnPolicy spawnPolicy, VfxSpawnRequest request)
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
                return GetOrAdd<ParticleSystemVfxBehaviour>(instance);

            if (info.EffectType == VfxConstants.EffectType.Laser || info.PlaybackType == VfxConstants.PlaybackType.Laser)
                return GetOrAdd<VfxLaser>(instance);

            return GetOrAdd<DefaultVfx>(instance);
        }

        private static VfxConstants.FollowMode ResolveFollowMode(VfxSpawnPolicy spawnPolicy, bool isExplicitFollowRequest)
        {
            if (spawnPolicy == null)
                return isExplicitFollowRequest ? VfxConstants.FollowMode.Position : VfxConstants.FollowMode.None;

            if (spawnPolicy.FollowMode != VfxConstants.FollowMode.None)
                return spawnPolicy.FollowMode;

            return isExplicitFollowRequest ? VfxConstants.FollowMode.Position : VfxConstants.FollowMode.Position;
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

        private void EnsureAnimationController(GameObject instance, VfxBehaviourBase behaviour, VfxRuntimeData info)
        {
            if (behaviour is ParticleSystemVfxBehaviour || info == null || info is VfxParticleRuntimeData)
            {
                behaviour.VfxAnimationController = null;
                return;
            }

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

            behaviour.VfxAnimationController = vfxAnimationController;
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
