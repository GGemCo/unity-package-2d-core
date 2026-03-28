using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// SpriteRenderer 기반 잔상(Afterimage) 트레일 / 스냅샷 연출.
    /// - 백스탭/대시/회피 등 빠른 이동 구간의 연속 잔상에 사용합니다.
    /// - 공격 특정 프레임의 단발 스냅샷 잔상에도 재사용합니다.
    /// - 풀링을 사용하여 Instantiate/GC 비용을 최소화합니다.
    /// - AnimationEvent(string json)로 런타임 오버라이드 설정을 적용할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAfterimageTrail : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private SpriteRenderer source;

        [Header("Defaults")]
        [SerializeField, Min(0.005f)] private float defaultSpawnIntervalSeconds = 0.03f;
        [SerializeField, Min(0.01f)] private float defaultGhostLifetimeSeconds = 0.25f;
        [SerializeField] private Color defaultGhostColor = new(0.25f, 0.65f, 1.0f, 0.65f);
        [SerializeField] private int defaultSortingOrderOffset = -1;

        [Header("Pooling")]
        [SerializeField, Min(4)] private int prewarmCount = 12;

        private readonly Queue<SpriteRenderer> _pool = new();
        private readonly List<Ghost> _active = new(32);

        private bool _running;
        private float _nextSpawnTime;
        private float _stopTime;

        // 이번 실행에만 적용되는 런타임 설정
        private float _spawnIntervalSeconds;
        private float _ghostLifetimeSeconds;
        private Color _ghostColor;
        private int _sortingOrderOffset;

        private struct Ghost
        {
            public SpriteRenderer Sr;
            public float StartTime;
            public float EndTime;
            public float BaseAlpha;
        }

        private void Awake()
        {
            if (source == null)
                source = GetComponent<SpriteRenderer>();

            ApplyDefaults();

            for (int i = 0; i < prewarmCount; i++)
            {
                var sr = CreateGhostRenderer();
                sr.gameObject.SetActive(false);
                _pool.Enqueue(sr);
            }

            enabled = false;
        }

        private void OnDestroy()
        {
            foreach (var sr in _pool)
            {
                if (sr == null) continue;
                Destroy(sr.gameObject);
            }
        }

        private void ApplyDefaults()
        {
            _spawnIntervalSeconds = Mathf.Max(0.005f, defaultSpawnIntervalSeconds);
            _ghostLifetimeSeconds = Mathf.Max(0.01f, defaultGhostLifetimeSeconds);
            _ghostColor = defaultGhostColor;
            _ghostColor.a = Mathf.Clamp01(_ghostColor.a);
            _sortingOrderOffset = defaultSortingOrderOffset;
        }

        /// <summary>
        /// 기본 설정으로 트레일을 시작합니다.
        /// </summary>
        public void StartTrail(float durationSeconds = 0f)
        {
            ApplyDefaults();
            StartInternal(durationSeconds);
        }

        /// <summary>
        /// AnimationEvent JSON으로 받은 설정(선택)을 적용하여 트레일을 시작합니다.
        /// </summary>
        public void StartTrail(StruckAnimationEventBackstepTrail settings)
        {
            ApplyDefaults();
            ApplyTrailSettings(settings);
            StartInternal(settings?.DurationSeconds ?? 0f);
        }

        /// <summary>
        /// 현재 프레임의 Sprite를 단발 잔상으로 1회 캡처합니다.
        /// </summary>
        public void CaptureOnce()
        {
            ApplyDefaults();
            CaptureNow();
        }

        /// <summary>
        /// AnimationEvent JSON으로 받은 설정(선택)을 적용하여 단발 잔상을 1회 캡처합니다.
        /// </summary>
        public void CaptureOnce(StruckAnimationEventAfterimageSnapshot settings)
        {
            ApplyDefaults();
            ApplySnapshotSettings(settings);
            CaptureNow();
        }

        public void StopTrail()
        {
            _running = false;
            _stopTime = 0f;
            if (_active.Count > 0)
                enabled = true;
        }

        private void StartInternal(float durationSeconds)
        {
            if (!CanCaptureSource())
                return;

            _running = true;
            _nextSpawnTime = Time.time;
            _stopTime = durationSeconds > 0f ? (Time.time + durationSeconds) : 0f;
            enabled = true;
        }

        private void CaptureNow()
        {
            if (!CanCaptureSource())
                return;

            SpawnGhost(Time.time);
            enabled = true;
        }

        private bool CanCaptureSource()
        {
            return source != null && source.sprite != null;
        }

        private void ApplyTrailSettings(StruckAnimationEventBackstepTrail settings)
        {
            if (settings == null)
                return;

            if (settings.SpawnIntervalSeconds > 0f)
                _spawnIntervalSeconds = Mathf.Max(0.005f, settings.SpawnIntervalSeconds);

            if (settings.GhostLifetimeSeconds > 0f)
                _ghostLifetimeSeconds = Mathf.Max(0.01f, settings.GhostLifetimeSeconds);

            ApplyColorOverride(settings.ColorHex, null);

            if (settings.SortingOrderOffset.HasValue)
                _sortingOrderOffset = settings.SortingOrderOffset.Value;
        }

        private void ApplySnapshotSettings(StruckAnimationEventAfterimageSnapshot settings)
        {
            if (settings == null)
                return;

            if (settings.GhostLifetimeSeconds > 0f)
                _ghostLifetimeSeconds = Mathf.Max(0.01f, settings.GhostLifetimeSeconds);

            ApplyColorOverride(settings.ColorHex, settings.Alpha);

            if (settings.SortingOrderOffset.HasValue)
                _sortingOrderOffset = settings.SortingOrderOffset.Value;
        }

        private void ApplyColorOverride(string colorHex, float? alphaOverride)
        {
            if (!string.IsNullOrWhiteSpace(colorHex) &&
                ColorUtility.TryParseHtmlString(NormalizeHtmlColor(colorHex), out var parsedColor))
            {
                _ghostColor = parsedColor;
            }

            if (alphaOverride.HasValue && alphaOverride.Value >= 0f)
            {
                _ghostColor.a = Mathf.Clamp01(alphaOverride.Value);
            }
            else
            {
                _ghostColor.a = Mathf.Clamp01(_ghostColor.a);
            }
        }

        private void Update()
        {
            float now = Time.time;

            if (_running)
            {
                if (_stopTime > 0f && now >= _stopTime)
                    _running = false;

                if (now >= _nextSpawnTime)
                {
                    SpawnGhost(now);
                    _nextSpawnTime = now + _spawnIntervalSeconds;
                }
            }

            TickGhosts(now);

            if (!_running && _active.Count == 0)
                enabled = false;
        }

        private void SpawnGhost(float now)
        {
            var sprite = source.sprite;
            if (sprite == null)
                return;

            var sr = Rent();

            var sourceTransform = source.transform;
            var ghostTransform = sr.transform;
            ghostTransform.position = sourceTransform.position;
            ghostTransform.rotation = sourceTransform.rotation;
            ghostTransform.localScale = sourceTransform.lossyScale;

            sr.sprite = sprite;
            sr.flipX = source.flipX;
            sr.flipY = source.flipY;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder + _sortingOrderOffset;
            sr.color = _ghostColor;

            _active.Add(new Ghost
            {
                Sr = sr,
                StartTime = now,
                EndTime = now + _ghostLifetimeSeconds,
                BaseAlpha = _ghostColor.a
            });
        }

        private void TickGhosts(float now)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var ghost = _active[i];
                if (now >= ghost.EndTime)
                {
                    Return(ghost.Sr);
                    _active.RemoveAt(i);
                    continue;
                }

                float normalized = Mathf.InverseLerp(ghost.StartTime, ghost.EndTime, now);
                var color = ghost.Sr.color;
                color.a = ghost.BaseAlpha * (1f - normalized);
                ghost.Sr.color = color;
            }
        }

        private SpriteRenderer Rent()
        {
            if (_pool.Count > 0)
            {
                var sr = _pool.Dequeue();
                sr.gameObject.SetActive(true);
                return sr;
            }

            var created = CreateGhostRenderer();
            created.gameObject.SetActive(true);
            return created;
        }

        private void Return(SpriteRenderer sr)
        {
            if (sr == null)
                return;

            sr.gameObject.SetActive(false);
            sr.sprite = null;
            _pool.Enqueue(sr);
        }

        private SpriteRenderer CreateGhostRenderer()
        {
            var go = new GameObject("AfterimageGhost");
            go.transform.SetParent(null, false);
            var sr = go.AddComponent<SpriteRenderer>();
            return sr;
        }

        private static string NormalizeHtmlColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return "#FFFFFFFF";

            return hex.StartsWith("#") ? hex : "#" + hex;
        }
    }
}
