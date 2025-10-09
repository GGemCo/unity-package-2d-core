using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 마우스 클릭한 셀에 물주기(TileAction.Water) 적용
    /// - 빈 셀인 경우, 지정한 '기본 타일'로 생성하고 초기 상태(Dry 등) 기록 후 전이
    /// - UI 위 클릭 무시 옵션
    /// - Legacy Input / New Input System 모두 지원
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClickToWater : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("물주기 적용 대상 Tilemap (보통 Ground)")]
        private Tilemap _targetTilemap;

        [Tooltip("타일 전이 처리 시스템 (같은 오브젝트 또는 동일 Grid 내)")]
        private TileActionSystem _tileActionSystem;

        [Tooltip("좌표 변환용 카메라 (미지정 시 Camera.main)")]
        private Camera _worldCamera;

        [Header("Click Options")]
        [Tooltip("UI 위에서의 클릭은 무시")]
        [SerializeField] private bool ignoreWhenPointerOverUI = true;

        [Tooltip("왼쪽 클릭으로만 처리")]
        [SerializeField] private bool useLeftClickOnly = true;

        [Header("Empty Cell Handling")]
        [Tooltip("클릭한 셀이 비어있으면 신규로 생성합니다.")]
        [SerializeField] private bool createWhenEmpty = true;

        [Tooltip("빈 셀 생성 시 사용할 기본 타일(예: DryRuleTile)")]
        [SerializeField] private TileBase baseTileForEmpty;

        [Tooltip("빈 셀 초기 상태 (DB 전이 기준이 되는 상태)")]
        [SerializeField] private TileState initialStateForEmpty = TileState.Dry;

        private void Awake()
        {
            _targetTilemap = GetComponent<Tilemap>();
            _tileActionSystem = GetComponent<TileActionSystem>();
        }

        private void Start()
        {
            if (!_worldCamera) _worldCamera = SceneGame.Instance.mainCamera;
        }

        private void Update()
        {
            if (ignoreWhenPointerOverUI && EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            if (!GetClickDown()) return;

            if (!_worldCamera || !_targetTilemap || !_tileActionSystem)
            {
                Debug.LogWarning("[ClickToWater] Missing reference(s).", this);
                return;
            }

            // 스크린 → 월드 → 셀
            var mousePos = GetPointerScreenPosition();
            var worldPos = _worldCamera.ScreenToWorldPoint(mousePos);
            var cell     = _targetTilemap.WorldToCell(worldPos);

            // 비어있다면 생성(옵션)
            if (!_targetTilemap.HasTile(cell) && createWhenEmpty)
            {
                // 기본 타일이 없으면 경고만 남기고 진행(전이가 nextTile만 교체해도 되는 경우 대비)
                _tileActionSystem.InitializeCell(cell, initialStateForEmpty, baseTileForEmpty);
            }

            // Water 액션 적용 (전이 DB: (Dry, Water) → (Wet, WetRuleTile, duration))
            _tileActionSystem.ApplyAction(cell, TileAction.Water);
        }

        private bool GetClickDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (useLeftClickOnly) return Mouse.current.leftButton.wasPressedThisFrame;
                return Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                    || Mouse.current.middleButton.wasPressedThisFrame;
            }
            return false;
#else
            if (useLeftClickOnly) return Input.GetMouseButtonDown(0);
            return Input.GetMouseButtonDown(0)
                || Input.GetMouseButtonDown(1)
                || Input.GetMouseButtonDown(2);
#endif
        }

        private Vector3 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
            return Input.mousePosition;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!_targetTilemap || !_worldCamera) return;
            var world = _worldCamera.ScreenToWorldPoint(GetPointerScreenPosition());
            var cell  = _targetTilemap.WorldToCell(world);
            var p     = _targetTilemap.GetCellCenterWorld(cell);
            Gizmos.DrawWireSphere(p, 0.08f);
        }
#endif
    }
}
