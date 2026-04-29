using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 노드 두 개를 UI Image 선분으로 연결해 표시합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class WorldMapLineRenderer : MonoBehaviour
    {
        private const float DefaultThickness = 6f;

        private RectTransform _rectTransform;
        private Image _image;
        private RectTransform _from;
        private RectTransform _to;
        private WorldMapEdgeDefinition _edgeDefinition;
        private Color _normalColor;
        private Color _highlightColor;
        private float _thickness = DefaultThickness;

        /// <summary>
        /// 연결선 UI를 초기화하고 두 노드 RectTransform을 연결 대상으로 설정합니다.
        /// </summary>
        /// <param name="edgeDefinition">표시할 월드맵 연결선 정의입니다.</param>
        /// <param name="from">출발 노드 RectTransform입니다.</param>
        /// <param name="to">도착 노드 RectTransform입니다.</param>
        /// <param name="normalColor">일반 상태 색상입니다.</param>
        /// <param name="highlightColor">강조 상태 색상입니다.</param>
        /// <param name="thickness">선 두께입니다.</param>
        public void Initialize(
            WorldMapEdgeDefinition edgeDefinition,
            RectTransform from,
            RectTransform to,
            Color normalColor,
            Color highlightColor,
            float thickness = DefaultThickness)
        {
            _edgeDefinition = edgeDefinition;
            _from = from;
            _to = to;
            _normalColor = normalColor;
            _highlightColor = highlightColor;
            _thickness = Mathf.Max(1f, thickness);

            EnsureComponents();
            _image.raycastTarget = false;
            _image.color = _normalColor;
            Refresh();
        }

        /// <summary>
        /// 연결선이 지정한 노드 ID와 연결되어 있는지 확인합니다.
        /// </summary>
        /// <param name="nodeId">확인할 노드 ID입니다.</param>
        /// <returns>출발 또는 도착 노드가 일치하면 true입니다.</returns>
        public bool ContainsNode(string nodeId)
        {
            if (_edgeDefinition == null || string.IsNullOrWhiteSpace(nodeId))
            {
                return false;
            }

            return _edgeDefinition.FromNodeId == nodeId || _edgeDefinition.ToNodeId == nodeId;
        }

        /// <summary>
        /// 연결선 강조 표시 여부를 변경합니다.
        /// </summary>
        /// <param name="highlighted">강조 표시 여부입니다.</param>
        public void SetHighlighted(bool highlighted)
        {
            EnsureComponents();
            _image.color = highlighted ? _highlightColor : _normalColor;
        }

        /// <summary>
        /// 매 프레임 노드 위치를 기준으로 선분 위치와 회전을 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            Refresh();
        }

        /// <summary>
        /// 연결된 노드 RectTransform의 현재 위치를 기준으로 선분을 갱신합니다.
        /// </summary>
        public void Refresh()
        {
            if (_from == null || _to == null)
            {
                return;
            }

            EnsureComponents();

            Vector3 fromPosition = _from.localPosition;
            Vector3 toPosition = _to.localPosition;
            Vector3 direction = toPosition - fromPosition;
            float distance = direction.magnitude;

            _rectTransform.localPosition = fromPosition + direction * 0.5f;
            _rectTransform.sizeDelta = new Vector2(distance, _thickness);
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 연결선 표시를 위해 필요한 RectTransform과 Image 컴포넌트를 보장합니다.
        /// </summary>
        private void EnsureComponents()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.zero;
                _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }
    }
}
