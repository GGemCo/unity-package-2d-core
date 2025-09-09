#if UNITY_EDITOR
// File: TilemapDrawCallHUD.cs
// Desc: GGemCoDebugHudRoot의 스타일을 실시간으로 사용하도록 수정

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace GGemCo2DCore
{
    public static class TilemapDrawCallEstimator
    {
        private static readonly List<Tilemap> Tilemaps = new();
        private static readonly HashSet<Texture> Textures = new();

        private static Bounds GetCameraWorldAabb(Camera cam)
        {
            if (!cam) return new Bounds(Vector3.zero, Vector3.zero);

            var z = 0f; // 2D 기준
            var min = cam.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(cam.transform.position.z - z)));
            var max = cam.ViewportToWorldPoint(new Vector3(1, 1, Mathf.Abs(cam.transform.position.z - z)));
            var center = (min + max) * 0.5f;
            var size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 2f);
            return new Bounds(center, size);
        }

        public static int EstimateMinDrawCalls(
            Tilemap tilemap, TilemapRenderer tmr, Camera cam,
            bool cameraViewOnly, int cellScanBudgetPerAxis = 4096)
        {
            if (!tilemap || !tmr) return 0;

            Textures.Clear();

            BoundsInt cells = tilemap.cellBounds;
            if (cameraViewOnly && cam)
            {
                var aabb = GetCameraWorldAabb(cam);
                var minCell = tilemap.WorldToCell(aabb.min);
                var maxCell = tilemap.WorldToCell(aabb.max);

                int pad = 2;
                int x0 = Mathf.Clamp(Mathf.Min(minCell.x, maxCell.x) - pad, cells.xMin, cells.xMax);
                int x1 = Mathf.Clamp(Mathf.Max(minCell.x, maxCell.x) + pad, cells.xMin, cells.xMax);
                int y0 = Mathf.Clamp(Mathf.Min(minCell.y, maxCell.y) - pad, cells.yMin, cells.yMax);
                int y1 = Mathf.Clamp(Mathf.Max(minCell.y, maxCell.y) + pad, cells.yMin, cells.yMax);

                if (x1 - x0 > cellScanBudgetPerAxis) x1 = x0 + cellScanBudgetPerAxis;
                if (y1 - y0 > cellScanBudgetPerAxis) y1 = y0 + cellScanBudgetPerAxis;

                cells = new BoundsInt(x0, y0, 0, x1 - x0 + 1, y1 - y0 + 1, 1);
            }

            for (int y = cells.yMin; y < cells.yMax; y++)
            {
                for (int x = cells.xMin; x < cells.xMax; x++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var sprite = tilemap.GetSprite(pos);
                    if (!sprite) continue;

                    var tex = sprite.texture;
                    if (tex) Textures.Add(tex);
                }
            }

            return Mathf.Max(1, Textures.Count);
        }

        public static IReadOnlyList<(Tilemap tilemap, TilemapRenderer tmr)> CollectAllTilemaps(bool includeInactive = false)
        {
            Tilemaps.Clear();
            if (includeInactive)
            {
                foreach (var t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                    Tilemaps.Add(t);
            }
            else
            {
                foreach (var t in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.InstanceID))
                    if (t.isActiveAndEnabled) Tilemaps.Add(t);
            }

            List<(Tilemap, TilemapRenderer)> result = new(Tilemaps.Count);
            foreach (var tm in Tilemaps)
            {
                var r = tm.GetComponent<TilemapRenderer>();
                if (r) result.Add((tm, r));
            }
            return result;
        }
    }

    /// <summary>
    /// TilemapRenderer별 "최소 드로우콜(유니크 텍스처 수 기반)" 추산 HUD.
    /// GGemCoDebugHudRoot의 스타일을 사용하여 폰트/배경/패딩이 실시간 반영됩니다.
    /// </summary>
    public class TilemapDrawCallHUD : MonoBehaviour
    {
        [Header("General")]
        [Tooltip("미지정 시 Camera.main 사용")]
        public Camera targetCamera;

        [Tooltip("갱신 주기(초)")]
        [Min(0.05f)]
        public float updateInterval = 0.5f;

        [Tooltip("카메라 뷰 내 타일만 스캔(권장)")]
        public bool cameraViewOnly = true;

        [Tooltip("비활성 오브젝트 포함")]
        public bool includeInActive;

        [Header("Advanced")]
        [Tooltip("한 축당 최대 스캔 셀 수 제한(아주 큰 타일맵 보호용)")]
        public int cellScanBudgetPerAxis = 4096;

        private float _nextTime;
        private readonly StringBuilder _sb = new();
        private List<(Tilemap tm, TilemapRenderer r)> _cache;
        private GGemCoDebugHudRoot _root;

        private void OnEnable()
        {
            if (!targetCamera) targetCamera = Camera.main;
            _cache = new List<(Tilemap, TilemapRenderer)>();
            _root = FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);
            RefreshNow();
            _nextTime = Time.unscaledTime + updateInterval;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextTime)
            {
                RefreshNow();
                _nextTime = Time.unscaledTime + updateInterval;
            }

            // 루트가 런타임에 생성될 수 있으므로 매 프레임 한 번 확인(저비용)
            if (!_root) _root = FindAnyObjectByType<GGemCoDebugHudRoot>(FindObjectsInactive.Include);
        }

        private void RefreshNow()
        {
            _cache.Clear();
            var list = TilemapDrawCallEstimator.CollectAllTilemaps(includeInActive);
            _cache.AddRange(list);

            _sb.Length = 0;
            _sb.AppendLine("Tilemap DrawCall Estimator (min, by unique textures)");
            _sb.AppendLine($"CamViewOnly: {cameraViewOnly}, Interval: {updateInterval:0.00}s");
            _sb.AppendLine("----------------------------------------------------");

            int grandTotal = 0;
            foreach (var (tm, r) in _cache)
            {
                int est = TilemapDrawCallEstimator.EstimateMinDrawCalls(
                    tm, r, targetCamera, cameraViewOnly, cellScanBudgetPerAxis);

                grandTotal += est;

                _sb.Append($"{r.sortingLayerName}#{r.sortingOrder}  ");
                _sb.Append($"{tm.gameObject.name}  ");
                _sb.Append($"[Mat:{(r.sharedMaterial ? r.sharedMaterial.name : "Default")}]  ");
                _sb.AppendLine($"→ Min DC ≈ {est}");
            }
            _sb.AppendLine("----------------------------------------------------");
            _sb.AppendLine($"Scene Sum (lower bound): ≈ {grandTotal}");
            _sb.AppendLine("Tips: SpriteAtlas로 묶이면 Min DC가 1로 수렴합니다.");
        }

        private void OnGUI()
        {
            var content = new GUIContent(_sb.ToString());

            // Root 스타일/패딩 실시간 반영
            var style = _root ? _root.GetStyle() : GUI.skin.box;
            Vector2 pad = _root ? _root.padding : new Vector2(8, 8);

            Vector2 size = style.CalcSize(content);
            var rect = new Rect(pad.x, pad.y, Mathf.Min(size.x + 10, Screen.width - pad.x * 2), size.y + 10);

            if (_root) _root.DrawBox(rect, content);
            else GUI.Box(rect, content, style);
        }

        private void OnValidate()
        {
            // 에디터에서 파라미터 변경 시 즉시 반영
            EditorApplication.delayCall += RepaintAllGameViews;
        }

        private static void RepaintAllGameViews()
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView, UnityEditor");
            if (gameViewType == null) return;
            foreach (var gv in Resources.FindObjectsOfTypeAll(gameViewType))
                gameViewType.GetMethod("Repaint")?.Invoke(gv, null);
        }
    }
}
#endif