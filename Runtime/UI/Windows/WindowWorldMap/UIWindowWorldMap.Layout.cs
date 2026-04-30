using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowWorldMap의 배치와 레이어 책임을 분리한 partial 클래스입니다.
    /// </summary>
    public partial class UIWindowWorldMap
    {
        /// <summary>
        /// 월드맵 컨테이너 크기가 바뀌면 정규화 좌표 기반 노드 위치를 다시 계산합니다.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            RepositionWorldMapNodes();
            RefreshEdgeLines();
            ClampWorldMapDragPosition();
        }

        /// <summary>
        /// AddressableLoaderWorldMap에 캐싱된 기본 월드맵 정의를 조회합니다.
        /// </summary>
        /// <returns>기본 월드맵 정의입니다. 로드되지 않았으면 null입니다.</returns>
        private static WorldMapDefinition ResolveDefaultWorldMapDefinition()
        {
            if (AddressableLoaderWorldMap.Instance == null)
            {
                return null;
            }

            return AddressableLoaderWorldMap.Instance.TryGetDefaultWorldMap(out WorldMapDefinition definition)
                ? definition
                : null;
        }

        /// <summary>
        /// 월드맵 정의의 노드 개수를 안전하게 반환합니다.
        /// </summary>
        /// <param name="definition">노드 개수를 확인할 월드맵 정의입니다.</param>
        /// <returns>노드 개수입니다.</returns>
        private static int GetWorldMapNodeCount(WorldMapDefinition definition)
        {
            return definition != null && definition.Nodes != null ? definition.Nodes.Count : 0;
        }

        /// <summary>
        /// AddressableLoaderWorldMap에 캐싱된 월드맵 배경 Sprite를 배경 Image에 적용합니다.
        /// </summary>
        private void ApplyBackgroundSprite()
        {
            if (_worldMapDefinition == null || string.IsNullOrWhiteSpace(_worldMapDefinition.BackgroundAddress))
            {
                return;
            }

            Image targetImage = GetBackgroundImage();
            if (targetImage == null)
            {
                return;
            }

            string address = _worldMapDefinition.BackgroundAddress;
            _requestedBackgroundAddress = address;
            if (AddressableLoaderWorldMap.Instance == null ||
                !AddressableLoaderWorldMap.Instance.TryGetBackgroundSprite(address, out Sprite backgroundSprite) ||
                backgroundSprite == null ||
                _requestedBackgroundAddress != address)
            {
                return;
            }

            targetImage.sprite = backgroundSprite;
            if (targetImage.color.a <= 0f)
            {
                targetImage.color = Color.white;
            }

            targetImage.enabled = true;
        }

        /// <summary>
        /// 배경을 표시할 Image를 반환합니다.
        /// 명시 연결이 없으면 containerWorldMap에 붙은 Image를 재사용합니다.
        /// </summary>
        /// <returns>배경 Image입니다. 찾지 못하면 null입니다.</returns>
        private Image GetBackgroundImage()
        {
            if (imageBackground != null)
            {
                return imageBackground;
            }

            if (containerWorldMap == null)
            {
                return null;
            }

            containerWorldMap.TryGetComponent(out imageBackground);
            return imageBackground;
        }

        /// <summary>
        /// 월드맵 컨테이너에 드래그 컨트롤러를 보장하고 viewport/content 참조를 연결합니다.
        /// </summary>
        private void EnsureWorldMapDragController()
        {
            if (containerWorldMap == null)
            {
                return;
            }

            RectTransform contentRect = containerWorldMap.GetComponent<RectTransform>();
            if (contentRect == null)
            {
                contentRect = containerWorldMap.AddComponent<RectTransform>();
            }

            if (viewportWorldMap == null)
            {
                viewportWorldMap = contentRect.parent as RectTransform;
            }

            _dragController = containerWorldMap.GetComponent<WorldMapDragController>();
            if (_dragController == null)
            {
                _dragController = containerWorldMap.AddComponent<WorldMapDragController>();
            }

            _dragController.Initialize(viewportWorldMap, contentRect);
        }

        /// <summary>
        /// 월드맵 드래그 위치가 viewport 경계를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void ClampWorldMapDragPosition()
        {
            _dragController?.ClampContentPosition();
        }

        /// <summary>
        /// 월드맵 노드와 연결선 레이어를 보장하고 자유 배치를 위해 LayoutGroup을 비활성화합니다.
        /// </summary>
        private void EnsureWorldMapLayers()
        {
            if (containerWorldMap == null)
            {
                return;
            }

            LayoutGroup layoutGroup = containerWorldMap.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }

            RectTransform root = containerWorldMap.GetComponent<RectTransform>();
            if (root == null)
            {
                root = containerWorldMap.AddComponent<RectTransform>();
            }

            containerLineLayer = EnsureLayer(root, containerLineLayer, "LineLayer");
            containerNodeLayer = EnsureLayer(root, containerNodeLayer, "NodeLayer");
            containerLineLayer.SetAsFirstSibling();
            containerNodeLayer.SetAsLastSibling();
        }

        /// <summary>
        /// 지정한 이름의 월드맵 레이어 RectTransform을 찾거나 생성합니다.
        /// </summary>
        /// <param name="root">레이어를 붙일 루트 RectTransform입니다.</param>
        /// <param name="current">이미 연결된 레이어 RectTransform입니다.</param>
        /// <param name="layerName">찾거나 생성할 레이어 이름입니다.</param>
        /// <returns>보장된 레이어 RectTransform입니다.</returns>
        private static RectTransform EnsureLayer(RectTransform root, RectTransform current, string layerName)
        {
            if (current != null)
            {
                return current;
            }

            Transform found = root.Find(layerName);
            if (found != null && found.TryGetComponent(out RectTransform foundRect))
            {
                return foundRect;
            }

            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.SetParent(root, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
            layerRect.pivot = new Vector2(0.5f, 0.5f);
            return layerRect;
        }

        /// <summary>
        /// 노드 레이어 RectTransform을 반환합니다.
        /// </summary>
        /// <returns>노드 레이어 RectTransform입니다.</returns>
        private RectTransform GetNodeLayerRect()
        {
            EnsureWorldMapLayers();
            return containerNodeLayer != null
                ? containerNodeLayer
                : containerWorldMap != null
                    ? containerWorldMap.GetComponent<RectTransform>()
                    : null;
        }
    }
}
