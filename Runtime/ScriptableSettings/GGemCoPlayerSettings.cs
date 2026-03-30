using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Player.FileName, menuName = ConfigScriptableObject.Player.MenuName, order = ConfigScriptableObject.Player.Ordering)]
    public class GGemCoPlayerSettings : ScriptableObject
    {
        // =========================
        // Resource Start Value (시작 자원)
        // =========================
        public enum ResourceStartMode
        {
            /// <summary>최대치로 시작</summary>
            Max = 0,
            /// <summary>최대치의 %로 시작 (0~1)</summary>
            PercentOfMax = 1,
            /// <summary>고정 값으로 시작</summary>
            FixedValue = 2,
        }

        [Serializable]
        public struct ResourceStartSetting
        {
            [Tooltip("시작 자원 계산 방식")]
            public ResourceStartMode mode;

            [Tooltip("PercentOfMax 일 때 사용 (0~1)")]
            [Range(0f, 1f)]
            public float percentOfMax;

            [Tooltip("FixedValue 일 때 사용")]
            public long fixedValue;

            [Tooltip("최대치(및 0) 범위로 클램프할지 여부")]
            public bool clampToRange;

            public static ResourceStartSetting CreateMax()
            {
                return new ResourceStartSetting
                {
                    mode = ResourceStartMode.Max,
                    percentOfMax = 1f,
                    fixedValue = 0,
                    clampToRange = true
                };
            }

            public long Evaluate(long maxValue)
            {
                long value;
                switch (mode)
                {
                    case ResourceStartMode.FixedValue:
                        value = fixedValue;
                        break;

                    case ResourceStartMode.PercentOfMax:
                        value = (long)Math.Round(maxValue * Mathf.Clamp01(percentOfMax), MidpointRounding.AwayFromZero);
                        break;

                    case ResourceStartMode.Max:
                    default:
                        value = maxValue;
                        break;
                }

                if (!clampToRange) return value;
                if (value < 0) return 0;
                return value > maxValue ? maxValue : value;
            }
        }

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
        [Tooltip("플레이어의 기본 스테미나")]
        public int statStamina;
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
        [Tooltip("독 속성 저항 (100 → 1배 = 면역)")]
        public int statRegistPoison;

        [Header("시작 자원 값")]
        [Tooltip("게임 시작 시 현재 HP 값 설정")]
        public ResourceStartSetting startHp = ResourceStartSetting.CreateMax();
        [Tooltip("게임 시작 시 현재 MP 값 설정")]
        public ResourceStartSetting startMp = ResourceStartSetting.CreateMax();
        [Tooltip("게임 시작 시 현재 Stamina 값 설정")]
        public ResourceStartSetting startStamina = ResourceStartSetting.CreateMax();

        [Header("최대치 변경 시 현재 자원 보정 정책")]
        [Tooltip("최대 HP가 변할 때 현재 HP를 어떻게 보정할지 결정합니다.")]
        public CharacterConstants.ResourceMaxChangePolicy hpMaxChangePolicy = CharacterConstants.ResourceMaxChangePolicy.AddDelta;

        [Tooltip("최대 MP가 변할 때 현재 MP를 어떻게 보정할지 결정합니다.")]
        public CharacterConstants.ResourceMaxChangePolicy mpMaxChangePolicy = CharacterConstants.ResourceMaxChangePolicy.PreserveRatio;

        [Tooltip("최대 Stamina가 변할 때 현재 Stamina를 어떻게 보정할지 결정합니다.")]
        public CharacterConstants.ResourceMaxChangePolicy staminaMaxChangePolicy = CharacterConstants.ResourceMaxChangePolicy.KeepCurrent;

        [Header("소모형 추가 최대 HP(아이템 보너스) - 하트 단위")]
        [Tooltip("임시(추가) HP의 '조각 1개'가 의미하는 HP 값입니다. UI 하트 조각 규칙과 동일한 값을 사용하세요.")]
        public int itemBonusTempHpPerPiece = 100;

        [Tooltip("임시(추가) HP의 '하트 1개'가 가지는 조각 수입니다. UI 하트 조각 규칙과 동일한 값을 사용하세요.")]
        public int itemBonusTempPiecesPerHeart = 4;

        [Header("맵 경계 제한 옵션")]
        [Tooltip("왼쪽 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryLeft = true;
        [Tooltip("오른쪽 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryRight = true;
        [Tooltip("아래쪽(바닥) 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryBottom = true;
        [Tooltip("위쪽(천장) 경계를 벗어날 수 없도록 제한합니다.")]
        public bool limitBoundaryTop = true;


        [Header("Hit Stop")]
        [Tooltip("자신이 타격을 성공시켰을 때 적용할 기본 경직 시간(초)")]
        [Min(0f)] public float defaultSelfHitStopSeconds = 0.03f;
        [Tooltip("피격 대상에게 적용할 기본 경직 시간(초)")]
        [Min(0f)] public float defaultReceiveHitStopSeconds = 0.05f;
        [Tooltip("경직 중 애니메이션을 현재 프레임에서 멈출지 여부")]
        public bool hitStopPauseAnimation = true;
        [Tooltip("경직 중 Rigidbody2D 물리를 멈출지 여부")]
        public bool hitStopFreezePhysics = true;
        [Tooltip("경직 중 DontControl 상태를 적용할지 여부")]
        public bool hitStopLockControl = true;
        [Tooltip("경직 중 DontMove 상태를 적용할지 여부")]
        public bool hitStopLockMovement = true;

        [Header("Sprite White Overlay")]
        [Tooltip("피격 시 Sprite White Overlay 효과를 사용할지 여부")]
        public bool useSpriteWhiteOverlay;
        [Tooltip("Sprite White Overlay에서 사용할 기본 호환 Material. 비워두면 기존 Material을 유지합니다.")]
        public Material spriteWhiteOverlayMaterial;
        [Tooltip("Sprite White Overlay 효과에 사용할 색상")]
        public Color spriteWhiteOverlayColor = Color.white;
        [Tooltip("피격 시 Sprite White Overlay 유지 시간(초)")]
        [Min(0.01f)]
        public float spriteWhiteOverlayFlashDuration = 0.08f;

        [Serializable]
        public struct StatPointBonus
        {
            [Tooltip("포인트 1당 증가 방식 (Flat: 고정값, Percent: % 증가)")]
            public ConfigCommon.CalculateType mode;
            [Tooltip("포인트 1당 증가량. Percent는 '퍼센트 값'을 입력합니다. 예) 1.5 = 1.5%")]
            public float valuePerPoint;
        }

        [Header("스탯 포인트")]
        [Tooltip("새 게임 시작 시 지급되는 스탯 포인트")]
        public int statPointInitial;
        [Tooltip("레벨업 1회당 지급되는 스탯 포인트")]
        public int statPointPerLevel;

        [Tooltip("공격력 포인트 1당 증가량")]
        public StatPointBonus statPointAtk;
        [Tooltip("방어력 포인트 1당 증가량")]
        public StatPointBonus statPointDef;
        [Tooltip("체력 포인트 1당 증가량")]
        public StatPointBonus statPointHp;
        [Tooltip("마력 포인트 1당 증가량")]
        public StatPointBonus statPointMp;
        [Tooltip("스테미나 포인트 1당 증가량")]
        public StatPointBonus statPointStamina;

        [Header("Passive Temp HP")]
        [SerializeField]
        private PassiveTempHpApplyPolicy passiveTempHpApplyPolicy = PassiveTempHpApplyPolicy.KeepCurrent;

        public PassiveTempHpApplyPolicy PassiveTempHpApplyPolicy => passiveTempHpApplyPolicy;
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            facingDirection8   = CharacterConstants.FacingDirection8.Left;
            defaultSelfHitStopSeconds = 0.03f;
            defaultReceiveHitStopSeconds = 0.05f;
            hitStopPauseAnimation = true;
            hitStopFreezePhysics = true;
            hitStopLockControl = true;
            hitStopLockMovement = true;
            animationController = ConfigCommon.AnimationController.Sprite;
            startScale = 1;
            statAtk = 100;
            statDef = 100;
            statHp = 100;
            statMp = 100;
            statStamina = 100;
            statAttackSpeed = 100;
            statMoveStep = 100;
            statMoveSpeed = 100;
            statRegistFire = 0;
            statRegistCold = 0;
            statRegistLightning = 0;

            statPointInitial = 0;
            statPointPerLevel = 0;

            statPointAtk = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 1f };
            statPointDef = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 1f };
            statPointHp = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 10f };
            statPointMp = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 5f };
            statPointStamina = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 5f };

            itemBonusTempHpPerPiece = 100;
            itemBonusTempPiecesPerHeart = 4;

            useSpriteWhiteOverlay = false;
            spriteWhiteOverlayMaterial = null;
            spriteWhiteOverlayColor = Color.white;
            spriteWhiteOverlayFlashDuration = 0.08f;

        }
    }
}
