using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 퀵슬롯 윈도우
    /// </summary>
    public class UIWindowQuickSlot : UIWindow, IInputHandler
    {
        public Image[] iconHotKey;
        public UIWindowSkill uiWindowSkill;
        public int Priority => 1;

        private readonly Dictionary<KeyCode, int> indexByKeyCode = new Dictionary<KeyCode, int>
        {
            { KeyCode.Alpha1, 0 },
            { KeyCode.Alpha2, 1 },
            { KeyCode.Alpha3, 2 },
            { KeyCode.Alpha4, 3 },
        };
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowManager.WindowUid.QuickSlot;
            base.Awake();
            SetSetIconHandler(new SetIconHandlerQuickSlot());
            DragDropHandler.SetStrategy(new DragDropStrategyQuickSlot());
        }

        protected override void Start()
        {
            base.Start();
            SceneGame.Instance.KeyboardManager.RegisterInputHandler(this);
            LoadIcons();
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
                iconHotKey[index].transform.SetParent(slots[index].transform);
                iconHotKey[index].transform.localPosition = new Vector3(-slotSize.x/2f, slotSize.y/2f, 0);
                
                var icon = icons[index];
                if (icon == null) continue;
                UIIconSkill uiIcon = icon.GetComponent<UIIconSkill>();
                if (uiIcon == null) continue;
                SaveDataIcon structSkillIcon = datas.GetValueOrDefault(index);
                if (structSkillIcon == null) continue;
                
                int skillUid = structSkillIcon.Uid;
                int skillCount = structSkillIcon.Count;
                int skillLevel = structSkillIcon.Level;
                uiIcon.ChangeInfoByUid(skillUid, skillCount, skillLevel);
            }
        }
        protected void OnDisable()
        {
            if (SceneGame.Instance == null) return;
            SceneGame.Instance.KeyboardManager.RemoveInputHandler(this);
        }

        public bool HandleInput()
        {
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

            return false;
        }
        /// <summary>
        /// 키보드로 스킬 사용하기
        /// </summary>
        /// <param name="keyCode"></param>
        private void OnKeyDownSkill(KeyCode keyCode)
        {
            if (SceneGame.Instance.player == null)
            {
                GcLogger.LogError("플레이어가 없습니다.");
                return ;
            }
            // GcLogger.Log("UIWindowQuickSlot Key pressed Alpha1");
            UIIcon icon = GetIconByIndex(indexByKeyCode.GetValueOrDefault(keyCode));
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
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("마력이 부족합니다.");
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
            
            float time = SceneGame.Instance.uIIconCoolTimeManager.GetCurrentCoolTime(uid, icon.uid);
            if (time > 0)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("쿨타임 중에는 사용할 수 없습니다.");
                return;
            }
            // 스킬 창이 열려있을때는 해제 하기
            if (!uiWindowSkill.IsOpen()) return;
            DetachIcon(icon.slotIndex);
        }
    }
}