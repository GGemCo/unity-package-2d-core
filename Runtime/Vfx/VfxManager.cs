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

            EnsureAnimationController(instance, behaviour, info);
            behaviour.Initialize(info, ReleaseToPool);
            ApplyRequest(instance, behaviour, info, request);
            instance.SetActive(true);
            return behaviour;
        }

        private void ApplyRequest(GameObject instance, VfxBehaviourBase behaviour, StruckTableVfx info, VfxSpawnRequest request)
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
                behaviour.SetFollowCharacter(request.FollowTarget, ResolveFollowMode(info, true));

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

            switch (info.AttachType)
            {
                case VfxConstants.AttachType.Owner:
                    if (owner != null)
                        behaviour.SetFollowCharacter(owner, ResolveFollowMode(info, false));
                    break;
                case VfxConstants.AttachType.Target:
                    if (request.Target != null)
                        behaviour.SetFollowCharacter(request.Target, ResolveFollowMode(info, false));
                    break;
                case VfxConstants.AttachType.UI:
                    if (_sceneGame != null && _sceneGame.canvasUI != null)
                        instance.transform.SetParent(_sceneGame.canvasUI.transform, false);
                    instance.transform.localPosition = Vector3.zero;
                    break;
            }
        }

        private static VfxBehaviourBase EnsureBehaviour(GameObject instance, StruckTableVfx info)
        {
            if (info == null)
                return null;

            if (info.AssetKind == VfxConstants.AssetKind.Particle || info.PlaybackType == VfxConstants.PlaybackType.ParticleSystem)
                return GetOrAdd<ParticleSystemVfxBehaviour>(instance);

            if (info.Type == VfxConstants.Type.Laser || info.PlaybackType == VfxConstants.PlaybackType.Laser)
                return GetOrAdd<VfxLaser>(instance);

            return GetOrAdd<DefaultVfx>(instance);
        }

        private static VfxConstants.FollowMode ResolveFollowMode(StruckTableVfx info, bool isExplicitFollowRequest)
        {
            if (info == null)
                return isExplicitFollowRequest ? VfxConstants.FollowMode.Position : VfxConstants.FollowMode.None;

            if (info.FollowMode != VfxConstants.FollowMode.None)
                return info.FollowMode;

            return isExplicitFollowRequest ? VfxConstants.FollowMode.Position : VfxConstants.FollowMode.Position;
        }

        private void EnsureAnimationController(GameObject instance, VfxBehaviourBase behaviour, StruckTableVfx info)
        {
            if (behaviour is ParticleSystemVfxBehaviour || info == null || info.AssetKind == VfxConstants.AssetKind.Particle)
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
