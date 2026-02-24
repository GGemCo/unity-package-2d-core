using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터(스프라이트) 외곽선(Outline)을 런타임에서 표시/해제하는 컨트롤러.
    /// </summary>
    /// <remarks>
    /// - 셰이더 자산에 의존하지 않도록, SpriteRenderer 복제 + 8방향 오프셋으로 1px 테두리를 구성한다.
    /// - Affect/스킬 등 여러 시스템이 동시에 Outline을 요청할 수 있으므로, 내부적으로 요청을 참조 카운트로 관리한다.
    /// - 픽셀 두께는 스프라이트의 pixelsPerUnit 및 Transform 스케일을 고려하여 월드 단위로 변환된다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterOutlineController : MonoBehaviour
    {
        /// <summary>
        /// Outline 해제를 위한 핸들.
        /// </summary>
        public readonly struct OutlineHandle
        {
            private readonly CharacterOutlineController _owner;
            private readonly int _id;

            internal OutlineHandle(CharacterOutlineController owner, int id)
            {
                _owner = owner;
                _id = id;
            }

            public void Release()
            {
                _owner?.Release(_id);
            }
        }

        [Header("Outline")]
        [Tooltip("Outline 색상 (기본: 검정).")]
        [SerializeField] private Color outlineColor = Color.black;

        private readonly List<SpriteRenderer> _targets = new(16);
        private readonly Dictionary<int, int> _requests = new();
        private int _nextRequestId = 1;
        private int _currentMaxPixelSize;

        private readonly List<OutlineCloneGroup> _cloneGroups = new(16);

        private static readonly Vector2[] Directions8 =
        {
            new( 1, 0),
            new(-1, 0),
            new( 0, 1),
            new( 0,-1),
            new( 1, 1),
            new( 1,-1),
            new(-1, 1),
            new(-1,-1),
        };

        /// <summary>
        /// Outline을 요청한다.
        /// </summary>
        /// <param name="pixelSize">두께(픽셀). 1 이상.</param>
        /// <param name="color"></param>
        public OutlineHandle Acquire(int pixelSize, Color color)
        {
            if (pixelSize <= 0) pixelSize = 1;

            int id = _nextRequestId++;
            _requests[id] = pixelSize;

            int newMax = CalculateMaxPixelSize();
            if (newMax != _currentMaxPixelSize)
            {
                _currentMaxPixelSize = newMax;
                EnsureClones();
                ApplyOffsets();
            }
            else
            {
                EnsureClones();
            }
            outlineColor = color;

            SetEnabled(true);
            return new OutlineHandle(this, id);
        }

        private void Release(int id)
        {
            if (!_requests.Remove(id)) return;

            int newMax = CalculateMaxPixelSize();
            if (newMax != _currentMaxPixelSize)
            {
                _currentMaxPixelSize = newMax;
                if (_currentMaxPixelSize > 0)
                {
                    EnsureClones();
                    ApplyOffsets();
                }
            }

            if (_requests.Count == 0)
            {
                SetEnabled(false);
            }
        }

        private int CalculateMaxPixelSize()
        {
            int max = 0;
            foreach (var kv in _requests)
                max = Math.Max(max, kv.Value);
            return max;
        }

        private void Awake()
        {
            RebuildTargetCache();
        }

        private void OnEnable()
        {
            // 예외적으로 씬 로딩/프리팹 인스턴스 순서에 따라 renderer 캐시가 비어있을 수 있어 재빌드한다.
            if (_targets.Count == 0)
                RebuildTargetCache();

            EnsureClones();
            ApplyOffsets();
            SetEnabled(_requests.Count > 0);
        }

        private void OnDisable()
        {
            SetEnabled(false);
        }

        private void LateUpdate()
        {
            // Animator가 SpriteRenderer의 sprite/flip/sorting/scale 등을 프레임마다 변경할 수 있으므로,
            // Outline이 항상 올바르게 보이도록 활성 상태에서 매 프레임 동기화한다.
            if (_requests.Count <= 0) return;

            // 타겟이 파괴되거나(씬 전환/리셋) 구조가 바뀌었을 수 있으므로 최소 검증.
            if (_targets.Count == 0)
                RebuildTargetCache();

            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] != null) continue;
                RebuildTargetCache();
                break;
            }

            EnsureClones();

            // 두께, 스케일(lossyScale), sprite ppu 변화에 따라 오프셋이 달라질 수 있어 그룹 단위로 조건부 적용.
            for (int i = 0; i < _cloneGroups.Count; i++)
                _cloneGroups[i].Tick(_currentMaxPixelSize, Directions8, outlineColor);
        }

        private void OnDestroy()
        {
            // 생성한 복제 오브젝트 정리
            for (int i = 0; i < _cloneGroups.Count; i++)
                _cloneGroups[i].Dispose();
            _cloneGroups.Clear();
        }

        private void RebuildTargetCache()
        {
            _targets.Clear();
            var arr = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            if (arr == null) return;
            _targets.AddRange(arr);

            // Outline 대상: SpriteRenderer만
            // (Tag/UI 등 SpriteRenderer가 섞일 수 있으나, 정렬 order를 따라가므로 일반적으로 문제 없도록 설계)
        }

        private void EnsureClones()
        {
            if (_currentMaxPixelSize <= 0) return;

            // 타겟 수가 변했으면 그룹 수를 맞춘다.
            if (_cloneGroups.Count != _targets.Count)
            {
                // 기존 정리 후 재생성(캐릭터 구조가 변하는 경우를 단순화)
                for (int i = 0; i < _cloneGroups.Count; i++)
                    _cloneGroups[i].Dispose();
                _cloneGroups.Clear();

                for (int i = 0; i < _targets.Count; i++)
                {
                    var sr = _targets[i];
                    if (sr == null) continue;
                    _cloneGroups.Add(new OutlineCloneGroup(sr, outlineColor));
                }
            }

            // 색상/스프라이트 등 동기화
            for (int i = 0; i < _cloneGroups.Count; i++)
                _cloneGroups[i].SyncFromSource(outlineColor);
        }

        private void ApplyOffsets()
        {
            if (_currentMaxPixelSize <= 0) return;

            for (int i = 0; i < _cloneGroups.Count; i++)
            {
                var group = _cloneGroups[i];
                group.ApplyOffsets(_currentMaxPixelSize, Directions8);
            }
        }

        private void SetEnabled(bool enabled)
        {
            for (int i = 0; i < _cloneGroups.Count; i++)
                _cloneGroups[i].SetActive(enabled);
        }

        private sealed class OutlineCloneGroup
        {
            private readonly SpriteRenderer _source;
            private readonly GameObject _root;
            private readonly SpriteRenderer[] _clones;

            private Sprite _lastSprite;
            private bool _lastFlipX;
            private bool _lastFlipY;
            private int _lastSortingLayerId;
            private int _lastSortingOrder;
            private SpriteMaskInteraction _lastMaskInteraction;
            private Material _lastMaterial;
            private uint _lastRenderingLayerMask;
            private Vector3 _lastLossyScale;
            private float _lastPpu;
            private int _lastPixelSize;
            private Color _lastColor;

            public OutlineCloneGroup(SpriteRenderer source, Color color)
            {
                _source = source;
                _root = new GameObject("__Outline");
                _root.hideFlags = HideFlags.DontSave;
                _root.transform.SetParent(source.transform, worldPositionStays: false);
                _root.transform.localPosition = Vector3.zero;
                _root.transform.localRotation = Quaternion.identity;
                _root.transform.localScale = Vector3.one;

                _clones = new SpriteRenderer[8];
                for (int i = 0; i < _clones.Length; i++)
                {
                    var go = new GameObject($"__Outline_{i}");
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(_root.transform, worldPositionStays: false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.color = color;
                    sr.sprite = source.sprite;
                    sr.flipX = source.flipX;
                    sr.flipY = source.flipY;
                    sr.drawMode = source.drawMode;
                    sr.sortingLayerID = source.sortingLayerID;
                    sr.sortingOrder = source.sortingOrder - 1;
                    sr.maskInteraction = source.maskInteraction;
                    sr.sharedMaterial = source.sharedMaterial;
                    sr.renderingLayerMask = source.renderingLayerMask;
                    sr.enabled = true;
                    _clones[i] = sr;
                }

                CacheFromSource(color, pixelSize: 0);
            }

            private void CacheFromSource(Color color, int pixelSize)
            {
                _lastSprite = _source != null ? _source.sprite : null;
                _lastFlipX = _source != null && _source.flipX;
                _lastFlipY = _source != null && _source.flipY;
                _lastSortingLayerId = _source != null ? _source.sortingLayerID : 0;
                _lastSortingOrder = _source != null ? _source.sortingOrder : 0;
                _lastMaskInteraction = _source != null ? _source.maskInteraction : SpriteMaskInteraction.None;
                _lastMaterial = _source != null ? _source.sharedMaterial : null;
                _lastRenderingLayerMask = _source != null ? _source.renderingLayerMask : 0;
                _lastLossyScale = _source != null ? _source.transform.lossyScale : Vector3.one;
                _lastPpu = (_source != null && _source.sprite != null) ? _source.sprite.pixelsPerUnit : 0f;
                _lastPixelSize = pixelSize;
                _lastColor = color;
            }

            public void SyncFromSource(Color color)
            {
                if (_source == null) return;
                for (int i = 0; i < _clones.Length; i++)
                {
                    var sr = _clones[i];
                    if (sr == null) continue;
                    sr.sprite = _source.sprite;
                    sr.flipX = _source.flipX;
                    sr.flipY = _source.flipY;
                    sr.sortingLayerID = _source.sortingLayerID;
                    sr.sortingOrder = _source.sortingOrder - 1;
                    sr.maskInteraction = _source.maskInteraction;
                    sr.sharedMaterial = _source.sharedMaterial;
                    sr.renderingLayerMask = _source.renderingLayerMask;
                    sr.color = color;
                }
            }

            public void Tick(int pixelSize, Vector2[] directions, Color color)
            {
                if (_source == null) return;

                bool changed = false;

                var sprite = _source.sprite;
                if (!ReferenceEquals(sprite, _lastSprite)) changed = true;
                if (_source.flipX != _lastFlipX) changed = true;
                if (_source.flipY != _lastFlipY) changed = true;
                if (_source.sortingLayerID != _lastSortingLayerId) changed = true;
                if (_source.sortingOrder != _lastSortingOrder) changed = true;
                if (_source.maskInteraction != _lastMaskInteraction) changed = true;
                if (!ReferenceEquals(_source.sharedMaterial, _lastMaterial)) changed = true;
                if (_source.renderingLayerMask != _lastRenderingLayerMask) changed = true;
                if (_source.transform.lossyScale != _lastLossyScale) changed = true;

                float ppu = (sprite != null) ? sprite.pixelsPerUnit : 0f;
                if (Mathf.Abs(ppu - _lastPpu) > 0.0001f) changed = true;
                if (pixelSize != _lastPixelSize) changed = true;
                if (color != _lastColor) changed = true;

                if (!changed) return;

                SyncFromSource(color);
                ApplyOffsets(pixelSize, directions);
                CacheFromSource(color, pixelSize);
            }

            public void ApplyOffsets(int pixelSize, Vector2[] directions)
            {
                if (_source == null || _source.sprite == null) return;

                float ppu = _source.sprite.pixelsPerUnit;
                if (ppu <= 0f) ppu = 100f;

                // local offset(월드 픽셀 -> local) = (pixelSize / ppu) / lossyScale
                var lossy = _source.transform.lossyScale;
                float sx = Mathf.Abs(lossy.x) <= 0.0001f ? 1f : Mathf.Abs(lossy.x);
                float sy = Mathf.Abs(lossy.y) <= 0.0001f ? 1f : Mathf.Abs(lossy.y);
                float ox = (pixelSize / ppu) / sx;
                float oy = (pixelSize / ppu) / sy;

                for (int i = 0; i < _clones.Length && i < directions.Length; i++)
                {
                    var sr = _clones[i];
                    if (sr == null) continue;
                    var dir = directions[i];
                    sr.transform.localPosition = new Vector3(dir.x * ox, dir.y * oy, 0f);
                }
            }

            public void SetActive(bool active)
            {
                if (_root != null) _root.SetActive(active);
            }

            public void Dispose()
            {
                if (_root != null)
                    UnityEngine.Object.Destroy(_root);
            }
        }
    }
}
