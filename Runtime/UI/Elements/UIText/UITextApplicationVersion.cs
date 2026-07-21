using System;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Player Settings에 설정된 애플리케이션 버전을 UI 텍스트에 표시합니다.
    /// </summary>
    /// <remarks>
    /// 버전은 <see cref="Application.version"/>에서 읽으며 오브젝트가 활성화될 때 한 번만 갱신합니다.
    /// 모든 프로젝트에서 같은 컴포넌트를 사용할 수 있도록 게임 전용 설정이나 테이블에 의존하지 않습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class UITextApplicationVersion : MonoBehaviour
    {
        [Tooltip("애플리케이션 버전 앞에 표시할 문자열입니다.")]
        [SerializeField] private string prefix = "v";

        [Tooltip("애플리케이션 버전 뒤에 표시할 문자열입니다.")]
        [SerializeField] private string suffix = string.Empty;

        private TextMeshProUGUI _targetText;
        private bool _didLogMissingText;

        /// <summary>
        /// 같은 게임 오브젝트의 버전 표시용 TMP 텍스트를 캐시합니다.
        /// </summary>
        private void Awake()
        {
            TryCacheTargetText();
        }

        /// <summary>
        /// UI가 다시 활성화될 때 현재 애플리케이션 버전을 반영합니다.
        /// </summary>
        private void OnEnable()
        {
            RefreshVersionText();
        }

        /// <summary>
        /// Player Settings의 애플리케이션 버전을 현재 UI 텍스트에 즉시 반영합니다.
        /// </summary>
        public void RefreshVersionText()
        {
            if (!TryCacheTargetText())
            {
                if (!_didLogMissingText)
                {
                    GcLogger.LogError(
                        $"[{nameof(UITextApplicationVersion)}] TextMeshProUGUI 컴포넌트가 없습니다. " +
                        $"gameObject={gameObject.name}");
                    _didLogMissingText = true;
                }

                return;
            }

            _didLogMissingText = false;
            string displayText = string.Concat(prefix, Application.version, suffix);
            if (string.Equals(_targetText.text, displayText, StringComparison.Ordinal))
            {
                return;
            }

            // 버전은 실행 중 변경되지 않으므로 활성화 시점에만 문자열을 만들고 TMP 텍스트를 갱신합니다.
            _targetText.SetText(displayText);
        }

        /// <summary>
        /// 버전을 표시할 TMP 텍스트를 찾아 캐시합니다.
        /// </summary>
        /// <returns>사용 가능한 텍스트 컴포넌트를 확보했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryCacheTargetText()
        {
            if (_targetText == null)
            {
                _targetText = GetComponent<TextMeshProUGUI>();
            }

            return _targetText != null;
        }
    }
}
