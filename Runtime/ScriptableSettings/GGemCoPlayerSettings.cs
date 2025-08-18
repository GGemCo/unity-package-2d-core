using UnityEngine;
using UnityEngine.Serialization;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Player.FileName, menuName = ConfigScriptableObject.Player.MenuName, order = ConfigScriptableObject.Player.Ordering)]
    public class GGemCoPlayerSettings :ScriptableObject
    {
        [FormerlySerializedAs("defaultFacing")]
        [Header("플레이어 디폴트 값 설정")] 
        [Header("플레이어 디폴트 방향")] public CharacterConstants.FacingDirection8 facingDirection8;
        [Header("애니메이션 컨트롤러")] public ConfigCommon.AnimationController animationController;
        [Header("플레이어 최대 레벨")] public int maxLevel;
        [Header("시작 scale")] public float startScale;
        [Header("기본 공격력")] public int statAtk;
        [Header("기본 방어력")] public int statDef;
        [Header("기본 생명력")] public int statHp;
        [Header("기본 마력")] public int statMp;
        [Header("애니메이션 스텝 당 이동거리(픽셀)")] public int statMoveStep;
        [Header("공격 속도(100->1)")] public int statAttackSpeed;
        [Header("이동 속도(100->1)")] public int statMoveSpeed;
        [Header("불 속성 저항(100->1)")] public int statRegistFire;
        [Header("얼음 속성 저항(100->1)")] public int statRegistCold;
        [Header("전기 속성 저항(100->1)")] public int statRegistLightning;
        [Header("사정거리")] public Vector2 rangeAttack;
        [Header("충돌 박스 OffSet")] public Vector2 rangeColliderOffset;
        [Header("충돌 박스 Size")] public Vector2 rangeCollider;
        [Header("HitArea Offset")] public Vector2 rangeHitAreaOffset;
        [Header("HitArea Size")] public Vector2 rangeHitAreaSize;
        [Header("디폴트 캐릭터 크기")] public Vector2 size;
        [Header("Jump 최고 높이")] public float jumpHeight;
        [Header("Jump 속도. 지면→정점까지 걸리는 시간(초)")] public float jumpSpeed;
        
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            facingDirection8 = CharacterConstants.FacingDirection8.Left;
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
            jumpHeight = 10;
            jumpSpeed = 1;
        }
    }
}