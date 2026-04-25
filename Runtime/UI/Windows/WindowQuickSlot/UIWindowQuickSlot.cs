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

        private static readonly KeyCode[] HotKeyCodes =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
        };
#if GGEMCO_USE_NEW_INPUT
        private static readonly System.Func<Keyboard, bool>[] HotKeyPressedChecks =
        {
            keyboard => keyboard != null && keyboard.digit1Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit2Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit3Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit4Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit5Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit6Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit7Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit8Key.wasPressedThisFrame,
            keyboard => keyboard != null && keyboard.digit9Key.wasPressedThisFrame,
        };
#endif
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.QuickSlot;
            base.Awake();

            // Core 기본 등록: 인벤토리 아이템 드래그와 기본 사용 핸들러를 연결합니다.
            QuickSlotDragStrategyRegistry.Register(UIWindowConstants.WindowUid.Inventory,
                new DragDropStrategyQuickSlotFromInventory());
            QuickSlotUseHandlerRegistry.Register(IconConstants.Type.Skill, new QuickSlotSkillUseHandler());
            QuickSlotUseHandlerRegistry.Register(IconConstants.Type.Item, new QuickSlotItemUseHandler());

            IconPoolManager.SetSetIconHandler(new SetIconHandlerQuickSlot());
            DragDropHandler.SetStrategy(new DragDropStrategyQuickSlot());
        }

        protected override void Start()
        {
            base.Start();

            // DefaultActive 값이 True이면 OnShow는 호출되지 않으므로 여기서 LoadIcons 호출
            var info = TableLoaderManager.Instance.GetWindowData((int)uid);
            if (info != null && info.DefaultActive)
            {
                LoadIcons();
            }
        }
        public override void OnShow(bool show)
        {
            if (SceneGame == null || TableLoaderManager.Instance == null) return;
            base.OnShow(show);
            if (show)
            {
                LoadIcons();
            }
        }

        /// <summary>
        /// 저장된 퀵슬롯 엔트리(스킬/아이템/패시브)를 아이콘으로 복원합니다.
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
                if (index < iconHotKey.Length && iconHotKey[index])
                {
                    iconHotKey[index].transform.SetParent(slots[index].transform);
                    iconHotKey[index].transform.localPosition = new Vector3(-slotSize.x / 2f, slotSize.y / 2f, 0);
                }

                var icon = icons[index];
                if (icon == null) continue;
                UIIconQuickSlot uiIconQuickSlot = icon.GetComponent<UIIconQuickSlot>();
                if (uiIconQuickSlot == null) continue;
                if (!datas.TryGetValue(index, out var info))
                {
                    uiIconQuickSlot.ClearIconInfos();
                    continue;
                }
                SaveDataIcon structInventoryIcon = info;
                int itemUid = structInventoryIcon.Uid;
                int itemCount = structInventoryIcon.Count;
                int itemLevel = structInventoryIcon.Level;
                bool itemIsLearn = structInventoryIcon.IsLearned;
                IconConstants.Type type = (IconConstants.Type)structInventoryIcon.IconType;
                if (itemUid <= 0 || itemCount <= 0)
                {
                    uiIconQuickSlot.ClearIconInfos();
                    continue;
                }
                SetIconCount(index, itemUid, itemCount, itemLevel, itemIsLearn, structInventoryIcon.InstanceId, type);
            }
        }

        protected void OnEnable()
        {
            if (SceneGame.Instance == null) return;
            SceneGame.Instance.KeyboardManager.RegisterInputHandler(this);
        }
        protected void OnDisable()
        {
            if (SceneGame.Instance == null) return;
            SceneGame.Instance.KeyboardManager.RemoveInputHandler(this);
        }
        public bool HandleInput()
        {
            int inputCount = GetProcessableHotKeyCount();
            if (inputCount <= 0)
                return false;

#if GGEMCO_USE_OLD_INPUT
            for (int i = 0; i < inputCount; i++)
            {
                if (Input.GetKeyDown(HotKeyCodes[i]))
                {
                    OnKeyDownQuickSlotBySlotIndex(i);
                    return true;
                }
            }
#elif GGEMCO_USE_NEW_INPUT
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            for (int i = 0; i < inputCount; i++)
            {
                if (HotKeyPressedChecks[i](keyboard))
                {
                    OnKeyDownQuickSlotBySlotIndex(i);
                    return true;
                }
            }
#endif

            return false;
        }

        private int GetProcessableHotKeyCount()
        {
            int maxByUi = Mathf.Max(0, maxCountIcon);
            int maxByIcons = icons?.Length ?? 0;
            int maxByHotKeys = HotKeyCodes.Length;

#if GGEMCO_USE_NEW_INPUT
            int maxByInputSystem = HotKeyPressedChecks.Length;
            return Mathf.Min(maxByUi, maxByIcons, maxByHotKeys, maxByInputSystem);
#else
            return Mathf.Min(maxByUi, maxByIcons, maxByHotKeys);
#endif
        }
        
        /// <summary>
        /// 단축키 입력 시 현재 슬롯 엔트리를 타입별 핸들러에 위임합니다.
        /// 등록 가능 여부와 실제 사용 동작을 분리해 두면 아이템/스킬/패시브 확장이 쉬워집니다.
        /// </summary>
        private void OnKeyDownQuickSlotBySlotIndex(int slotIndex)
        {
            if (SceneGame == null)
                return;

            var quickSlot = SceneGame.Instance.saveDataManager?.QuickSlot;
            if (quickSlot == null)
                return;

            if (!quickSlot.TryGetEntry(slotIndex, out SaveDataIcon entry) || entry == null || entry.Uid <= 0)
                return;

            var iconType = (IconConstants.Type)entry.IconType;
            if (!QuickSlotUseHandlerRegistry.TryGet(iconType, out var handler))
                return;

            if (!handler.CanUse(this, entry, out var failMessageKey))
            {
                ShowQuickSlotUseFailedMessage(failMessageKey);
                return;
            }

            if (!handler.Use(this, entry, out failMessageKey))
            {
                ShowQuickSlotUseFailedMessage(failMessageKey);
            }
        }

        /// <summary>
        /// 액티브 스킬 퀵슬롯 실행 본문입니다.
        /// Skill 패키지 직접 참조 없이 Core 인터페이스만 사용합니다.
        /// </summary>
        public bool TryUseQuickSlotSkill(SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;
            if (SceneGame == null)
                return false;

            var playerGo = SceneGame.player;
            if (playerGo == null || entry == null || entry.Uid <= 0)
                return false;

            var driver = playerGo.GetComponent<ICharacterSkillDriver>();
            if (driver == null)
                return false;

            SkillDriverRequest request;
            var targetingProvider = playerGo.GetComponent<IPlayerSkillTargetingProvider>();
            if (targetingProvider != null)
            {
                if (!targetingProvider.TryBuildSkillRequest(
                        playerGo,
                        entry.Uid,
                        ConfigCommon.SkillTableSource.Player,
                        out var resolvedRequest,
                        out var targetingFailReason))
                {
                    failMessageKey = ResolveSkillUseFailedMessage(targetingFailReason);
                    return false;
                }

                request = resolvedRequest;
            }
            else
            {
                // 타겟팅 제공자가 없으면 기존 전방/자기 위치 fallback 을 사용합니다.
                var forward = ResolveForward2D(playerGo);
                request = new SkillDriverRequest(
                    lockedTarget: null,
                    groundPoint: playerGo.transform.position,
                    forward: forward,
                    source: ConfigCommon.SkillTableSource.Player
                );
            }

            var result = driver.TryUseSkill(entry.Uid, request);
            if (result.IsStarted)
                return true;

            failMessageKey = ResolveSkillUseFailedMessage(result.FailReason);
            return false;
        }

        /// <summary>
        /// 소비 아이템 퀵슬롯은 인벤토리 원본 슬롯을 찾아 사용합니다.
        /// 사용 후에는 퀵슬롯 count 를 실제 인벤토리 상태와 다시 맞춰 줍니다.
        /// </summary>
        public bool CanUseQuickSlotItem(SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;

            var inventory = SceneGame?.saveDataManager?.Inventory;
            if (inventory == null || entry == null || entry.Uid <= 0)
                return false;

            if (TableLoaderManager.Instance?.TableItemUse == null ||
                !TableLoaderManager.Instance.TableItemUse.TryGetByItemUid(entry.Uid, out _))
            {
                failMessageKey = "Item_NotUsable";
                return false;
            }

            if (!inventory.TryFindUsableSlot(entry.Uid, entry.InstanceId, out _))
            {
                failMessageKey = "Item_NoUsableCount";
                return false;
            }

            return true;
        }

        public bool TryUseQuickSlotItem(SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;
            if (!CanUseQuickSlotItem(entry, out failMessageKey))
            {
                SyncQuickSlotItemEntry(entry);
                return false;
            }

            float currentCd = SceneGame.uIIconCoolTimeManager.GetCurrentCoolTime(uid, entry.Uid);
            if (currentCd > 0)
            {
                failMessageKey = "Action_CannotUseDuringCooldown";
                return false;
            }

            var inventory = SceneGame.saveDataManager.Inventory;
            if (!inventory.TryFindUsableSlot(entry.Uid, entry.InstanceId, out var inventorySlotIndex))
            {
                failMessageKey = "Item_NoUsableCount";
                SyncQuickSlotItemEntry(entry);
                return false;
            }

            var useResult = ItemUseService.TryUseInventoryItem(SceneGame, inventory, inventorySlotIndex, out var cooldown);

            // 인벤토리 윈도우가 열려 있지 않아도 데이터 반영 결과는 즉시 동기화합니다.
            var inventoryWindow = SceneGame.uIWindowManager
                .GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            inventoryWindow?.SetIcons(useResult);

            SyncQuickSlotItemEntry(entry);

            if (!useResult.IsSuccess())
                return false;

            if (cooldown > 0)
            {
                var icon = GetIconByIndex(entry.SlotIndex);
                icon?.PlayCoolTime(cooldown);
            }

            return true;
        }

        /// <summary>
        /// 인벤토리 변경 이후 퀵슬롯 항목이 실제로 남아 있는지 다시 확인하고
        /// count/instanceId 를 최신 상태로 맞춥니다.
        /// </summary>
        private void SyncQuickSlotItemEntry(SaveDataIcon entry)
        {
            if (entry == null)
                return;

            var inventory = SceneGame?.saveDataManager?.Inventory;
            if (inventory == null)
                return;

            if (!inventory.TryFindUsableSlot(entry.Uid, entry.InstanceId, out var inventorySlotIndex) ||
                !inventory.ItemCounts.TryGetValue(inventorySlotIndex, out var inventoryEntry) ||
                inventoryEntry == null ||
                inventoryEntry.Count <= 0)
            {
                DetachIcon(entry.SlotIndex);
                return;
            }

            SetIconCount(entry.SlotIndex, inventoryEntry.Uid, inventoryEntry.Count, instanceId: inventoryEntry.InstanceId,
                type: IconConstants.Type.Item);
        }

        private void ShowQuickSlotUseFailedMessage(string failMessageKey)
        {
            if (SceneGame == null || SceneGame.systemMessageManager == null || string.IsNullOrEmpty(failMessageKey))
                return;

            SceneGame.systemMessageManager.ShowMessageWarning(failMessageKey);
        }

        private static string ResolveSkillUseFailedMessage(SkillUseFailReason failReason)
        {
            switch (failReason)
            {
                case SkillUseFailReason.NoTarget:
                    return "Skill_NoTarget"; // 타겟이 필요합니다.
                case SkillUseFailReason.OutOfRange:
                    return "Skill_TargetOutOfRange"; // 사거리 안에 타겟이 없습니다.
                case SkillUseFailReason.Cooldown:
                    return "Action_CannotUseDuringCooldown";
                case SkillUseFailReason.Busy:
                    return "Skill_AlreadyInUse"; // 다른 스킬을 사용 중입니다.
                case SkillUseFailReason.InsufficientMp:
                    return "Skill_NotEnoughMp"; // 마나가 부족합니다.
                case SkillUseFailReason.ControlLocked:
                    return null; // 제어 잠금 상태(예: 탈진)에서는 별도 메시지를 노출하지 않습니다.
                default:
                    return null;
            }
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
