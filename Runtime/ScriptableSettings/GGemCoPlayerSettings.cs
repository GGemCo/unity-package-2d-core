using UnityEngine;
using UnityEngine.Serialization;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Player.FileName, menuName = ConfigScriptableObject.Player.MenuName, order = ConfigScriptableObject.Player.Ordering)]
    public class GGemCoPlayerSettings : ScriptableObject
    {
        [FormerlySerializedAs("defaultFacing")]
        [Header("플레이어 디폴트 값 설정")]
        [Tooltip("플레이어의 초기 바라보는 방향 (좌/우/기타)")]
        public CharacterConstants.FacingDirection8 facingDirection8;
        [Tooltip("애니메이션 컨트롤러 타입 (Sprite/Spine 등)")]
        public ConfigCommon.AnimationController animationController;
        [Tooltip("플레이어의 최대 도달 가능 레벨")]
        public int maxLevel;
        [Tooltip("디폴트 캐릭터 크기 (폭, 높이)")]
        public Vector2 size;
        [Tooltip("플레이어의 시작 스케일 값 (1 = 100%)")]
        public float startScale;

        [Header("스탯 기본값")]
        [Tooltip("플레이어의 기본 공격력")]
        public int statAtk;
        [Tooltip("플레이어의 기본 방어력")]
        public int statDef;
        [Tooltip("플레이어의 기본 생명력")]
        public int statHp;
        [Tooltip("플레이어의 기본 마력")]
        public int statMp;
        [Tooltip("애니메이션 1스텝당 이동 거리 (픽셀 단위)")]
        public int statMoveStep;
        [Tooltip("공격 속도 (100 → 1배속)")]
        public int statAttackSpeed;
        [Tooltip("이동 속도 (100 → 1배속)")]
        public int statMoveSpeed;
        [Tooltip("불 속성 저항 (100 → 1배 = 면역)")]
        public int statRegistFire;
        [Tooltip("얼음 속성 저항 (100 → 1배 = 면역)")]
        public int statRegistCold;
        [Tooltip("전기 속성 저항 (100 → 1배 = 면역)")]
        public int statRegistLightning;

        [Header("맵 경계 제한 옵션")]
        [Tooltip("왼쪽 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryLeft = true;
        [Tooltip("오른쪽 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryRight = true;
        [Tooltip("아래쪽(바닥) 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryBottom = true;
        [Tooltip("위쪽(천장) 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryTop = true;

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            facingDirection8   = CharacterConstants.FacingDirection8.Left;
            animationController = ConfigCommon.AnimationController.Sprite;
            startScale = 1;
            statAtk = 100;
            statDef = 100;
            statHp = 100;
            statMp = 100;
            statAttackSpeed = 100;
            statMoveStep = 100;
            statMoveSpeed = 100;
            statRegistFire = 0;
            statRegistCold = 0;
            statRegistLightning = 0;
        }
    }
}
