using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 윈도우
    /// </summary>
    public class UIWindowQuickSlot : UIWindow, IInputHandler
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("단축키에 사용할 숫자 UI Image")]
        public Image[] iconHotKey;
        public int Priority => 1;

        private readonly Dictionary<KeyCode, int> _indexByKeyCode = new Dictionary<KeyCode, int>
        {
            { KeyCode.Alpha1, 0 },
            { KeyCode.Alpha2, 1 },
            { KeyCode.Alpha3, 2 },
            { KeyCode.Alpha4, 3 },
        };
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.QuickSlot;
            base.Awake();
            RegisterQuickSlotProviders();

            IconPoolManager.SetSetIconHandler(new SetIconHandlerQuickSlot());
            DragDropHandler.SetStrategy(new DragDropStrategyQuickSlot());
        }

        protected override void Start()
        {
            base.Start();
            SceneGame.Instance.KeyboardManager.RegisterInputHandler(this);
            LoadIcons();
        }


        /// <summary>
        /// 퀵슬롯 아이콘 제공자 등록(Core 기본: Item Provider)
        /// - Skill Provider 는 Skill 패키지에서 별도로 등록한다.
        /// </summary>
        private void RegisterQuickSlotProviders()
        {
            // Core 기본 아이템 Provider
            QuickSlotContentProviderRegistry.Register(new QuickSlotItemContentProvider());
        }

        /// <summary>
        /// 세이브 엔트리를 슬롯 아이콘에 반영한다(세이브를 다시 쓰지 않도록 직접 아이콘에 적용).
        /// </summary>
        private void ApplyEntryToSlot(int slotIndex, QuickSlotContentKind kind, int iconUid, int iconCount, int iconLevel, bool iconIsLearn, long iconInstanceId)
        {
            var icon = GetIconByIndex(slotIndex);
            if (icon == null) return;

            if (kind == QuickSlotContentKind.None || iconUid <= 0 || iconCount <= 0)
            {
                if (icon is UIIconQuickSlot qs)
                    qs.ClearEntry();
                else
                    icon.ChangeInfoByUid(0, 0, 0, false, 0, 0);
                return;
            }

            // QuickSlot 전용 아이콘이면 ProviderRegistry 기반으로 스킬/아이템 모두 표시
            if (icon is UIIconQuickSlot quickSlotIcon)
            {
                quickSlotIcon.ApplyEntry(kind, iconUid, iconCount, iconLevel, iconIsLearn, iconInstanceId);
                return;
            }

            // 구버전 아이콘 프리팹(스킬 전용 등) 호환: 최소한 데이터는 반영
            icon.ChangeInfoByUid(iconUid, iconCount, iconLevel, iconIsLearn, 0, iconInstanceId);
        }
        /// <summary>
        /// 저장되어있는 스킬 정보로 아이콘 셋팅하기
        /// 스킬창이 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.Instance.saveDataManager.QuickSlot.GetAllDatas();
            if (datas == null) return;
            for (int index = 0; index < maxCountIcon; index++)
            {
                if (index >= icons.Length) continue;
                // 단축키 이미지 위치 설정
                if (iconHotKey[index])
                {
                    iconHotKey[index].transform.SetParent(slots[index].transform);
                    iconHotKey[index].transform.localPosition = new Vector3(-slotSize.x / 2f, slotSize.y / 2f, 0);
                }

                var icon = icons[index];
                if (icon == null) continue;
                if (datas.TryGetValue(index, out var entry) && entry != null && entry.Uid > 0 && entry.Count > 0)
                {
                    var kind = (QuickSlotContentKind)entry.Kind;
                    ApplyEntryToSlot(index, kind, entry.Uid, entry.Count, entry.Level, entry.IsLearned, entry.InstanceId);
                }
                else
                {
                    // 비어있는 슬롯
                    ApplyEntryToSlot(index, QuickSlotContentKind.None, 0, 0, 0, false, 0);
                }
            }
        }
        protected void OnDisable()
        {
            if (SceneGame.Instance == null) return;
            SceneGame.Instance.KeyboardManager.RemoveInputHandler(this);
        }

        public bool HandleInput()
        {
            
#if GGEMCO_USE_OLD_INPUT
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OnKeyDownSkill(KeyCode.Alpha1);
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                OnKeyDownSkill(KeyCode.Alpha2);
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                OnKeyDownSkill(KeyCode.Alpha3);
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                OnKeyDownSkill(KeyCode.Alpha4);
                return true;
            }
