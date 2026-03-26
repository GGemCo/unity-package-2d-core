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

            IconPoolManager.SetSetIconHandler(new SetIconHandlerQuickSlot());
            DragDropHandler.SetStrategy(new DragDropStrategyQuickSlot());
        }

        protected override void Start()
        {
            base.Start();
            SceneGame.Instance.KeyboardManager.RegisterInputHandler(this);

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
            if (show)
            {
                LoadIcons();
            }
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
                SetIconCount(index, itemUid, itemCount, itemLevel, itemIsLearn, type: type);
            }
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
                    OnKeyDownSkillBySlotIndex(i);
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
                    OnKeyDownSkillBySlotIndex(i);
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
        /// 키보드로 스킬 사용하기
        /// </summary>
        /// <param name="slotIndex"></param>
        private void OnKeyDownSkillBySlotIndex(int slotIndex)
        {
            if (SceneGame == null) return;
            var playerGo = SceneGame.player;
            if (playerGo == null) return;

            // 2) 세이브 데이터에서 스킬 UID 조회
            var quickSlot = SceneGame.Instance.saveDataManager?.QuickSlot;
            if (quickSlot == null) return;

            var all = quickSlot.TryGetEntry(slotIndex, out SaveDataIcon entry);
            if (entry == null || entry.IconType != (int)IconConstants.Type.Skill)
                return;
            int skillUid = entry.Uid;
            if (skillUid <= 0) return;

            // (선택) Count를 “남은 횟수/탄약”처럼 쓰는 정책이면 여기서 체크
            // 무제한 스킬이면 Count를 0으로 저장할 수도 있으니,
            // 프로젝트 정책에 맞춰 조건을 조정하세요.
            // if (iconData.Count <= 0) return;

            // 3) Core 추상화(드라이버)로 스킬 사용 요청
            // Core가 Skill 패키지 타입을 몰라도 되게 GetComponent<Interface>로 찾습니다.
            var driver = playerGo.GetComponent<ICharacterSkillDriver>();
            if (driver == null) return;

            SkillDriverRequest request;
            var targetingProvider = playerGo.GetComponent<IPlayerSkillTargetingProvider>();
            if (targetingProvider != null)
            {
                if (!targetingProvider.TryBuildSkillRequest(
                        playerGo,
                        skillUid,
                        ConfigCommon.SkillTableSource.Player,
                        out var resolvedRequest,
                        out var targetingFailReason))
                {
                    ShowSkillUseFailedMessage(targetingFailReason);
                    return;
                }

                request = resolvedRequest;
            }
            else
            {
                // 타겟팅 제공자가 없으면 기존 전방/자기 위치 fallback을 사용합니다.
                var forward = ResolveForward2D(playerGo);
                request = new SkillDriverRequest(
                    lockedTarget: null,
                    groundPoint: playerGo.transform.position,
                    forward: forward,
                    source: ConfigCommon.SkillTableSource.Player
                );
            }

            var result = driver.TryUseSkill(skillUid, request);
            if (!result.IsStarted)
            {
                ShowSkillUseFailedMessage(result.FailReason);
            }
        }
        private void ShowSkillUseFailedMessage(SkillUseFailReason failReason)
        {
            if (SceneGame == null || SceneGame.systemMessageManager == null)
                return;

            string message = ResolveSkillUseFailedMessage(failReason);
            if (string.IsNullOrEmpty(message))
                return;

            SceneGame.systemMessageManager.ShowMessageWarning(message);
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