using System;
using System.Collections.Generic;
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

        [Header("기본 항목 시작값")]
        [Tooltip("장비/패시브의 BASE_* 옵션이 누적되는 플레이어 기본 항목 시작값입니다.")]
        public CharacterBaseAttributeValues baseAttributes;

        [Header("스탯 항목 시작값")]
        [Tooltip("스탯 포인트와 STAT_* 옵션이 누적되는 플레이어 스탯 항목 시작값입니다.")]
        public CharacterGrowthStatValues stats;

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

        [Header("피격 VFX")]
        [Tooltip("플레이어 피격 시 재생할 VFX 설정입니다.")]
        public IncomingHitVfxSettings incomingHitVfx = IncomingHitVfxSettings.CreateDisabled();

        public enum StatPointAcquirePolicy
        {
            [Tooltip("경험치 레벨업으로만 스탯 포인트를 획득합니다.")]
            LevelUpOnly = 0,
            [Tooltip("골드 구매로만 스탯 포인트를 획득합니다.")]
            GoldPurchaseOnly = 1,
            [Tooltip("경험치 레벨업과 골드 구매를 모두 허용합니다.")]
            LevelUpAndGoldPurchase = 2,
        }

        public enum StatPointLevelUpOnInvestPolicy
        {
            [Tooltip("스탯 포인트를 투자해도 플레이어 레벨은 오르지 않습니다.")]
            None = 0,
            [Tooltip("스탯 포인트를 1 투자할 때마다 플레이어 레벨을 1 올립니다.")]
            IncreaseLevelByInvestedPoints = 1,
        }

        public enum StatPointRefundPolicy
        {
            [Tooltip("이미 커밋된 스탯 포인트를 다시 회수할 수 있습니다.")]
            AllowCommittedRefund = 0,
            [Tooltip("이미 커밋된 스탯 포인트는 회수할 수 없고, 이번 드래프트에서 새로 넣은 포인트만 취소할 수 있습니다.")]
            DisallowCommittedRefund = 1,
        }

        [Serializable]
        public struct StatPointBonus
        {
            [Tooltip("포인트 1당 증가 방식 (Flat: 고정값, Percent: % 증가)")]
            public ConfigCommon.CalculateType mode;
            [Tooltip("포인트 1당 증가량. Percent는 '퍼센트 값'을 입력합니다. 예) 1.5 = 1.5%")]
            public float valuePerPoint;
        }

        /// <summary>
        /// 플레이어가 피격될 때 재생할 VFX 설정입니다.
        /// </summary>
        public enum IncomingHitVfxTriggerType
        {
            /// <summary>
            /// 실제 데미지 확정 시점에 피격 VFX를 재생합니다.
            /// </summary>
            OnDamageConfirmed = 0,

            /// <summary>
            /// 애니메이션 이벤트(<c>GGemCoAniEventPlayerHit</c>) 시점에 피격 VFX를 재생합니다.
            /// </summary>
            OnAnimationEventPlayerHit = 1,

            /// <summary>
            /// 데미지 확정과 애니메이션 이벤트 두 경로 모두에서 피격 VFX를 재생합니다.
            /// </summary>
            Both = 2,
        }

        /// <summary>
        /// 플레이어 피격 VFX의 재생 트리거 정책입니다.
        /// </summary>
        [Serializable]
        public struct IncomingHitVfxSettings
        {
            [Tooltip("플레이어 피격 VFX를 사용할지 여부")]
            public bool enabled;

            [Tooltip("재생할 vfx_effect 테이블 Uid")]
            public int vfxUid;

            [Tooltip("VFX를 플레이어를 따라가며 재생할지 여부")]
            public bool followTarget;

            /// <summary>
            /// 피격 VFX가 플레이어를 따라가는 방식을 정의합니다.
            /// </summary>
            /// <remarks>
            /// <see cref="VfxConstants.FollowMode.None"/>이면 기존 <see cref="followTarget"/> 값을 기준으로 Follow 여부를 해석합니다.
            /// </remarks>
            [Tooltip("피격 VFX Follow 모드입니다. None이면 기존 Follow Target 값을 기준으로 해석합니다.")]
            public VfxConstants.FollowMode followMode;

            /// <summary>
            /// 피격 VFX가 Follow 중 유지할 위치 기준 정책입니다.
            /// </summary>
            [Tooltip("피격 VFX Follow 위치 기준입니다. SpawnPosition이면 최초 스폰 위치의 상대 오프셋을 유지합니다.")]
            public VfxConstants.FollowAnchorMode followAnchorMode;

            [Tooltip("피격 VFX의 추가 위치 오프셋(World 기준)")]
            public Vector3 positionOffset;

            [Tooltip("Y 위치 계산 시 캐릭터 높이 자동 반영 여부")]
            public ConfigCommon.PositionYType positionYType;

            [Tooltip("VFX 크기 오버라이드 값 (0 이하이면 테이블 기본값 사용)")]
            public float scaleOverride;

            [Tooltip("VFX 지속 시간 오버라이드 값(초, 0 이하이면 테이블 기본값 사용)")]
            public float durationOverride;

            [Tooltip("Sorting Layer 오버라이드 사용 여부")]
            public bool hasSortingLayerOverride;

            [Tooltip("오버라이드할 Sorting Layer 키")]
            public ConfigSortingLayer.Keys sortingLayerKey;

            [Tooltip("Sorting Order 오버라이드 사용 여부")]
            public bool hasSortingOrderOverride;

            [Tooltip("오버라이드할 Sorting Order 값")]
            public int sortingOrder;

            [Tooltip("연속 피격 시 VFX 재생 최소 간격(초, 0 이하이면 제한 없음)")]
            [Min(0f)]
            public float minIntervalSeconds;

            [Tooltip("피격 VFX 재생 트리거 방식(데미지 확정/애니메이션 이벤트)을 선택합니다.")]
            public IncomingHitVfxTriggerType triggerType;

            /// <summary>
            /// 비활성 기본 설정을 생성합니다.
            /// </summary>
            /// <returns>피격 VFX가 꺼진 기본 설정을 반환합니다.</returns>
            public static IncomingHitVfxSettings CreateDisabled()
            {
                return new IncomingHitVfxSettings
                {
                    enabled = false,
                    vfxUid = 0,
                    followTarget = false,
                    followMode = VfxConstants.FollowMode.None,
                    followAnchorMode = VfxConstants.FollowAnchorMode.FollowTargetOrigin,
                    positionOffset = Vector3.zero,
                    positionYType = ConfigCommon.PositionYType.None,
                    scaleOverride = 0f,
                    durationOverride = 0f,
                    hasSortingLayerOverride = false,
                    sortingLayerKey = ConfigSortingLayer.Keys.CharacterTop,
                    hasSortingOrderOverride = false,
                    sortingOrder = 0,
                    minIntervalSeconds = 0f,
                    triggerType = IncomingHitVfxTriggerType.OnDamageConfirmed
                };
            }

            /// <summary>
            /// 현재 플레이어 피격 VFX 설정에 적용할 실제 Follow 모드를 반환합니다.
            /// </summary>
            /// <returns>런타임 VFX 생성 요청에 적용할 Follow 모드입니다.</returns>
            /// <remarks>
            /// 신규 <see cref="followMode"/> 값이 있으면 우선 사용하고,
            /// 기본값인 <see cref="VfxConstants.FollowMode.None"/>이면 기존 <see cref="followTarget"/> bool 설정을 호환 처리합니다.
            /// </remarks>
            public VfxConstants.FollowMode GetRuntimeFollowMode()
            {
                if (followMode != VfxConstants.FollowMode.None)
                {
                    return followMode;
                }

                return followTarget
                    ? VfxConstants.FollowMode.PositionAndFlip
                    : VfxConstants.FollowMode.None;
            }

            /// <summary>
            /// 현재 플레이어 피격 VFX 설정에 적용할 실제 Follow 위치 기준 정책을 반환합니다.
            /// </summary>
            /// <returns>런타임 VFX 생성 요청에 적용할 Follow 위치 기준 정책입니다.</returns>
            public VfxConstants.FollowAnchorMode GetRuntimeFollowAnchorMode()
            {
                return followAnchorMode;
            }
        }

        [Header("스탯 포인트")]
        [Tooltip("스탯 포인트 리셋 비용")]
        public int statPointResetCost;
        [Tooltip("스탯 포인트 획득 경로 정책입니다.")]
        public StatPointAcquirePolicy statPointAcquirePolicy = StatPointAcquirePolicy.LevelUpOnly;
        [Tooltip("스탯 포인트 투자 시 플레이어 레벨 증가 정책입니다.")]
        public StatPointLevelUpOnInvestPolicy statPointLevelUpOnInvestPolicy = StatPointLevelUpOnInvestPolicy.None;
        [Tooltip("이미 적용된 스탯 포인트를 다시 회수할 수 있는지 결정합니다.")]
        public StatPointRefundPolicy statPointRefundPolicy = StatPointRefundPolicy.AllowCommittedRefund;
        [Tooltip("GoldPurchaseOnly 정책에서는 런타임에서 Gold로 고정됩니다. LevelUpAndGoldPurchase 정책의 직접 구매 버튼에서 사용할 재화 타입입니다.")]
        public CurrencyConstants.Type statPointPurchaseCurrencyType = CurrencyConstants.Type.Gold;
        [Tooltip("LevelUpAndGoldPurchase의 직접 구매 버튼 기본 가격입니다. GoldPurchaseOnly에서는 exp 테이블의 NeedStatPointGold 값을 우선 사용하고, 값이 없을 때 fallback으로 사용합니다.")]
        [Min(0)]
        public int statPointPurchaseCurrencyValue = 0;
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

        [Header("Element Gauge")]
        [Tooltip("플레이어에게 적용할 속성 게이지 규칙 목록입니다. 비어 있으면 런타임 기본값을 사용합니다.")]
        public List<ElementGaugeRuleDefinition> elementGaugeRules = new();

        [Header("Passive Temp HP")]
        [SerializeField]
        [Tooltip("패시브 스킬로 임시 HP 증가/변할 때 현재 임시 HP를 어떻게 보정할지 결정합니다.")]
        private PassiveTempHpApplyPolicy passiveTempHpApplyPolicy = PassiveTempHpApplyPolicy.KeepCurrent;

        public PassiveTempHpApplyPolicy PassiveTempHpApplyPolicy => passiveTempHpApplyPolicy;

        [Header("사망")]
        [Tooltip("사망 연출 사용 여부")]
        public bool useCutsceneDie;
        [Tooltip("사망 연출 Uid")]
        public int cutsceneUidDie;
        
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
            baseAttributes = new CharacterBaseAttributeValues
            {
                atk = 100,
                def = 100,
                hp = 100,
                mp = 100,
                stamina = 100,
                superArmor = 0,
                moveSpeed = 100,
                attackSpeed = 100,
                criticalDamage = 100,
                criticalProbability = 0,
                registFire = 0,
                registCold = 0,
                registLightning = 0,
                registPoison = 0,
                moveStep = 100,
            };

            stats = new CharacterGrowthStatValues
            {
                atk = 100,
                def = 100,
                hp = 100,
                mp = 100,
                stamina = 100,
            };

            statPointAcquirePolicy = StatPointAcquirePolicy.LevelUpOnly;
            statPointLevelUpOnInvestPolicy = StatPointLevelUpOnInvestPolicy.None;
            statPointRefundPolicy = StatPointRefundPolicy.AllowCommittedRefund;
            statPointPurchaseCurrencyType = CurrencyConstants.Type.Gold;
            statPointPurchaseCurrencyValue = 0;

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
            incomingHitVfx = IncomingHitVfxSettings.CreateDisabled();

            elementGaugeRules = ElementGaugeRuleDefinition.CreateDefaultPlayerRules();

        }
    }
}
