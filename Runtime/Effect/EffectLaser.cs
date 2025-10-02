using System;
using UnityEngine;

#if GGEMCO_USE_SPINE
using Spine.Unity;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 전용 이펙트
    /// - 길이/두께/스크롤/컬러/플립 제어
    /// - LineRenderer / Sprite Tiled / Spine Tiled 지원
    /// - DefaultEffect 의 수명/정렬/색상/플립/End 재생 파이프라인을 그대로 따름
    /// </summary>
    [DisallowMultipleComponent]
    public class EffectLaser : DefaultEffect
    {
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        private static readonly int TileX = Shader.PropertyToID("_TileX");
        private static readonly int ScrollX = Shader.PropertyToID("_ScrollX");
        private static readonly int Thickness = Shader.PropertyToID("_Thickness");

        public enum LaserRenderMode
        {
            LineRenderer,
            SpriteTiled,
#if GGEMCO_USE_SPINE
            SpineTiled,
#endif
        }

        [Header("Laser Settings")]
        [Tooltip("레이저 렌더링 방식")]
        [SerializeField] private LaserRenderMode renderMode = LaserRenderMode.SpriteTiled;

        [Tooltip("레이저 기본 유닛폭(월드 단위). 한 타일의 폭(= spritePixelWidth / PPU). Line/Sprite 공용")]
        [Min(0.0001f)]
        [SerializeField] private float unitWidth = 3f;

        [Tooltip("두께(월드 단위). LineRenderer width / SpriteRenderer.height에 매핑")]
        [Min(0f)]
        [SerializeField] private float thickness = 3f;

        [Tooltip("UV 스크롤 속도(초당 타일). 셰이더에서 _ScrollX 등으로 사용")]
        [SerializeField] private float scrollSpeed = 0f;

        [Header("Optional End Caps")]
        [Tooltip("시작 캡(옵션): 별도 스프라이트/프리팹을 배치할 수 있음")]
        [SerializeField] private Transform startCap;
        [Tooltip("끝 캡(옵션): 별도 스프라이트/프리팹을 배치할 수 있음")]
        [SerializeField] private Transform endCap;

        // Runtime State
        private Vector3 _startWorld;
        private Vector3 _endWorld;
        private float _length;

        // Components (Lazy)
        private LineRenderer _lr;
        private SpriteRenderer _sr;
        private Material _instancedMat; // 각 이펙트마다 인스턴스된 머티리얼
        private MaterialPropertyBlock _mpb;

#if GGEMCO_USE_SPINE
        private SkeletonRenderer _skeletonRenderer;
        private Material _spineMatInst;
#endif

        protected new void Awake()
        {
            base.Awake(); // DefaultEffect.Awake()

            // Lazy get
            _lr = GetComponent<LineRenderer>();
            _sr = GetComponent<SpriteRenderer>();
#if GGEMCO_USE_SPINE
            _skeletonRenderer = GetComponent<SkeletonRenderer>();
#endif
            _mpb = new MaterialPropertyBlock();

            // 두께 초기 적용
            ApplyThickness();
        }

        protected override void Update()
        {
            // DefaultEffect.Update()는 follow 처리만 함. 여기서 레이저 UV 스크롤 등 전용 처리.
            base.Update();

            if (scrollSpeed != 0f)
            {
                float offset = Time.time * scrollSpeed;

                switch (renderMode)
                {
                    case LaserRenderMode.LineRenderer:
                        if (_lr && _lr.sharedMaterial)
                        {
                            EnsureInstancedMaterial(ref _instancedMat, _lr);
                            _instancedMat.SetTextureOffset(MainTex, new Vector2(-offset, 0));
                        }
                        break;

                    case LaserRenderMode.SpriteTiled:
                        if (_sr)
                        {
                            _sr.GetPropertyBlock(_mpb);
                            _mpb.SetVector(MainTexSt, new Vector4(_length / Math.Max(0.0001f, unitWidth), 1, -offset, 0));
                            _sr.SetPropertyBlock(_mpb);
                        }
                        break;

#if GGEMCO_USE_SPINE
                    case LaserRenderMode.SpineTiled:
                        if (_skeletonRenderer && _skeletonRenderer.CustomMaterialOverride != null)
                        {
                            // Spine 머티리얼에 _TileX / _ScrollX 같은 속성이 있다고 가정하고 갱신
                            foreach (var kv in _skeletonRenderer.CustomMaterialOverride)
                            {
                                var mat = kv.Value;
                                if (!mat) continue;
                                if (!ReferenceEquals(mat, _spineMatInst))
                                {
                                    _spineMatInst = new Material(mat);
                                    _skeletonRenderer.CustomMaterialOverride[kv.Key] = _spineMatInst;
                                }
                                _spineMatInst.SetFloat(TileX, _length / Math.Max(0.0001f, unitWidth));
                                _spineMatInst.SetFloat(ScrollX, -offset);
                            }
                        }
                        break;
#endif
                }
            }

            // 캡 위치 갱신
            UpdateCaps();
        }

        /// <summary>
        /// 월드 좌표 기준으로 레이저의 시작/끝을 지정하고 길이/회전/타일을 모두 갱신합니다.
        /// </summary>
        public void SetEndpoints(Vector3 startWorld, Vector3 endWorld)
        {
            _startWorld = startWorld;
            _endWorld = endWorld;

            Vector3 dir = (endWorld - startWorld);
            _length = dir.magnitude;

            if (_length <= 0.0001f) return;

            // 회전(Z), 위치(center) 적용
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.position = startWorld;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // 모드별 길이 반영
            switch (renderMode)
            {
                case LaserRenderMode.LineRenderer:
                    if (_lr)
                    {
                        _lr.textureMode = LineTextureMode.Tile;
                        _lr.positionCount = 2;
                        _lr.SetPosition(0, startWorld);
                        _lr.SetPosition(1, endWorld);

                        if (_lr.sharedMaterial)
                        {
                            EnsureInstancedMaterial(ref _instancedMat, _lr);
                            // 타일 수 = 길이 / 유닛폭
                            Vector2 st = _instancedMat.mainTextureScale;
                            st.x = Math.Max(1f, _length / Math.Max(unitWidth, 0.0001f));
                            _instancedMat.mainTextureScale = st;
                        }
                    }
                    break;

                case LaserRenderMode.SpriteTiled:
                    if (_sr)
                    {
                        // SpriteRenderer는 drawMode=Tiled 이어야 함
                        _sr.drawMode = SpriteDrawMode.Tiled;
                        _sr.size = new Vector2(_length, thickness <= 0 ? _sr.size.y : thickness);

                        // 타일 수 = size.x / unitWidth (셰이더가 ST 타일을 읽는 경우)
                        _sr.GetPropertyBlock(_mpb);
                        _mpb.SetVector(MainTexSt, new Vector4(_length / Math.Max(0.0001f, unitWidth), 1, 0, 0));
                        _sr.SetPropertyBlock(_mpb);
                    }
                    break;

#if GGEMCO_USE_SPINE
                case LaserRenderMode.SpineTiled:
                    if (_skeletonRenderer && _skeletonRenderer.CustomMaterialOverride != null)
                    {
                        foreach (var kv in _skeletonRenderer.CustomMaterialOverride)
                        {
                            var mat = kv.Value;
                            if (!mat) continue;

                            if (!ReferenceEquals(mat, _spineMatInst))
                            {
                                _spineMatInst = new Material(mat);
                                _skeletonRenderer.CustomMaterialOverride[kv.Key] = _spineMatInst;
                            }
                            _spineMatInst.SetFloat(TileX, _length / Math.Max(0.0001f, unitWidth));
                        }
                    }
                    break;
#endif
            }

            UpdateCaps();
        }

        /// <summary>길이만 직접 세팅하고 싶을 때(로컬 축 + 타일만 조정)</summary>
        public void SetLength(float length)
        {
            SetEndpoints(transform.position, transform.position + transform.right * length);
        }

        /// <summary>두께(굵기)를 적용합니다.</summary>
        public void SetThickness(float worldThickness)
        {
            thickness = Mathf.Max(0f, worldThickness);
            ApplyThickness();
        }

        private void ApplyThickness()
        {
            switch (renderMode)
            {
                case LaserRenderMode.LineRenderer:
                    if (_lr) _lr.widthMultiplier = thickness;
                    break;
                case LaserRenderMode.SpriteTiled:
                    if (_sr)
                    {
                        _sr.drawMode = SpriteDrawMode.Tiled;
                        _sr.size = new Vector2(_sr.size.x <= 0 ? unitWidth : _sr.size.x, thickness <= 0 ? _sr.size.y : thickness);
                    }
                    break;
#if GGEMCO_USE_SPINE
                case LaserRenderMode.SpineTiled:
                    // Spine은 슬롯 스케일/본 스케일 또는 머티리얼로 굵기를 표현.
                    // 프로젝트 셰이더 정책에 따라 별도 파라미터로 처리하세요(예: _Thickness).
                    if (_skeletonRenderer && _skeletonRenderer.CustomMaterialOverride != null)
                    {
                        foreach (var kv in _skeletonRenderer.CustomMaterialOverride)
                        {
                            var mat = kv.Value; if (!mat) continue;
                            if (!ReferenceEquals(mat, _spineMatInst))
                            {
                                _spineMatInst = new Material(mat);
                                _skeletonRenderer.CustomMaterialOverride[kv.Key] = _spineMatInst;
                            }
                            _spineMatInst.SetFloat(Thickness, thickness);
                        }
                    }
                    break;
#endif
            }
        }

        private void UpdateCaps()
        {
            if (startCap) startCap.position = _startWorld;
            if (endCap) endCap.position = _endWorld;
        }

        private static void EnsureInstancedMaterial(ref Material inst, LineRenderer lr)
        {
            if (inst == null)
            {
                inst = new Material(lr.sharedMaterial);
                lr.material = inst; // 인스턴스 바인딩
            }
        }
    }
}
