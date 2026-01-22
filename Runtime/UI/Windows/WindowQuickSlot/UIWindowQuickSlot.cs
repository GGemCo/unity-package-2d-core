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
                // todo. 정리 필요.
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
            // todo. 정리 필요
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