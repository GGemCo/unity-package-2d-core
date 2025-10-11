using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션용 퀵슬롯 윈도우
    /// </summary>
    public class UIWindowQuickSlotSimulation : UIWindow, IInputHandler
    {
        public int Priority => 1;
        
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("단축키에 사용할 숫자 UI Image")]
        public Image[] iconHotKey;
        
        private UIWindowSkill _uiWindowSkill;
        private UIWindowInventory _uiWindowInventory;
        private Player _player;

        private readonly Dictionary<KeyCode, int> _indexByKeyCode = new Dictionary<KeyCode, int>
        {
            { KeyCode.Alpha1, 0 },
            { KeyCode.Alpha2, 1 },
            { KeyCode.Alpha3, 2 },
            { KeyCode.Alpha4, 3 },
            { KeyCode.Alpha5, 4 },
            { KeyCode.Alpha6, 5 },
            { KeyCode.Alpha7, 6 },
            { KeyCode.Alpha8, 7 },
            { KeyCode.Alpha9, 8 },
        };
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.QuickSlotSimulation;
            if (TableLoaderManager.Instance == null) return;
            base.Awake();
            IconPoolManager.SetSetIconHandler(new SetIconHandlerQuickSlotSimulation());
            DragDropHandler.SetStrategy(new DragDropStrategyQuickSlotSimulation());
        }

        protected override void Start()
        {
            base.Start();
            SceneGame.KeyboardManager.RegisterInputHandler(this);
            _uiWindowSkill =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowSkill>(UIWindowConstants.WindowUid.Skill);
            _uiWindowInventory =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            
            LoadIcons();
        }
        /// <summary>
        /// 저장되어있는 스킬 정보로 아이콘 셋팅하기
        /// 스킬창이 열려있지 않으면 업데이트 하지 않음
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.Instance.saveDataManager.QuickSlotSimulation.GetAllDatas();
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

                UIIcon uiIcon = icon.GetComponent<UIIcon>();
                if (uiIcon == null) continue;
                SaveDataIcon dataIcon = datas.GetValueOrDefault(index);
                if (dataIcon == null) continue;
                    
                int uid = dataIcon.Uid;
                int count = dataIcon.Count;
                int level = dataIcon.Level;
                uiIcon.ChangeInfoByUid(uid, count, level);
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
                OnKeyDown(KeyCode.Alpha1);
                return true;
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                OnKeyDown(KeyCode.Alpha2);
                return true;
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                OnKeyDown(KeyCode.Alpha3);
                return true;
            }
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                OnKeyDown(KeyCode.Alpha4);
                return true;
            }
#endif

            return false;
        }
        /// <summary>
        /// 키보드로 스킬 사용하기
        /// </summary>
        /// <param name="keyCode"></param>
        private void OnKeyDown(KeyCode keyCode)
        {
            if (iconType == IconConstants.Type.Skill)
            {
                OnKeyDownSkill(keyCode);
            }
            else if (iconType == IconConstants.Type.Item)
            {
                OnKeyDownItem(keyCode);
            }
        }

        private void OnKeyDownItem(KeyCode keyCode)
        {
        }

        private void OnKeyDownSkill(KeyCode keyCode)
        {
            if (SceneGame.Instance.player == null)
            {
                GcLogger.LogError("플레이어가 없습니다.");
                return ;
            }
            // GcLogger.Log("UIWindowQuickSlot Key pressed Alpha1");
            UIIcon icon = GetIconByIndex(_indexByKeyCode.GetValueOrDefault(keyCode));
            if (icon == null || icon.uid <= 0) return;
            if (!icon.IsSkill()) return;
            var info = TableLoaderManager.Instance.TableSkill.GetDataByUidLevel(icon.uid, icon.GetLevel());
            if (info == null)
            {
                GcLogger.LogError("스킬 테이블에 없는 스킬입니다. uid: " + icon.uid);
                return;
            }

            if (SceneGame.Instance.player.GetComponent<Player>().CheckNeedMp(info.NeedMp) == false)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("QuickSlot_NotEnoughMana");//"마력이 부족합니다."
                return;
            }

            if (!icon.PlayCoolTime(info.CoolTime)) return;
            
            SceneGame.Instance.player.GetComponent<Player>().UseSkill(icon.uid, icon.GetLevel());
        }

        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;

            if (iconType == IconConstants.Type.Skill)
            {
                OnRightClickSkill(icon);
            }
            else if (iconType == IconConstants.Type.Item)
            {
                OnRightClickItem(icon);
            }
            
        }

        private void OnRightClickItem(UIIcon icon)
        {
            // 인벤토리 창이 열려있을때는 해제 하기
            if (!_uiWindowInventory || !_uiWindowInventory.IsOpen()) return;
            DetachIcon(icon.slotIndex);
        }

        private void OnRightClickSkill(UIIcon icon)
        {
            float time = SceneGame.Instance.uIIconCoolTimeManager.GetCurrentCoolTime(uid, icon.uid);
            if (time > 0)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("Action_CannotUseDuringCooldown");//"쿨타임 중에는 사용할 수 없습니다."
                return;
            }
            // 스킬 창이 열려있을때는 해제 하기
            if (!_uiWindowSkill || !_uiWindowSkill.IsOpen()) return;
            DetachIcon(icon.slotIndex);
        }

        protected override void OnSelectedIcon(UIIcon selectedIcon)
        {
            if (!_player)
            {
                _player = SceneGame.player.GetComponent<Player>();
                return;
            }
            if (!_player) return;
            if (!selectedIcon || selectedIcon.uid <= 0) return;
            // const int partIndex = (int)ItemConstants.PartsType.Weapon;
            // _player.EquipItem(partIndex, selectedIcon.uid, selectedIcon.GetCount());
            
            // 장착이 불가능한 경우, 머리위에 들기
            // 장착 가능한 경우 인벤토리에 있는 아이템을 장착
            
            var partSlotIndex = selectedIcon.GetPartsSlotIndex();
            if (partSlotIndex < 0) return;
            SceneGame.uIWindowManager.MoveIcon(uid, selectedIcon.index, UIWindowConstants.WindowUid.Equip,
                selectedIcon.GetCount(), partSlotIndex);
        }

    }
}