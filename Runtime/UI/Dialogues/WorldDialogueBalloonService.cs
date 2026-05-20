using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임플레이 중 캐릭터 위에 짧은 말풍선 대사를 표시하는 범용 서비스입니다.
    /// 컷신 타임라인과 독립적으로 동작하므로 동행 NPC, 펫, 전투 조언 대사에 사용할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldDialogueBalloonService : MonoBehaviour
    {
        private DialogueBalloonPool _pool;
        private Coroutine _currentRoutine;
        private GameObject _currentBalloon;

        /// <summary>
        /// 말풍선 풀을 초기화합니다.
        /// </summary>
        /// <param name="container">말풍선 UI가 배치될 부모 Transform입니다.</param>
        public void Initialize(Transform container)
        {
            if (container == null)
            {
                GcLogger.LogWarning("WorldDialogueBalloonService 초기화에 사용할 container가 없습니다.");
                return;
            }

            _pool = new DialogueBalloonPool(container);
        }

        /// <summary>
        /// 대상 캐릭터 위에 지정 시간 동안 말풍선을 표시합니다.
        /// </summary>
        /// <param name="speaker">말풍선을 표시할 캐릭터입니다.</param>
        /// <param name="message">표시할 텍스트입니다.</param>
        /// <param name="duration">표시 시간입니다.</param>
        /// <param name="fontSize">폰트 크기입니다. 0 이하면 프리팹 기본값을 사용합니다.</param>
        /// <param name="interruptCurrent">기존 말풍선을 중단하고 새 말풍선을 표시할지 여부입니다.</param>
        /// <returns>표시 요청을 수락했으면 true입니다.</returns>
        public bool Say(CharacterBase speaker, string message, float duration = 2f, float fontSize = 0f, bool interruptCurrent = true)
        {
            if (speaker == null || string.IsNullOrWhiteSpace(message) || _pool == null)
            {
                return false;
            }

            if (_currentRoutine != null)
            {
                if (!interruptCurrent)
                {
                    return false;
                }

                StopCoroutine(_currentRoutine);
                ReturnCurrentBalloon();
            }

            _currentRoutine = StartCoroutine(CoSay(speaker, message, duration, fontSize));
            return true;
        }

        /// <summary>
        /// 현재 표시 중인 말풍선을 즉시 종료합니다.
        /// </summary>
        public void Clear()
        {
            if (_currentRoutine != null)
            {
                StopCoroutine(_currentRoutine);
                _currentRoutine = null;
            }

            ReturnCurrentBalloon();
        }

        /// <summary>
        /// 말풍선을 생성하고 duration 이후 풀에 반환합니다.
        /// </summary>
        /// <param name="speaker">화자 캐릭터입니다.</param>
        /// <param name="message">표시할 메시지입니다.</param>
        /// <param name="duration">표시 시간입니다.</param>
        /// <param name="fontSize">폰트 크기입니다.</param>
        /// <returns>말풍선 표시 코루틴입니다.</returns>
        private IEnumerator CoSay(CharacterBase speaker, string message, float duration, float fontSize)
        {
            _currentBalloon = _pool.Get(this);
            UIDialogueBalloon balloonUi = _currentBalloon != null ? _currentBalloon.GetComponent<UIDialogueBalloon>() : null;
            if (balloonUi == null)
            {
                ReturnCurrentBalloon();
                _currentRoutine = null;
                yield break;
            }

            DialogueBalloonData data = new DialogueBalloonData
            {
                message = message,
                fontSize = fontSize,
                useTypewriter = false,
                waitForUserInput = false,
                thumbnailPositionType = ConfigCommon.ThumbnailPositionType.None
            };
            balloonUi.Initialize(speaker, data);

            float waitTime = Mathf.Max(0.1f, duration);
            yield return new WaitForSeconds(waitTime);

            ReturnCurrentBalloon();
            _currentRoutine = null;
        }

        /// <summary>
        /// 현재 말풍선을 풀에 반환합니다.
        /// </summary>
        private void ReturnCurrentBalloon()
        {
            if (_currentBalloon != null)
            {
                _pool?.Return(_currentBalloon, this);
            }

            _pool?.ReturnAllByOwner(this);
            _currentBalloon = null;
        }

        /// <summary>
        /// 서비스 오브젝트가 제거될 때 표시 중인 말풍선을 안전하게 회수합니다.
        /// </summary>
        private void OnDestroy()
        {
            Clear();
        }
    }
}
