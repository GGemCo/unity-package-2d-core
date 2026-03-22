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
    public class VfxEffectLaser : VfxBehaviourEffect
    {
        private static readonly int MainTex   = Shader.PropertyToID("_MainTex");
        private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
        private static readonly int TileX     = Shader.PropertyToID("_TileX");
        private static readonly int ScrollX   = Shader.PropertyToID("_ScrollX");
        private static readonly int Thickness = Shader.PropertyToID("_Thickness");

        public enum LaserRenderMode
        {
            LineRenderer,
            SpriteTiled,
#if GGEMCO_USE_SPINE
            SpineTiled,
#endif
        }

        /// <summary>
        /// Pivot(늘어나는 기준점) 종류
        /// - Start : 시작점 기준(0)으로 길이가 늘어남
        /// - Center: 중앙(0.5) 기준으로 양쪽으로 늘어남
        /// - End   : 끝점(1) 기준으로 길이가 늘어남
        /// - CustomT: 0..1 임의 지점 기준으로 늘어남
        /// </summary>
        public enum PivotAnchor
        {
            Start,   // 0
            Center,  // 0.5
            End,     // 1
            CustomT  // 0..1
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

        [Header("Pivot")]
        [Tooltip("레이저가 늘어나는 기준점(피벗)")]
        [SerializeField] private PivotAnchor pivot = PivotAnchor.Start;

        [Tooltip("PivotAnchor=CustomT일 때 0..1 (0=Start, 0.5=Center, 1=End)")]
        [Range(0f, 1f)]
        [SerializeField] private float pivotT = 0.5f;

        [Tooltip("렌더러가 붙은 Transform (비우면 현재 Transform 사용). Pivot 보정 시 이 트랜스폼을 로컬 오프셋으로 이동시켜 시각적 피벗을 구현합니다.")]
        [SerializeField] private Transform visualRoot;

        [Header("Optional End Caps")]
        [Tooltip("시작 캡(옵션): 별도 스프라이트/프리팹을 배치할 수 있음")]
        [SerializeField] private Transform startCap;
        [Tooltip("끝 캡(옵션): 별도 스프라이트/프리팹을 배치할 수 있음")]
        [SerializeField] private Transform endCap;

        // Runtime State
        private Vector3 _startWorld;
        private Vector3 _endWorld;
        private float _length;

        // Pivot/방향 계산용(월드 기준 방향)
        private Vector3 _dirWorld;   // start→end 방향(정규화)
        private Vector3 _pivotWorld; // 피벗의 월드 좌표

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
            if (!visualRoot) visualRoot = transform;

            _lr = visualRoot.GetComponent<LineRenderer>();
            _sr = visualRoot.GetComponent<SpriteRenderer>();
#if GGEMCO_USE_SPINE
            _skeletonRenderer = visualRoot.GetComponent<SkeletonRenderer>();
#endif
            _mpb = new MaterialPropertyBlock();

            // 두께 초기 적용
            ApplyThickness();

            // Pivot을 자연스럽게 적용하려면 LineRenderer는 로컬 좌표 사용이 편리합니다.
            if (_lr) _lr.useWorldSpace = false; // Pivot 기준으로 좌우(-left, +right) 배치
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
            _endWorld   = endWorld;

            Vector3 dir = (endWorld - startWorld);
            _length = dir.magnitude;

            if (_length <= 0.0001f) return;

            _dirWorld = dir / _length;

            // Pivot(0..1) 지점 계산: 0=Start, 0.5=Center, 1=End (CustomT는 pivotT 사용)
            float t = GetPivotT();
            _pivotWorld = Vector3.Lerp(_startWorld, _endWorld, t);

            // 회전(Z), 위치(Pivot) 적용
            var angle = Mathf.Atan2(_dirWorld.y, _dirWorld.x) * Mathf.Rad2Deg;
            transform.position = _pivotWorld;                     // 피벗을 레이저 루트 위치로 사용
            transform.rotation = Quaternion.Euler(0f, 0f, angle); // 레이저가 +X로 뻗도록 회전

            // 모드별 길이 반영
            switch (renderMode)
            {
                case LaserRenderMode.LineRenderer:
                    if (_lr)
                    {
                        _lr.textureMode = LineTextureMode.Tile;
                        _lr.positionCount = 2;

                        // LineRenderer는 useWorldSpace=false 로컬 좌표 사용:
                        // 피벗을 원점으로 왼쪽/오른쪽으로 배치한다.
                        float left  =  t      * _length; // 피벗에서 시작점까지 거리
                        float right = (1 - t) * _length; // 피벗에서 끝점까지 거리
                        _lr.SetPosition(0, new Vector3(-left,  0f, 0f));
                        _lr.SetPosition(1, new Vector3( right, 0f, 0f));

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

                        // Sprite는 런타임 pivot 변경이 불가하므로 visualRoot를 피벗만큼 로컬 오프셋
                        // sprite의 중앙(0.5)을 기준으로, (0.5 - t) * length 만큼 X축 이동
                        if (visualRoot)
                            visualRoot.localPosition = new Vector3((0.5f - t) * _length, 0f, 0f);

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
                        // Spine도 Sprite와 동일하게 visualRoot를 피벗만큼 이동
                        if (visualRoot)
                        {
                            float tPivot = t;
                            visualRoot.localPosition = new Vector3((0.5f - tPivot) * _length, 0f, 0f);
                        }

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
            // 현재 transform.position(=Pivot)을 기준으로 로컬 +X 방향으로 length 적용
            // (Start=Pivot에서 오른쪽으로만, Center=Pivot 양쪽, End=Pivot에서 왼쪽으로만 보이도록 SetEndpoints로 재위임)
            Vector3 start = transform.position - transform.right * (GetPivotT() * length);
            Vector3 end   = transform.position + transform.right * ((1f - GetPivotT()) * length);
            SetEndpoints(start, end);
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
            if (!startCap && !endCap) return;

            // 실제 월드 시작/끝 지점을 재계산하여 캡을 배치합니다(피벗/길이를 고려).
            float t = GetPivotT();
            float left  =  t      * _length;
            float right = (1 - t) * _length;

            Vector3 start = _pivotWorld - _dirWorld * left;
            Vector3 end   = _pivotWorld + _dirWorld * right;

            if (startCap) startCap.position = start;
            if (endCap)   endCap.position   = end;
        }

        private static void EnsureInstancedMaterial(ref Material inst, LineRenderer lr)
        {
            if (inst == null)
            {
                inst = new Material(lr.sharedMaterial);
                lr.material = inst; // 인스턴스 바인딩
            }
        }

        /// <summary>
        /// Pivot 비율(0..1)을 반환합니다. 0=Start, 0.5=Center, 1=End, CustomT는 pivotT 사용
        /// </summary>
        private float GetPivotT()
        {
            return pivot switch
            {
                PivotAnchor.Start   => 0f,
                PivotAnchor.Center  => 0.5f,
                PivotAnchor.End     => 1f,
                PivotAnchor.CustomT => Mathf.Clamp01(pivotT),
                _ => 0f
            };
        }

        /// <summary>
        /// 런타임에 Pivot 기준을 변경합니다.
        /// </summary>
        public void SetPivot(PivotAnchor anchor, float customT = 0.5f)
        {
            pivot = anchor;
            if (anchor == PivotAnchor.CustomT)
                pivotT = Mathf.Clamp01(customT);
            // 현재 길이/방향 상태 유지 시, 마지막 SetEndpoints 호출을 반복하거나 SetLength로 재적용하세요.
            // (시각적 보정: Sprite/Spine는 visualRoot.localPosition, Line은 로컬 좌표 배치로 처리됩니다)
        }
        /// <summary>
        /// 레이저 renderer는 tiled, slice 모드이므로, flip시 pivot을 변경하여 처리
        /// </summary>
        /// <param name="dirX"></param>
        protected override void OnSetFlip(float dirX)
        {
            if (Mathf.Approximately(dirX, -1))
            {
                SetPivot(PivotAnchor.End);
            }
            else
            {
                SetPivot(PivotAnchor.Start);
            }
        }
    }
}
