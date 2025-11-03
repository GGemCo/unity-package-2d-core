using UnityEngine;
using UnityEngine.Rendering;

namespace GGemCo2DCore
{
    /// <summary>
    /// 지정한 Collider2D 영역 안에 플레이어가 있고,
    /// 실제로 렌더링 정렬상 '나무 뒤'에 있으면 스프라이트 알파를 부드럽게 낮춘다.
    /// (트리거 이벤트 미사용. 좌표 기반 폴링)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OccludeFader2D : MonoBehaviour
    {
        [Header("표시 옵션")]
        [Range(0f, 1f)] public float occludedAlpha = 0.35f;
        [Range(0f, 1f)] public float fadeDuration  = 0.15f;

        [Header("대상 렌더러(지정 안하면 자식 SpriteRenderer 자동 수집)")]
        private Renderer[] _renderersToFade;

        [Header("영역 판정(좌표 기반)")]
        [Tooltip("플레이어 포함 여부를 판정할 Collider2D(Trigger 필요 없음). 미지정 시 GetComponentInChildren<Collider2D>()")]
        private Collider2D _area;

        [Tooltip("실제로 '뒤에' 있는 상황인지 SortingOrder로 추가 확인")]
        private const bool UseSortingCheck = true;

        private Transform _player;
        private struct Original { public Renderer r; public Color c; public Original(Renderer r, Color c){ this.r=r; this.c=c; } }
        private Original[] _originals;
        private bool _isOccluded;
        private bool _quitting;
        private static readonly int ColorProp = Shader.PropertyToID("_Color"); // Sprite-Lit/Unlit 공통
        private static readonly int BaseColor  = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            if (_renderersToFade == null || _renderersToFade.Length == 0)
                _renderersToFade = GetComponentsInParent<Renderer>(true);

            _originals = new Original[_renderersToFade.Length];
            for (int i = 0; i < _renderersToFade.Length; i++)
            {
                var r = _renderersToFade[i];
                var baseColor = (r is SpriteRenderer sr) ? sr.color : Color.white;
                _originals[i] = new Original(r, baseColor);
            }

            if (!_area) _area = GetComponent<Collider2D>();
            _mpb = new MaterialPropertyBlock();
        }

        private void OnApplicationQuit() => _quitting = true;

        private void OnDisable()
        {
            StopAllCoroutines();
            // 필요 시 원복:
            // ApplyAlpha(1f);
        }

        private void Update()
        {
            if (!isActiveAndEnabled || _quitting || !Application.isPlaying) return;
            if (!_area || !_area.enabled) { SetOccluded(false); return; }

            // 플레이어 탐색
            if (!_player)
            {
                var p = SceneGame.Instance ? SceneGame.Instance.player : null;
                if (p) _player = p.transform;
                if (!_player) { SetOccluded(false); return; }
            }

            // 1) 좌표 기반 포함 판정
            // OverlapPoint는 이벤트가 아니라 “해당 점이 콜라이더 도형 내부인가”를 즉시 판정
            var playerPos = (Vector2)_player.position;
            bool inside = _area.OverlapPoint(playerPos);
            // 대체(정밀도 낮음): inside = _area.bounds.Contains(_player.position);

            if (!inside) { SetOccluded(false); return; }

            // 2) (옵션) 정렬 우선순위로 '뒤에' 있는지 추가 확인
            if (UseSortingCheck)
            {
                int treeOrder   = GetEffectiveOrder(_renderersToFade);
                int playerOrder = GetEffectiveOrder(_player.GetComponentsInParent<Renderer>(true));
                // GcLogger.Log($"tree: {treeOrder}, player: {playerOrder}");
                if (!(playerOrder < treeOrder))
                {
                    SetOccluded(false);
                    return;
                }
            }

            // 3) 최종 판정: 페이드 인
            SetOccluded(true);
        }

        private int GetEffectiveOrder(Renderer[] rs)
        {
            if (rs == null || rs.Length == 0) return 0;
            int max = int.MinValue;
            foreach (var r in rs)
            {
                if (!r) continue;
                var sg = r.GetComponentInParent<SortingGroup>();
                if (sg) { max = Mathf.Max(max, sg.sortingOrder); continue; }
                max = Mathf.Max(max, r.sortingOrder);
            }
            return max == int.MinValue ? 0 : max;
        }

        private void SetOccluded(bool v)
        {
            if (!isActiveAndEnabled || _quitting || !Application.isPlaying) return;
            if (_isOccluded == v) return;

            _isOccluded = v;
            StopAllCoroutines();
            StartCoroutine(FadeTo(_isOccluded ? occludedAlpha : 1f));
        }

        private System.Collections.IEnumerator FadeTo(float target)
        {
            if (!isActiveAndEnabled || _quitting) yield break;

            float start = 1f;
            if (_renderersToFade.Length > 0 && _renderersToFade[0] is SpriteRenderer sr0)
                start = sr0.color.a;
            else if (_originals.Length > 0)
                start = _originals[0].c.a;

            float t = 0f;
            while (t < fadeDuration)
            {
                if (!isActiveAndEnabled || _quitting) yield break;
                t += Time.deltaTime;
                ApplyAlpha(Mathf.Lerp(start, target, t / fadeDuration));
                yield return null;
            }
            ApplyAlpha(target);
        }

        private void ApplyAlpha(float a)
        {
            for (int i = 0; i < _renderersToFade.Length; i++)
            {
                var r = _renderersToFade[i];
                if (!r) continue;

                if (r is SpriteRenderer sr)
                {
                    var c = _originals[i].c; c.a = a;
                    sr.color = c; // 권장 방식
                }
                else
                {
                    r.GetPropertyBlock(_mpb);
                    var mat = r.sharedMaterial;
                    if (mat && (mat.HasProperty(ColorProp) || mat.HasProperty(BaseColor)))
                    {
                        var id = mat.HasProperty(ColorProp) ? ColorProp : BaseColor;
                        var c = _originals[i].c; c.a = a;
                        _mpb.SetColor(id, c);
                        r.SetPropertyBlock(_mpb);
                    }
                }
            }
        }
    }
}
