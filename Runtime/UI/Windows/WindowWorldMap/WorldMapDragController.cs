using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 선택 노드를 화면 중앙으로 이동시키는 옵션입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapNodeCenteringOptions
    {
        /// <summary>
        /// 선택 노드를 화면 중앙으로 이동할지 여부입니다.
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// 중앙 이동에 애니메이션을 적용할지 여부입니다.
        /// </summary>
        public bool useAnimation = true;

        /// <summary>
        /// 가까운 거리에서 사용할 최소 이동 속도입니다.
        /// </summary>
        public float minSpeed = 1200f;

        /// <summary>
        /// 먼 거리에서 사용할 최대 이동 속도입니다.
        /// </summary>
        public float maxSpeed = 2200f;

        /// <summary>
        /// 최대 속도에 가까워지는 기준 거리입니다.
        /// </summary>
        public float maxDistanceForSpeed = 1200f;

        /// <summary>
        /// 중앙 이동 애니메이션의 최소 시간입니다.
        /// </summary>
        public float minDuration = 0.12f;

        /// <summary>
        /// 중앙 이동 애니메이션의 최대 시간입니다.
        /// </summary>
        public float maxDuration = 0.45f;

        /// <summary>
        /// 중앙 이동 애니메이션에 사용할 Easing 타입입니다.
        /// </summary>
        public Easing.EaseType easeType = Easing.EaseType.EaseOutCubic;
    }

    /// <summary>
    /// 월드맵 컨테이너를 포인터 입력으로 이동시키고 viewport 밖 빈 공간이 보이지 않도록 제한합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldMapDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float DefaultDragSensitivity = 1f;

        [Tooltip("드래그 이동 민감도입니다.")]
        [SerializeField] private float dragSensitivity = DefaultDragSensitivity;

        private RectTransform _viewportRect;
        private RectTransform _contentRect;
        private Vector2 _lastPointerLocalPosition;
        private bool _isDragging;
        private Coroutine _centeringRoutine;

        /// <summary>
        /// 드래그 컨트롤러가 사용할 viewport와 content RectTransform을 초기화합니다.
        /// </summary>
        /// <param name="viewportRect">월드맵이 보이는 기준 영역입니다.</param>
        /// <param name="contentRect">이동시킬 월드맵 컨테이너입니다.</param>
        public void Initialize(RectTransform viewportRect, RectTransform contentRect)
        {
            _viewportRect = viewportRect;
            _contentRect = contentRect != null ? contentRect : GetComponent<RectTransform>();
            dragSensitivity = Mathf.Max(0.01f, dragSensitivity);

            EnsureRaycastTarget();
            ClampContentPosition();
        }

        /// <summary>
        /// 포인터 드래그 시작 시 입력 모드와 포인터 종류를 검사하고 기준 위치를 저장합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = false;
            StopCenteringAnimation();
            if (!CanStartDrag(eventData))
            {
                return;
            }

            RectTransform parentRect = GetContentParentRect();
            if (parentRect == null)
            {
                return;
            }

            _isDragging = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out _lastPointerLocalPosition);
        }

        /// <summary>
        /// 포인터 이동량을 content의 부모 좌표계로 변환해 월드맵 컨테이너 위치에 반영합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _contentRect == null)
            {
                return;
            }

            RectTransform parentRect = GetContentParentRect();
            if (parentRect == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 currentPointerLocalPosition))
            {
                return;
            }

            Vector2 delta = (currentPointerLocalPosition - _lastPointerLocalPosition) * dragSensitivity;
            _lastPointerLocalPosition = currentPointerLocalPosition;

            _contentRect.localPosition += new Vector3(delta.x, delta.y, 0f);
            ClampContentPosition();
        }

        /// <summary>
        /// 드래그 종료 시 내부 드래그 상태를 정리하고 마지막으로 경계를 보정합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            ClampContentPosition();
        }

        /// <summary>
        /// content의 네 면이 viewport 안쪽으로 들어오지 않도록 현재 위치를 보정합니다.
        /// </summary>
        public void ClampContentPosition()
        {
            if (_viewportRect == null || _contentRect == null)
            {
                return;
            }

            Rect viewportRect = _viewportRect.rect;
            Bounds contentBounds = GetContentBoundsInViewport();
            Vector3 correctionInViewport = Vector3.zero;

            correctionInViewport.x = CalculateAxisCorrection(
                contentBounds.min.x,
                contentBounds.max.x,
                contentBounds.center.x,
                viewportRect.xMin,
                viewportRect.xMax,
                viewportRect.center.x,
                viewportRect.width);

            correctionInViewport.y = CalculateAxisCorrection(
                contentBounds.min.y,
                contentBounds.max.y,
                contentBounds.center.y,
                viewportRect.yMin,
                viewportRect.yMax,
                viewportRect.center.y,
                viewportRect.height);

            if (correctionInViewport == Vector3.zero)
            {
                return;
            }

            _contentRect.localPosition += ConvertViewportDeltaToContentParent(correctionInViewport);
        }

        /// <summary>
        /// 대상 RectTransform이 viewport 중앙에 오도록 월드맵 content 위치를 이동합니다.
        /// </summary>
        /// <param name="target">화면 중앙에 오도록 이동할 대상 RectTransform입니다.</param>
        /// <param name="options">중앙 이동 동작 옵션입니다.</param>
        /// <param name="onComplete">중앙 이동이 완료된 뒤 호출할 콜백입니다.</param>
        public void MoveTargetToViewportCenter(
            RectTransform target,
            WorldMapNodeCenteringOptions options,
            Action onComplete = null)
        {
            if (target == null || _viewportRect == null || _contentRect == null)
            {
                return;
            }

            if (options == null || !options.enabled)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 targetLocalPosition = CalculateCenteredContentLocalPosition(target);
            if (options.useAnimation)
            {
                StartCenteringAnimation(targetLocalPosition, options, onComplete);
                return;
            }

            StopCenteringAnimation();
            _contentRect.localPosition = targetLocalPosition;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 진행 중인 중앙 이동 애니메이션을 중단합니다.
        /// </summary>
        public void StopCenteringAnimation()
        {
            if (_centeringRoutine == null)
            {
                return;
            }

            StopCoroutine(_centeringRoutine);
            _centeringRoutine = null;
        }

        /// <summary>
        /// 현재 입력 모드에서 드래그를 시작할 수 있는 포인터인지 확인합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        /// <returns>드래그를 시작할 수 있으면 true입니다.</returns>
        private static bool CanStartDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return false;
            }

            bool mobileHudMode = WorldMapMobileHudModeResolver.IsMobileHudEnabled();
            if (mobileHudMode)
            {
                return IsTouchPointer(eventData);
            }

            return eventData.button == PointerEventData.InputButton.Left;
        }

        /// <summary>
        /// 포인터 ID가 터치 입력 범위인지 확인합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        /// <returns>터치 포인터이면 true입니다.</returns>
        private static bool IsTouchPointer(PointerEventData eventData)
        {
            return eventData.pointerId >= 0;
        }

        /// <summary>
        /// 포인터 ID가 마우스 입력 범위인지 확인합니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터입니다.</param>
        /// <returns>마우스 포인터이면 true입니다.</returns>
        private static bool IsMousePointer(PointerEventData eventData)
        {
            return eventData.pointerId < 0;
        }

        /// <summary>
        /// 대상 RectTransform이 viewport 중앙에 오기 위해 필요한 content 로컬 위치를 계산합니다.
        /// </summary>
        /// <param name="target">중앙에 맞출 대상 RectTransform입니다.</param>
        /// <returns>경계 보정까지 반영된 content 로컬 위치입니다.</returns>
        private Vector3 CalculateCenteredContentLocalPosition(RectTransform target)
        {
            Vector3 targetCenterInViewport = _viewportRect.InverseTransformPoint(GetRectWorldCenter(target));
            Vector3 viewportCenter = _viewportRect.rect.center;
            Vector3 deltaInViewport = viewportCenter - targetCenterInViewport;
            Vector3 desiredLocalPosition = _contentRect.localPosition + ConvertViewportDeltaToContentParent(deltaInViewport);
            return GetClampedContentLocalPosition(desiredLocalPosition);
        }

        /// <summary>
        /// 지정한 RectTransform의 월드 좌표 기준 중앙점을 반환합니다.
        /// </summary>
        /// <param name="rectTransform">중앙점을 계산할 RectTransform입니다.</param>
        /// <returns>월드 좌표 기준 중앙점입니다.</returns>
        private static Vector3 GetRectWorldCenter(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        /// <summary>
        /// 지정한 content 로컬 위치에 viewport 경계 보정을 적용한 결과를 계산합니다.
        /// </summary>
        /// <param name="localPosition">보정할 content 로컬 위치입니다.</param>
        /// <returns>viewport 경계를 침범하지 않는 content 로컬 위치입니다.</returns>
        private Vector3 GetClampedContentLocalPosition(Vector3 localPosition)
        {
            Vector3 originLocalPosition = _contentRect.localPosition;
            _contentRect.localPosition = localPosition;

            Rect viewportRect = _viewportRect.rect;
            Bounds contentBounds = GetContentBoundsInViewport();
            Vector3 correctionInViewport = Vector3.zero;
            correctionInViewport.x = CalculateAxisCorrection(
                contentBounds.min.x,
                contentBounds.max.x,
                contentBounds.center.x,
                viewportRect.xMin,
                viewportRect.xMax,
                viewportRect.center.x,
                viewportRect.width);
            correctionInViewport.y = CalculateAxisCorrection(
                contentBounds.min.y,
                contentBounds.max.y,
                contentBounds.center.y,
                viewportRect.yMin,
                viewportRect.yMax,
                viewportRect.center.y,
                viewportRect.height);

            Vector3 result = localPosition + ConvertViewportDeltaToContentParent(correctionInViewport);
            _contentRect.localPosition = originLocalPosition;
            return result;
        }

        /// <summary>
        /// 중앙 이동 애니메이션을 시작합니다.
        /// </summary>
        /// <param name="targetLocalPosition">이동할 content 로컬 위치입니다.</param>
        /// <param name="options">중앙 이동 동작 옵션입니다.</param>
        /// <param name="onComplete">이동 완료 후 호출할 콜백입니다.</param>
        private void StartCenteringAnimation(
            Vector3 targetLocalPosition,
            WorldMapNodeCenteringOptions options,
            Action onComplete)
        {
            StopCenteringAnimation();

            float duration = CalculateCenteringDuration(_contentRect.localPosition, targetLocalPosition, options);
            if (duration <= 0f)
            {
                _contentRect.localPosition = targetLocalPosition;
                onComplete?.Invoke();
                return;
            }

            _centeringRoutine = StartCoroutine(MoveContentToCenterRoutine(targetLocalPosition, duration, options.easeType, onComplete));
        }

        /// <summary>
        /// 현재 위치와 목표 위치 사이의 거리로 중앙 이동 시간을 계산합니다.
        /// </summary>
        /// <param name="startLocalPosition">시작 content 로컬 위치입니다.</param>
        /// <param name="targetLocalPosition">목표 content 로컬 위치입니다.</param>
        /// <param name="options">중앙 이동 동작 옵션입니다.</param>
        /// <returns>거리와 속도 설정을 반영한 이동 시간입니다.</returns>
        private static float CalculateCenteringDuration(
            Vector3 startLocalPosition,
            Vector3 targetLocalPosition,
            WorldMapNodeCenteringOptions options)
        {
            float distance = Vector3.Distance(startLocalPosition, targetLocalPosition);
            if (distance <= 0.01f)
            {
                return 0f;
            }

            float maxDistanceForSpeed = Mathf.Max(0.01f, options.maxDistanceForSpeed);
            float distanceRatio = Mathf.Clamp01(distance / maxDistanceForSpeed);
            float speed = Mathf.Lerp(
                Mathf.Max(0.01f, options.minSpeed),
                Mathf.Max(0.01f, options.maxSpeed),
                distanceRatio);
            float minDuration = Mathf.Max(0f, options.minDuration);
            float maxDuration = Mathf.Max(minDuration, options.maxDuration);
            return Mathf.Clamp(distance / speed, minDuration, maxDuration);
        }

        /// <summary>
        /// content를 목표 로컬 위치까지 Easing을 적용해 이동합니다.
        /// </summary>
        /// <param name="targetLocalPosition">목표 content 로컬 위치입니다.</param>
        /// <param name="duration">이동 시간입니다.</param>
        /// <param name="easeType">이동에 적용할 Easing 타입입니다.</param>
        /// <param name="onComplete">이동 완료 후 호출할 콜백입니다.</param>
        /// <returns>코루틴 실행 상태입니다.</returns>
        private IEnumerator MoveContentToCenterRoutine(
            Vector3 targetLocalPosition,
            float duration,
            Easing.EaseType easeType,
            Action onComplete)
        {
            Vector3 startLocalPosition = _contentRect.localPosition;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
                float easedTime = Mathf.Clamp01(Easing.Apply(normalizedTime, easeType));
                _contentRect.localPosition = Vector3.LerpUnclamped(startLocalPosition, targetLocalPosition, easedTime);
                yield return null;
            }

            _contentRect.localPosition = targetLocalPosition;
            _centeringRoutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// content가 포인터 이벤트를 받을 수 있도록 투명 Image를 보장합니다.
        /// </summary>
        private void EnsureRaycastTarget()
        {
            if (_contentRect == null)
            {
                return;
            }

            Image image = _contentRect.GetComponent<Image>();
            if (image == null)
            {
                image = _contentRect.gameObject.AddComponent<Image>();
                image.color = Color.clear;
            }

            image.raycastTarget = true;
        }

        /// <summary>
        /// content의 부모 RectTransform을 반환합니다.
        /// </summary>
        /// <returns>content 부모 RectTransform입니다.</returns>
        private RectTransform GetContentParentRect()
        {
            return _contentRect != null ? _contentRect.parent as RectTransform : null;
        }

        /// <summary>
        /// content의 월드 코너를 viewport 로컬 좌표계 기준 Bounds로 변환합니다.
        /// </summary>
        /// <returns>viewport 기준 content Bounds입니다.</returns>
        private Bounds GetContentBoundsInViewport()
        {
            Vector3[] corners = new Vector3[4];
            _contentRect.GetWorldCorners(corners);

            Vector3 first = _viewportRect.InverseTransformPoint(corners[0]);
            Bounds bounds = new Bounds(first, Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
            {
                bounds.Encapsulate(_viewportRect.InverseTransformPoint(corners[i]));
            }

            return bounds;
        }

        /// <summary>
        /// 한 축에서 content 경계가 viewport 경계를 침범하지 않도록 필요한 보정량을 계산합니다.
        /// </summary>
        /// <param name="contentMin">content 최소 좌표입니다.</param>
        /// <param name="contentMax">content 최대 좌표입니다.</param>
        /// <param name="contentCenter">content 중앙 좌표입니다.</param>
        /// <param name="viewportMin">viewport 최소 좌표입니다.</param>
        /// <param name="viewportMax">viewport 최대 좌표입니다.</param>
        /// <param name="viewportCenter">viewport 중앙 좌표입니다.</param>
        /// <param name="viewportSize">viewport 크기입니다.</param>
        /// <returns>해당 축의 보정량입니다.</returns>
        private static float CalculateAxisCorrection(
            float contentMin,
            float contentMax,
            float contentCenter,
            float viewportMin,
            float viewportMax,
            float viewportCenter,
            float viewportSize)
        {
            float contentSize = contentMax - contentMin;
            if (contentSize <= viewportSize)
            {
                return viewportCenter - contentCenter;
            }

            if (contentMin > viewportMin)
            {
                return viewportMin - contentMin;
            }

            if (contentMax < viewportMax)
            {
                return viewportMax - contentMax;
            }

            return 0f;
        }

        /// <summary>
        /// viewport 로컬 좌표계의 보정 벡터를 content 부모 로컬 좌표계 벡터로 변환합니다.
        /// </summary>
        /// <param name="viewportDelta">viewport 로컬 좌표계의 보정 벡터입니다.</param>
        /// <returns>content 부모 로컬 좌표계의 보정 벡터입니다.</returns>
        private Vector3 ConvertViewportDeltaToContentParent(Vector3 viewportDelta)
        {
            RectTransform parentRect = GetContentParentRect();
            if (parentRect == null)
            {
                return viewportDelta;
            }

            Vector3 worldStart = _viewportRect.TransformPoint(Vector3.zero);
            Vector3 worldEnd = _viewportRect.TransformPoint(viewportDelta);
            return parentRect.InverseTransformPoint(worldEnd) - parentRect.InverseTransformPoint(worldStart);
        }
    }

    /// <summary>
    /// Control 패키지에 있는 모바일 HUD 설정을 Core에서 직접 참조하지 않고 읽기 위한 도우미입니다.
    /// </summary>
    internal static class WorldMapMobileHudModeResolver
    {
        private const string LoaderTypeName = "GGemCo2DControl.AddressableLoaderSettingsControl, GGemCo2DControl";
        private const string InstancePropertyName = "Instance";
        private const string MobileHudSettingsFieldName = "mobileHudSettings";
        private const string EnableMobileHudFieldName = "enableMobileHud";

        private static Type _loaderType;
        private static PropertyInfo _instanceProperty;
        private static FieldInfo _mobileHudSettingsField;
        private static FieldInfo _enableMobileHudField;
        private static bool _reflectionInitialized;

        /// <summary>
        /// GGemCoMobileHudSettings.enableMobileHud 값을 조회합니다.
        /// Control 패키지나 설정 인스턴스가 없으면 false를 반환합니다.
        /// </summary>
        /// <returns>모바일 HUD가 활성화되어 있으면 true입니다.</returns>
        public static bool IsMobileHudEnabled()
        {
            EnsureReflectionCache();
            if (_loaderType == null || _instanceProperty == null || _mobileHudSettingsField == null)
            {
                return false;
            }

            object loaderInstance = _instanceProperty.GetValue(null);
            object mobileHudSettings = loaderInstance != null
                ? _mobileHudSettingsField.GetValue(loaderInstance)
                : null;

            if (mobileHudSettings == null)
            {
                return false;
            }

            _enableMobileHudField ??= mobileHudSettings.GetType().GetField(EnableMobileHudFieldName, BindingFlags.Instance | BindingFlags.Public);
            return _enableMobileHudField != null && _enableMobileHudField.GetValue(mobileHudSettings) is bool enabled && enabled;
        }

        /// <summary>
        /// 모바일 HUD 설정 조회에 필요한 리플렉션 정보를 한 번만 캐싱합니다.
        /// </summary>
        private static void EnsureReflectionCache()
        {
            if (_reflectionInitialized)
            {
                return;
            }

            _reflectionInitialized = true;
            _loaderType = Type.GetType(LoaderTypeName);
            if (_loaderType == null)
            {
                return;
            }

            _instanceProperty = _loaderType.GetProperty(InstancePropertyName, BindingFlags.Static | BindingFlags.Public);
            _mobileHudSettingsField = _loaderType.GetField(MobileHudSettingsFieldName, BindingFlags.Instance | BindingFlags.Public);
        }
    }
}
