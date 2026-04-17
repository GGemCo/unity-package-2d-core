using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    public class TagNameNpc : DefaultTagName
    {
        private Npc _npc;
        private StruckTableNpc _struckTableNpc;
        private bool _isStartFade;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _isStartFade = false;
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Initialize(GameObject itemObject)
        {
            if (itemObject == null || TableLoaderManager.Instance == null) return;
            _npc = itemObject.GetComponent<Npc>();
            _struckTableNpc = TableLoaderManager.Instance.GetNpcData(_npc.uid);
            string nameFunction = "";
            if (_struckTableNpc.InteractionUid > 0)
            {
                var info = TableLoaderManager.Instance.GetInteractionData(_struckTableNpc.InteractionUid);
                string displayName = ResolveFirstInteractionDisplayName(info);
                if (string.IsNullOrWhiteSpace(displayName) == false)
                {
                    nameFunction = $" - {displayName}";
                }
            }

            textName.text = $"[ {_struckTableNpc.Name}{nameFunction} ]";
            ApplyTextEffect();
        }

        private static string ResolveFirstInteractionDisplayName(StruckTableInteraction info)
        {
            if (info == null)
            {
                return string.Empty;
            }

            if (info.Type1 != InteractionConstants.Type.None)
            {
                return InteractionConstants.GetTypeName(info.Type1);
            }

            if (string.IsNullOrWhiteSpace(info.CustomTypeKey1) == false)
            {
                if (InteractionCustomHandlerRegistry.TryGetDisplayName(info.CustomTypeKey1, info.Value1, out var customName))
                {
                    return customName;
                }

                return info.CustomTypeKey1;
            }

            return string.Empty;
        }

        private void LateUpdate()
        {
            if (_npc == null || _npc.gameObject == null) return;
            // 아이템 위 월드 좌표 설정
            Vector3 npcNameWorldPosition = _npc.gameObject.transform.position + new Vector3(0, _npc.GetHeightByScale(), 0) + diffTextPosition;
            gameObject.transform.position = npcNameWorldPosition;
        }

        public bool IsVisible()
        {
            return gameObject.activeSelf && (_canvasGroup == null || _canvasGroup.alpha > 0f);
        }

        public void SetVisibleImmediate(bool isVisible)
        {
            StopAllCoroutines();
            _isStartFade = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isVisible ? 1f : 0f;
            }

            gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// fade in 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeIn()
        {
            if (_isStartFade) return;
            _isStartFade = true;
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
        }

        /// <summary>
        /// fade out 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeOut()
        {
            if (_isStartFade) return;
            _isStartFade = true;
            StartCoroutine(FadeOut(ConfigCommon.CharacterFadeSec));
        }
        private IEnumerator FadeIn(float duration)
        {
            yield return FadeEffect(duration, true);
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return FadeEffect(duration, false);
            gameObject.SetActive(false);
        }

        private IEnumerator FadeEffect(float duration, bool fadeIn)
        {
            float elapsedTime = 0f;
            float startAlpha = fadeIn ? 0 : 1;
            float endAlpha = fadeIn ? 1 : 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                yield return null;
            }

            SetIsStartFade(false);
        }
        private void SetIsStartFade(bool value)
        {
            _isStartFade = value;
        }
    }
}
