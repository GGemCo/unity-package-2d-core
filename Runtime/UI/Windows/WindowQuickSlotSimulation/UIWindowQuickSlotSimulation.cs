using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
        [HideInInspector] public TableItem tableItem;
        [HideInInspector] public QuickSlotSimulationData quickSlotSimulationData;
        [Tooltip("단축키에 사용할 숫자 UI Image")]
        public Image[] iconHotKey;
        
        private UIWindowSkill _uiWindowSkill;
        private UIWindowInventory _uiWindowInventory;
        private Player _player;

        // ─────────────────────────────────────────────────────────────
        //  Hotkey 맵 (최대 9개) — maxCountIcon에 따라 동적 사용
        // ─────────────────────────────────────────────────────────────
        private const int MaxDigits = 9;

#if GGEMCO_USE_OLD_INPUT
        private static readonly KeyCode[] _alphaKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };
#endif

#if GGEMCO_USE_NEW_INPUT
        private KeyControl[] _digitKeys; // Keyboard.current.digit1Key ~ digit9Key 캐싱
        private static readonly KeyCode[] AlphaKeysForNew =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
        };
#endif

        // 선택적으로, UI 뱃지/툴팁에 사용할 키-인덱스 매핑이 필요하면 유지
        private readonly Dictionary<KeyCode, int> _indexByKeyCode = new Dictionary<KeyCode, int>(MaxDigits);

        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.QuickSlotSimulation;
            if (TableLoaderManager.Instance == null) return;
            base.Awake();
            tableItem = TableLoaderManager.Instance.TableItem;
            IconPoolManager.SetSetIconHandler(new SetIconHandlerQuickSlotSimulation());
            DragDropHandler.SetStrategy(new DragDropStrategyQuickSlotSimulation());
            
            BuildHotkeyBindings();      // 단축키 맵 구성
            SetupHotkeyUIAnchors();     // 숫자 UI 위치/활성 정리
        }

        protected override void Start()
        {
            base.Start();
            if (SceneGame != null && SceneGame.saveDataManager != null)
            {
                quickSlotSimulationData = SceneGame.saveDataManager.QuickSlotSimulation;
            }
            SceneGame.KeyboardManager.RegisterInputHandler(this);
            _uiWindowSkill =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowSkill>(UIWindowConstants.WindowUid.Skill);
            _uiWindowInventory =
                SceneGame.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            
            LoadIcons();
        }
        /// <summary>
        /// 저장되어있는 아이템 정보로 아이콘 셋팅하기
        /// </summary>
        private void LoadIcons()
        {
            if (!gameObject.activeSelf) return;
            var datas = SceneGame.Instance.saveDataManager.QuickSlotSimulation.GetAllItemCounts();
            if (datas == null) return;
            
            // 숫자 UI(아이콘 위 표시)도 maxCountIcon에 맞춰 위치/활성
            SetupHotkeyUIAnchors();
            
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
                    
                int iconUid = dataIcon.Uid;
                int count = dataIcon.Count;
                int level = dataIcon.Level;
                uiIcon.ChangeInfoByUid(iconUid, count, level);
            }
        }
        protected void OnDisable()
        {
            if (SceneGame.Instance == null) return;
            SceneGame.Instance.KeyboardManager.RemoveInputHandler(this);
        }

        public bool HandleInput()
        {
            int usable = Mathf.Min(maxCountIcon, MaxDigits);
            if (usable <= 0) return false;

#if GGEMCO_USE_OLD_INPUT
            for (int i = 0; i < usable; i++)
            {
                var key = _alphaKeys[i];
                if (Input.GetKeyDown(key))
                {
                    OnKeyDown(key); // 내부에서 index 계산
                    return true;
                }
            }
#elif GGEMCO_USE_NEW_INPUT
            var kb = Keyboard.current;
            if (kb == null) return false;

            // 최초 1회 캐싱 실패 시 재시도(도메인 리로드 등)
            if (_digitKeys is not { Length: MaxDigits } || _digitKeys[0] == null)
                BuildHotkeyBindings();

            for (int i = 0; i < usable; i++)
            {
                var keyCtrl = _digitKeys?[i];
                if (keyCtrl is not { wasPressedThisFrame: true }) continue;
                // New Input은 KeyControl만 있는데, 내부 로직은 KeyCode 기반이므로 매핑 KeyCode 전달
                OnKeyDown(AlphaKeysForNew[i]);
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
            if (!_indexByKeyCode.TryGetValue(keyCode, out var index))
                return;

            if (index < 0 || index >= maxCountIcon)
                return;

            SetSelectedIcon(index);
        }
        /// <summary>
        /// 아이콘 우클릭했을때 처리 
        /// </summary>
        /// <param name="icon"></param>
        public override void OnRightClick(UIIcon icon)
        {
            if (icon == null) return;

            // 인벤토리가 열려 있으면
            if (_uiWindowInventory != null && _uiWindowInventory.IsOpen())
            {
                SceneGame.uIWindowManager.MoveIcon(uid, icon.slotIndex,
                        UIWindowConstants.WindowUid.Inventory, icon.GetCount());
            }
        }

        /// <summary>
        /// 아이템 선택시, 플레이어의 ToolController에 등록한다.
        /// </summary>
        /// <param name="icon"></param>
        protected override void OnSelectedIcon(UIIcon icon)
        {
            if (!_player)
            {
                _player = SceneGame.player.GetComponent<Player>();
            }
            if (!_player) return;
            if (!icon || icon.uid <= 0) return;

            if (icon.IsToolType() || icon.IsSeedType())
            {
                _player.EquipTool(icon.uid);
            }
        }
        // ─────────────────────────────────────────────────────────────
        // 유틸: 단축키 맵 구성/캐싱
        // ─────────────────────────────────────────────────────────────
        private void BuildHotkeyBindings()
        {
            _indexByKeyCode.Clear();

            // KeyCode → 인덱스
            // (1→0, 2→1, ... 9→8)
            _indexByKeyCode[KeyCode.Alpha1] = 0;
            _indexByKeyCode[KeyCode.Alpha2] = 1;
            _indexByKeyCode[KeyCode.Alpha3] = 2;
            _indexByKeyCode[KeyCode.Alpha4] = 3;
            _indexByKeyCode[KeyCode.Alpha5] = 4;
            _indexByKeyCode[KeyCode.Alpha6] = 5;
            _indexByKeyCode[KeyCode.Alpha7] = 6;
            _indexByKeyCode[KeyCode.Alpha8] = 7;
            _indexByKeyCode[KeyCode.Alpha9] = 8;

#if GGEMCO_USE_NEW_INPUT
            // Keyboard.current.digit1Key ~ digit9Key 캐싱(최초 1회)
            var kb = Keyboard.current;
            if (kb != null)
            {
                _digitKeys = new []
                {
                    kb.digit1Key, kb.digit2Key, kb.digit3Key,
                    kb.digit4Key, kb.digit5Key, kb.digit6Key,
                    kb.digit7Key, kb.digit8Key, kb.digit9Key
                };
            }
#endif
        }

        // ─────────────────────────────────────────────────────────────
        // 유틸: 숫자 UI(anchor) 정리
        //    - maxCountIcon 이하만 활성화/부모 재지정/위치 조정
        // ─────────────────────────────────────────────────────────────
        private void SetupHotkeyUIAnchors()
        {
            if (slots == null || iconHotKey == null) return;

            int usable = Mathf.Min(maxCountIcon, MaxDigits);

            for (int i = 0; i < iconHotKey.Length; i++)
            {
                var img = iconHotKey[i];
                if (!img) continue;

                bool enable = i < usable && i < slots.Length && slots[i];
                img.gameObject.SetActive(enable);

                if (enable)
                {
                    // 슬롯 좌상단에 붙이기(기존 로직 유지)
                    img.transform.SetParent(slots[i].transform, worldPositionStays: false);
                    img.rectTransform.anchoredPosition = new Vector2(-slotSize.x / 2f, slotSize.y / 2f);
                }
            }
        }
    }
}