using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 사용하는 UI 패널 전용 Presenter입니다.
    /// panelId를 기준으로 패널을 생성, 재사용, 표시 제어 및 제거합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CutsceneUiPanelPresenter : MonoBehaviour
    {
        /// <summary>
        /// 개별 UI 패널 인스턴스와 관련 컴포넌트 참조를 묶어 관리합니다.
        /// </summary>
        private sealed class PanelHandle
        {
            public string Id;
            public GameObject GameObject;
            public RectTransform RectTransform;
            public CanvasGroup CanvasGroup;
            public Image Image;
            public Canvas Canvas;
            public UiPanelLayoutMode LayoutMode;
            public Vector2 StretchOffsetMin;
            public Vector2 StretchOffsetMax;
        }

        private readonly Dictionary<string, PanelHandle> _panels = new();
        private Canvas _canvas;
        private RectTransform _rootRect;
        private ScreenFadeRenderMode _currentRenderMode = ScreenFadeRenderMode.OverlayUi;

        /// <summary>
        /// Presenter에 필요한 Canvas를 보장하고 기존 표시 상태를 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            EnsureCanvas();
            ResetPresentation();
        }

        /// <summary>
        /// 전달된 데이터에 따라 Presenter 루트 Canvas의 렌더링 방식을 적용합니다.
        /// </summary>
        /// <param name="data">렌더링 모드, 정렬 순서, 카메라 사용 여부 등을 포함한 패널 설정 데이터입니다.</param>
        /// <param name="sceneGame">메인 카메라 참조를 가져오기 위한 현재 씬 컨텍스트입니다.</param>
        public void ApplyRenderSettings(UiPanelData data, SceneGame sceneGame)
        {
            EnsureCanvas();

            var resolved = data ?? new UiPanelData();
            _currentRenderMode = resolved.renderMode;
            var mainCamera = sceneGame != null ? sceneGame.mainCamera : Camera.main;
            string sortingLayerName = ResolveSortingLayerName(resolved.sortingLayerName);

            switch (_currentRenderMode)
            {
                case ScreenFadeRenderMode.ScreenSpaceCamera:
                    _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _canvas.worldCamera = mainCamera;
                    _canvas.planeDistance = Mathf.Max(0.01f, resolved.planeDistance);
                    break;

                case ScreenFadeRenderMode.OverlayUi:
                default:
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.worldCamera = null;
                    _canvas.planeDistance = Mathf.Max(0.01f, resolved.planeDistance);
                    break;
            }

            _canvas.overrideSorting = true;
            _canvas.sortingLayerName = sortingLayerName;
            _canvas.sortingOrder = resolved.useIndependentCanvasSorting ? 0 : resolved.orderInLayer;

            if (_rootRect != null)
            {
                _rootRect.anchorMin = Vector2.zero;
                _rootRect.anchorMax = Vector2.one;
                _rootRect.offsetMin = Vector2.zero;
                _rootRect.offsetMax = Vector2.zero;
                _rootRect.anchoredPosition3D = Vector3.zero;
                _rootRect.localScale = Vector3.one;
                _rootRect.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 현재 생성된 모든 패널을 제거하고 내부 캐시를 비웁니다.
        /// </summary>
        public void ResetPresentation()
        {
            if (_panels.Count <= 0)
            {
                return;
            }

            foreach (var pair in _panels)
            {
                if (pair.Value?.GameObject != null)
                {
                    Destroy(pair.Value.GameObject);
                }
            }

            _panels.Clear();
        }

        /// <summary>
        /// 지정한 ID의 패널이 현재 존재하는지 확인합니다.
        /// </summary>
        /// <param name="panelId">확인할 패널 식별자입니다.</param>
        /// <returns>해당 패널이 존재하면 <see langword="true"/>, 없으면 <see langword="false"/>를 반환합니다.</returns>
        public bool HasPanel(string panelId)
        {
            return TryGetHandle(panelId, out _);
        }

        /// <summary>
        /// 지정한 ID의 패널이 없으면 생성하고, 이미 있으면 기존 패널을 유지합니다.
        /// </summary>
        /// <param name="panelId">보장할 패널 식별자입니다.</param>
        /// <returns>패널이 존재하거나 새로 생성되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool EnsurePanel(string panelId)
        {
            return GetOrCreateHandle(panelId, true) != null;
        }

        /// <summary>
        /// 지정한 패널의 레이아웃, 시각 옵션 및 정렬 설정을 적용합니다.
        /// </summary>
        /// <param name="panelId">설정할 패널 식별자입니다.</param>
        /// <param name="data">패널 생성 여부와 레이아웃/표시 옵션을 포함한 설정 데이터입니다.</param>
        public void ConfigurePanel(string panelId, UiPanelData data)
        {
            var handle = GetOrCreateHandle(panelId, data == null || data.createIfMissing);
            if (handle == null)
            {
                return;
            }

            ApplyLayout(handle, data);
            ApplyVisualOptions(handle, data);
            ApplySorting(handle, data);
        }

        /// <summary>
        /// 패널의 위치, 크기, 색상, 투명도와 활성 상태를 적용합니다.
        /// </summary>
        /// <param name="panelId">상태를 적용할 패널 식별자입니다.</param>
        /// <param name="anchoredPosition">패널의 Anchored Position 값입니다.</param>
        /// <param name="sizeDelta">패널의 SizeDelta 값입니다.</param>
        /// <param name="color">패널 이미지의 표시 색상입니다.</param>
        /// <param name="alpha">패널의 알파값입니다. 0~1 범위로 보정됩니다.</param>
        /// <remarks>
        /// Stretch가 활성화된 축은 anchoredPosition과 sizeDelta 대신 ConfigurePanel에서 저장한 부모 경계 여백을 사용합니다.
        /// </remarks>
        public void ApplyState(string panelId, Vec2 anchoredPosition, Vec2 sizeDelta, Color color, float alpha)
        {
            if (!TryGetHandle(panelId, out var handle))
            {
                return;
            }

            handle.RectTransform.anchoredPosition = anchoredPosition.ToVector2();
            handle.RectTransform.sizeDelta = sizeDelta.ToVector2();
            ApplyStretchOffsets(handle);
            handle.Image.color = new Color(color.r, color.g, color.b, 1f);
            handle.CanvasGroup.alpha = Mathf.Clamp01(alpha);
            handle.GameObject.SetActive(true);
        }

        /// <summary>
        /// 지정한 패널의 활성화 여부를 변경합니다.
        /// </summary>
        /// <param name="panelId">표시 상태를 변경할 패널 식별자입니다.</param>
        /// <param name="visible"><see langword="true"/>이면 패널을 활성화하고, 아니면 비활성화합니다.</param>
        public void SetPanelVisible(string panelId, bool visible)
        {
            if (!TryGetHandle(panelId, out var handle))
            {
                return;
            }

            handle.GameObject.SetActive(visible);
        }

        /// <summary>
        /// 지정한 패널을 파괴하고 내부 관리 목록에서 제거합니다.
        /// </summary>
        /// <param name="panelId">제거할 패널 식별자입니다.</param>
        public void DestroyPanel(string panelId)
        {
            if (!TryGetHandle(panelId, out var handle))
            {
                return;
            }

            if (handle.GameObject != null)
            {
                Destroy(handle.GameObject);
            }

            _panels.Remove(panelId);
        }

        /// <summary>
        /// 지정한 ID의 패널 핸들을 반환하거나, 필요 시 새로 생성합니다.
        /// </summary>
        /// <param name="panelId">조회 또는 생성할 패널 식별자입니다.</param>
        /// <param name="createIfMissing">패널이 없을 때 새로 생성할지 여부입니다.</param>
        /// <returns>기존 또는 새로 생성된 패널 핸들이며, 생성하지 않도록 설정된 경우 없으면 <see langword="null"/>입니다.</returns>
        private PanelHandle GetOrCreateHandle(string panelId, bool createIfMissing)
        {
            if (TryGetHandle(panelId, out var existing))
            {
                return existing;
            }

            if (!createIfMissing)
            {
                return null;
            }

            panelId = NormalizePanelId(panelId);
            var go = new GameObject($"UiPanel_{panelId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400f, 200f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            var canvasGroup = go.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(0f, 0f, 0f, 1f);

            var handle = new PanelHandle
            {
                Id = panelId,
                GameObject = go,
                RectTransform = rect,
                CanvasGroup = canvasGroup,
                Image = image,
            };

            _panels[panelId] = handle;
            go.SetActive(false);
            return handle;
        }

        /// <summary>
        /// 지정한 ID에 해당하는 패널 핸들을 조회합니다.
        /// </summary>
        /// <param name="panelId">조회할 패널 식별자입니다.</param>
        /// <param name="handle">조회된 패널 핸들입니다.</param>
        /// <returns>패널을 찾았으면 <see langword="true"/>, 없으면 <see langword="false"/>를 반환합니다.</returns>
        private bool TryGetHandle(string panelId, out PanelHandle handle)
        {
            return _panels.TryGetValue(NormalizePanelId(panelId), out handle);
        }

        /// <summary>
        /// Presenter 루트에 필요한 Canvas와 RectTransform 컴포넌트가 존재하도록 보장합니다.
        /// </summary>
        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }

            _rootRect = GetComponent<RectTransform>();
            if (_rootRect == null)
            {
                _rootRect = gameObject.AddComponent<RectTransform>();
            }
        }

        /// <summary>
        /// 패널의 레이아웃 모드, 앵커, 피벗, Stretch 여백 및 형제 인덱스를 적용합니다.
        /// </summary>
        /// <param name="handle">레이아웃을 적용할 패널 핸들입니다.</param>
        /// <param name="data">레이아웃 설정 데이터입니다.</param>
        private static void ApplyLayout(PanelHandle handle, UiPanelData data)
        {
            if (handle == null || data == null)
            {
                return;
            }

            Vector2 anchorMin = data.anchorMin.ToVector2();
            Vector2 anchorMax = data.anchorMax.ToVector2();
            Vector2 pivot = data.pivot.ToVector2();

            // Stretch하지 않는 축은 pivot 위치에 고정하여 기존 위치와 크기 보간이 그대로 동작하게 합니다.
            switch (data.layoutMode)
            {
                case UiPanelLayoutMode.StretchHorizontal:
                    anchorMin = new Vector2(0f, pivot.y);
                    anchorMax = new Vector2(1f, pivot.y);
                    break;

                case UiPanelLayoutMode.StretchVertical:
                    anchorMin = new Vector2(pivot.x, 0f);
                    anchorMax = new Vector2(pivot.x, 1f);
                    break;

                case UiPanelLayoutMode.StretchBoth:
                    anchorMin = Vector2.zero;
                    anchorMax = Vector2.one;
                    break;
            }

            handle.LayoutMode = data.layoutMode;
            handle.StretchOffsetMin = data.stretchOffsetMin.ToVector2();
            handle.StretchOffsetMax = data.stretchOffsetMax.ToVector2();
            handle.RectTransform.anchorMin = anchorMin;
            handle.RectTransform.anchorMax = anchorMax;
            handle.RectTransform.pivot = pivot;

            if (data.siblingIndex >= 0)
            {
                handle.RectTransform.SetSiblingIndex(data.siblingIndex);
            }
        }

        /// <summary>
        /// Stretch가 활성화된 축에 부모 경계 기준 안쪽 여백을 적용합니다.
        /// </summary>
        /// <param name="handle">레이아웃 상태와 대상 RectTransform을 보관한 패널 핸들입니다.</param>
        /// <remarks>
        /// 비-Stretch 축에는 앞서 적용한 anchoredPosition과 sizeDelta를 그대로 유지합니다.
        /// offsetMax는 Unity RectTransform 규칙에 따라 오른쪽과 위쪽 안쪽 여백을 음수로 적용합니다.
        /// </remarks>
        private static void ApplyStretchOffsets(PanelHandle handle)
        {
            if (handle?.RectTransform == null)
            {
                return;
            }

            RectTransform rectTransform = handle.RectTransform;
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;

            if (IsHorizontalStretch(handle.LayoutMode))
            {
                offsetMin.x = handle.StretchOffsetMin.x;
                offsetMax.x = -handle.StretchOffsetMax.x;
            }

            if (IsVerticalStretch(handle.LayoutMode))
            {
                offsetMin.y = handle.StretchOffsetMin.y;
                offsetMax.y = -handle.StretchOffsetMax.y;
            }

            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        /// <summary>
        /// 지정한 레이아웃 모드가 가로 Stretch를 사용하는지 확인합니다.
        /// </summary>
        /// <param name="layoutMode">확인할 UI Panel 레이아웃 모드입니다.</param>
        /// <returns>가로축을 부모 너비에 맞춰 늘리면 <see langword="true"/>입니다.</returns>
        private static bool IsHorizontalStretch(UiPanelLayoutMode layoutMode)
        {
            return layoutMode == UiPanelLayoutMode.StretchHorizontal ||
                   layoutMode == UiPanelLayoutMode.StretchBoth;
        }

        /// <summary>
        /// 지정한 레이아웃 모드가 세로 Stretch를 사용하는지 확인합니다.
        /// </summary>
        /// <param name="layoutMode">확인할 UI Panel 레이아웃 모드입니다.</param>
        /// <returns>세로축을 부모 높이에 맞춰 늘리면 <see langword="true"/>입니다.</returns>
        private static bool IsVerticalStretch(UiPanelLayoutMode layoutMode)
        {
            return layoutMode == UiPanelLayoutMode.StretchVertical ||
                   layoutMode == UiPanelLayoutMode.StretchBoth;
        }

        /// <summary>
        /// 패널의 레이캐스트 및 상호작용 관련 시각 옵션을 적용합니다.
        /// </summary>
        /// <param name="handle">옵션을 적용할 패널 핸들입니다.</param>
        /// <param name="data">시각 및 입력 처리 설정 데이터입니다.</param>
        private static void ApplyVisualOptions(PanelHandle handle, UiPanelData data)
        {
            if (handle == null || data == null)
            {
                return;
            }

            handle.Image.raycastTarget = data.raycastTarget;
            handle.CanvasGroup.blocksRaycasts = data.raycastTarget;
            handle.CanvasGroup.interactable = data.raycastTarget;
        }

        /// <summary>
        /// 패널별 독립 Canvas 정렬 설정을 적용하거나 제거합니다.
        /// </summary>
        /// <param name="handle">정렬 설정을 적용할 패널 핸들입니다.</param>
        /// <param name="data">독립 정렬 사용 여부와 정렬 레이어 정보를 포함한 데이터입니다.</param>
        private static void ApplySorting(PanelHandle handle, UiPanelData data)
        {
            if (handle == null)
            {
                return;
            }

            if (data == null || !data.useIndependentCanvasSorting)
            {
                if (handle.Canvas != null)
                {
                    Object.Destroy(handle.Canvas);
                    handle.Canvas = null;
                }

                return;
            }

            handle.Canvas ??= handle.GameObject.GetComponent<Canvas>() ?? handle.GameObject.AddComponent<Canvas>();
            handle.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            handle.Canvas.worldCamera = null;
            handle.Canvas.planeDistance = Mathf.Max(0.01f, data.planeDistance);
            handle.Canvas.overrideSorting = true;
            handle.Canvas.sortingLayerName = ResolveSortingLayerName(data.sortingLayerName);
            handle.Canvas.sortingOrder = data.orderInLayer;
        }

        /// <summary>
        /// 패널 식별자를 내부 관리용 기본 형식으로 정규화합니다.
        /// </summary>
        /// <param name="panelId">정규화할 패널 식별자입니다.</param>
        /// <returns>공백이거나 비어 있으면 기본값 "Panel"을, 아니면 Trim 처리된 식별자를 반환합니다.</returns>
        private static string NormalizePanelId(string panelId)
        {
            return string.IsNullOrWhiteSpace(panelId) ? "Panel" : panelId.Trim();
        }

        /// <summary>
        /// 사용할 정렬 레이어 이름을 결정합니다.
        /// </summary>
        /// <param name="sortingLayerName">요청된 정렬 레이어 이름입니다.</param>
        /// <returns>유효한 값이 있으면 해당 이름을, 없으면 UI 기본 정렬 레이어 이름을 반환합니다.</returns>
        private static string ResolveSortingLayerName(string sortingLayerName)
        {
            return string.IsNullOrWhiteSpace(sortingLayerName)
                ? nameof(ConfigSortingLayer.Keys.UI)
                : sortingLayerName;
        }
    }
}