#elif GGEMCO_USE_NEW_INPUT
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                OnKeyDownSkill(KeyCode.Alpha1);
                return true;
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                OnKeyDownSkill(KeyCode.Alpha2);
                return true;
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                OnKeyDownSkill(KeyCode.Alpha3);
                return true;
            }
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                OnKeyDownSkill(KeyCode.Alpha4);
                return true;
            }
#endif

            return false;
        }
        /// <summary>
        /// 키보드로 스킬 사용하기
        /// </summary>
        /// <param name="keyCode"></param>
        private void OnKeyDownSkill(KeyCode keyCode)
        {
            if (SceneGame == null) return;
            var playerGo = SceneGame.player;
            if (playerGo == null) return;

            // 1) 어떤 슬롯인지 결정
            if (!_indexByKeyCode.TryGetValue(keyCode, out int slotIndex))
                return;

            // 2) 세이브 데이터에서 스킬 UID 조회
            var quickSlot = SceneGame.Instance.saveDataManager?.QuickSlot;
            if (quickSlot == null) return;

            var all = quickSlot.TryGetEntry(slotIndex, out SaveDataIcon entry);
            if (entry == null || entry.Kind != (int)QuickSlotContentKind.Skill)
                return;
            int skillUid = entry.Uid;
            if (skillUid <= 0) return;

            // (선택) Count를 “남은 횟수/탄약”처럼 쓰는 정책이면 여기서 체크
            // 무제한 스킬이면 Count를 0으로 저장할 수도 있으니,
            // 프로젝트 정책에 맞춰 조건을 조정하세요.
            // if (iconData.Count <= 0) return;

            // 3) Core 추상화(드라이버)로 스킬 사용 요청
            // Core가 Skill 패키지 타입을 몰라도 되게 GetComponent<Interface>로 찾습니다.
            var driver = playerGo.GetComponent<IMonsterSkillDriver>();
            if (driver == null) return;

            if (driver.IsSkillBusy) return;

            // 4) 최소 타겟 컨텍스트 구성 (현재 Core에는 “플레이어 락온/조준” 시스템이 명확히 없으므로,
            //    우선은 forward + 자기 위치 기반으로 전달)
            var forward = ResolveForward2D(playerGo);
            var target = new MonsterSkillTarget(
                lockedTarget: null,
                groundPoint: playerGo.transform.position,
                forward: forward
            );

            driver.TryUseSkill(skillUid, target);
        }
        private static Vector2 ResolveForward2D(GameObject caster)
        {
            // CharacterBase가 있으면 CurrentFacing 기반으로 방향을 안정적으로 만들 수 있습니다.
            var cb = caster.GetComponent<CharacterBase>();
            if (cb != null)
            {
                return CharacterConstants.FacingToVector2(cb.CurrentFacing);
            }

            // fallback: transform.right(2D 프로젝트에서 오른쪽이 정방향인 경우)
            var r = caster.transform.right;
            var v = new Vector2(r.x, r.y);
            return v.sqrMagnitude < 1e-6f ? Vector2.right : v.normalized;
        }

        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;
            
            float time = SceneGame.Instance.uIIconCoolTimeManager.GetCurrentCoolTime(uid, icon.uid);
            if (time > 0)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("Action_CannotUseDuringCooldown");//"쿨타임 중에는 사용할 수 없습니다."
                return;
            }
            // 스킬 창이 열려있을때는 해제 하기
            // todo. 정리 필요.
            // if (!uiWindowSkill || !uiWindowSkill.IsOpen()) return;
            DetachIcon(icon.slotIndex);
        }
    }
}