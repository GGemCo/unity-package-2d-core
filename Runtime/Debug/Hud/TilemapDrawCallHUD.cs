using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    /// <summary>
    /// TilemapRenderer 기준 최소 DrawCall 하한 추정치를 계산하는 HUD Provider 입니다.
    /// </summary>
    [DebugHudProvider(400)]
    public sealed class TilemapDrawCallEstimator : IDebugHudProvider
    {
        private static readonly List<Tilemap> Tilemaps = new();
        private static readonly HashSet<Texture> Textures = new();

        private readonly StringBuilder _builder = new(512);
        private readonly List<(Tilemap tilemap, TilemapRenderer renderer)> _cache = new();

        public bool IsEnabled(GGemCoSettings settings)
        {
            return settings != null && settings.EnableDebugHud && settings.enableTilemapDrawCallHud;
        }

        public float GetUpdateInterval(GGemCoSettings settings)
        {
            return settings != null ? Mathf.Max(0.1f, settings.debugHudTilemapUpdateInterval) : 1f;
        }

        public void Reset()
        {
            _cache.Clear();
            _builder.Clear();
        }

        public void Tick(float elapsedSeconds)
        {
            GGemCoSettings settings = GGemCoDebugHudManager.CurrentSettings;
            if (settings == null)
            {
                _builder.Clear();
                return;
            }

            Camera targetCamera = Camera.main;
            _cache.Clear();
            _cache.AddRange(CollectAllTilemaps(settings.debugHudTilemapIncludeInactive));

            _builder.Clear();
            _builder.AppendLine("[Tilemap DrawCall]");
            _builder.Append("CamViewOnly: ").Append(settings.debugHudTilemapCameraViewOnly)
                .Append(", Interval: ").Append(settings.debugHudTilemapUpdateInterval.ToString("0.00")).AppendLine("s");
            _builder.AppendLine("----------------------------------------");

            int total = 0;
            foreach ((Tilemap tilemap, TilemapRenderer renderer) entry in _cache)
            {
                int estimate = EstimateMinDrawCalls(
                    entry.tilemap,
                    entry.renderer,
                    targetCamera,
                    settings.debugHudTilemapCameraViewOnly,
                    Mathf.Max(16, settings.debugHudTilemapCellScanBudgetPerAxis));

                total += estimate;
                _builder.Append(entry.renderer.sortingLayerName)
                    .Append('#').Append(entry.renderer.sortingOrder).Append("  ")
                    .Append(entry.tilemap.gameObject.name).Append("  ")
                    .Append("[Mat:")
                    .Append(entry.renderer.sharedMaterial ? entry.renderer.sharedMaterial.name : "Default")
                    .Append("]  -> Min DC ~= ")
                    .AppendLine(estimate.ToString());
            }

            _builder.AppendLine("----------------------------------------");
            _builder.Append("Scene Sum (lower bound): ~= ").AppendLine(total.ToString());
            _builder.Append("Tips: SpriteAtlas로 묶이면 Min DC가 1로 수렴합니다.");
        }

        public bool TryBuildContent(StringBuilder builder)
        {
            if (_builder.Length <= 0)
            {
                return false;
            }

            builder.Append(_builder);
            return true;
        }

        private static Bounds GetCameraWorldAabb(Camera camera)
        {
            if (!camera)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            float z = Mathf.Abs(camera.transform.position.z);
            Vector3 min = camera.ViewportToWorldPoint(new Vector3(0f, 0f, z));
            Vector3 max = camera.ViewportToWorldPoint(new Vector3(1f, 1f, z));
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 2f);
            return new Bounds(center, size);
        }

        public static int EstimateMinDrawCalls(
            Tilemap tilemap,
            TilemapRenderer tilemapRenderer,
            Camera camera,
            bool cameraViewOnly,
            int cellScanBudgetPerAxis = 4096)
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

        public static IReadOnlyList<(Tilemap tilemap, TilemapRenderer renderer)> CollectAllTilemaps(bool includeInactive = false)
        {
            Tilemaps.Clear();

            foreach (Tilemap tilemap in CompatObjectFind.FindAll<Tilemap>())
            {
                if (!includeInactive && !tilemap.isActiveAndEnabled)
                {
                    continue;
                }

                Tilemaps.Add(tilemap);
            }

            List<(Tilemap tilemap, TilemapRenderer renderer)> result = new(Tilemaps.Count);
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
