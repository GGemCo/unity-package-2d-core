using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 일반 공격 애니메이션 이벤트에서 사용하는 실제 공격 판정 영역을 전달합니다.
    /// </summary>
    /// <remarks>
    /// 이 Collider는 피해 판정과 플레이어 자동 이동 정지 후보 수집에만 사용합니다.
    /// 몬스터 선공 감지, 기본 공격 시작 거리, 선호 전투 거리와 추적 한계는 별도 전투 범위 프로필이 담당합니다.
    /// </remarks>
    public class CharacterAttackRange : MonoBehaviour
    {
        public CharacterBase target;

        /// <summary>
        /// 공격 판정 영역을 소유 캐릭터와 연결하고 로컬 Transform을 초기화합니다.
        /// </summary>
        /// <param name="character">이 공격 판정 영역을 소유한 캐릭터입니다.</param>
        public void Initialize(CharacterBase character)
        {
            target = character;
            transform.SetParent(target.gameObject.transform);
            transform.SetParent(gameObject.transform);
            tag = character.tag;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 실제 공격 판정 영역에 Collider가 진입한 사실을 소유 캐릭터에게 전달합니다.
        /// </summary>
        /// <param name="collision">진입한 Collider입니다.</param>
        protected void OnTriggerEnter2D(Collider2D collision)
        {
            if (!target) return;
            target.OnTriggerEnterByAttackRange(collision);
        }

        /// <summary>
        /// 실제 공격 판정 영역에서 Collider가 이탈한 사실을 소유 캐릭터에게 전달합니다.
        /// </summary>
        /// <param name="collision">이탈한 Collider입니다.</param>
        protected void OnTriggerExit2D(Collider2D collision)
        {
            if (!target) return;
            target.OnTriggerExitByAttackRange(collision);
        }
    }
}
