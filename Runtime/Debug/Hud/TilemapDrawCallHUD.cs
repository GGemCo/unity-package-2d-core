#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타일맵 최소 DrawCall 추정 정보를 계산하는 런타임 프로바이더입니다.
    /// </summary>
    public sealed class TilemapDrawCallEstimator : IDebugHudProvider
    {
        private static readonly List<Tilemap> Tilemaps = new();
        private static readonly HashSet<Texture> Textures = new();

        private readonly StringBuilder _sb = new();
        private readonly List<(Tilemap tm, TilemapRenderer renderer)> _cache = new();
        private float _remainingTime;
        private bool _initialized;

        public DebugHudAnchor Anchor => DebugHudAnchor.TopLeft;

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && DebugOptionRuntimeUtility.Resolve(settings.enableDebugHud) && DebugOptionRuntimeUtility.Resolve(settings.enableTilemapDrawCallHud);
        }

        public void Initialize(GGemCoSettings settings)
        {
            _remainingTime = 0f;
            _sb.Length = 0;
            _sb.AppendLine("[Tilemap DrawCall]");
            _sb.Append("Collecting...");
            _initialized = true;
        }

        public void Tick(float unscaledDeltaTime, GGemCoSettings settings)
        {
            if (!_initialized)
            {
                Initialize(settings);
            }

            _remainingTime -= Mathf.Max(0f, unscaledDeltaTime);
            if (_remainingTime > 0f)
            {
                return;
            }

            RefreshNow(settings);
            _remainingTime = Mathf.Max(0.1f, settings != null ? settings.debugHudTilemapUpdateInterval : 0.5f);
        }

        public string GetText() => _sb.ToString();

        private void RefreshNow(GGemCoSettings settings)
        {
            _cache.Clear();
            IReadOnlyList<(Tilemap tilemap, TilemapRenderer tmr)> tilemaps = CollectAllTilemaps(settings != null && settings.debugHudTilemapIncludeInactive);
            foreach (var tilemap in tilemaps)
            {
                _cache.Add((tilemap.tilemap, tilemap.tmr));
            }

            Camera targetCamera = Camera.main;
            bool cameraViewOnly = settings == null || settings.debugHudTilemapCameraViewOnly;
            int scanBudgetPerAxis = settings != null ? Mathf.Max(64, settings.debugHudTilemapCellScanBudgetPerAxis) : 4096;

            _sb.Length = 0;
            _sb.AppendLine("Tilemap DrawCall Estimator (min, by unique textures)");
            _sb.AppendLine($"CamViewOnly: {cameraViewOnly}, Interval: {(settings != null ? settings.debugHudTilemapUpdateInterval : 0.5f):0.00}s");
            _sb.AppendLine("----------------------------------------------------");

            int grandTotal = 0;
            foreach ((Tilemap tm, TilemapRenderer renderer) in _cache)
            {
                int estimate = EstimateMinDrawCalls(tm, renderer, targetCamera, cameraViewOnly, scanBudgetPerAxis);
                grandTotal += estimate;

                _sb.Append($"{renderer.sortingLayerName}#{renderer.sortingOrder}  ");
                _sb.Append($"{tm.gameObject.name}  ");
                _sb.Append($"[Mat:{(renderer.sharedMaterial ? renderer.sharedMaterial.name : "Default")}]  ");
                _sb.AppendLine($"→ Min DC ≈ {estimate}");
            }

            _sb.AppendLine("----------------------------------------------------");
            _sb.AppendLine($"Scene Sum (lower bound): ≈ {grandTotal}");
            _sb.AppendLine("Tips: SpriteAtlas로 묶이면 Min DC가 1로 수렴합니다.");
        }

        private static Bounds GetCameraWorldAabb(Camera cam)
        {
            if (!cam) return new Bounds(Vector3.zero, Vector3.zero);

            float z = 0f;
            Vector3 min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, Mathf.Abs(cam.transform.position.z - z)));
            Vector3 max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, Mathf.Abs(cam.transform.position.z - z)));
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 2f);
            return new Bounds(center, size);
        }

        public static int EstimateMinDrawCalls(Tilemap tilemap, TilemapRenderer tilemapRenderer, Camera camera, bool cameraViewOnly, int cellScanBudgetPerAxis = 4096)
        {
            if (!tilemap || !tilemapRenderer)
            {
                return 0;
            }

            Textures.Clear();
            BoundsInt cells = tilemap.cellBounds;
            if (cameraViewOnly && camera)
            {
                Bounds aabb = GetCameraWorldAabb(camera);
                Vector3Int minCell = tilemap.WorldToCell(aabb.min);
                Vector3Int maxCell = tilemap.WorldToCell(aabb.max);

                const int pad = 2;
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
                    Sprite sprite = tilemap.GetSprite(new Vector3Int(x, y, 0));
                    if (!sprite)
                    {
                        continue;
                    }

                    Texture texture = sprite.texture;
                    if (texture)
                    {
                        Textures.Add(texture);
                    }
                }
            }

            return Mathf.Max(1, Textures.Count);
        }

        public static IReadOnlyList<(Tilemap tilemap, TilemapRenderer tmr)> CollectAllTilemaps(bool includeInactive = false)
        {
            Tilemaps.Clear();
            if (includeInactive)
            {
                foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                {
                    Tilemaps.Add(tilemap);
                }
            }
            else
            {
                foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.InstanceID))
                {
                    if (tilemap.isActiveAndEnabled)
                    {
                        Tilemaps.Add(tilemap);
                    }
                }
            }

            List<(Tilemap, TilemapRenderer)> result = new(Tilemaps.Count);
            foreach (Tilemap tilemap in Tilemaps)
            {
                TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
                if (renderer)
                {
                    result.Add((tilemap, renderer));
                }
            }

            return result;
        }
    }
}
#endif
