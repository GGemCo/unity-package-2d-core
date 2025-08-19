using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 옵션(설정) 윈도우
    /// </summary>
    public class UIWindowOption : UIWindow
    {
        public enum IndexTapButton
        {
            Default,
            Control
        }

        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("탭 토글을 넣을 panel")]
        [SerializeField] private RectTransform panelTabToggle;   // 버튼을 담을 부모(레이아웃 그룹 권장)
        [Tooltip("탭 토글 그룹")]
        [SerializeField] private ToggleGroup toggleGroupTab;
        [Tooltip("탭 토글 프리팹")]
        [SerializeField] private UIToggleConfirmable uiToggleTab;
        
        [Header("레이어")]
        [Tooltip("기본 옵션 레이어")]
        [SerializeField] private UIPanelOptionBase uiPanelOptionDefault;

        private IndexTapButton _currentIndexTabButton;
        [Header("매니저")]
        // 인트로 씬에서는 수동으로 넣어주고 있다.
        [Tooltip("팝업 매니저")] [SerializeField]
        public PopupManager popupManager;
        public void SetPopupManager(PopupManager value) => popupManager = value;
        [Tooltip("사운드 매니저")] [SerializeField] public SoundManager soundManager;
        public void SetSoundManager(SoundManager value) => soundManager = value;

        private readonly List<Toggle> _spawned = new();
        private readonly Dictionary<IndexTapButton, UIPanelOptionBase> _dictionaryLayer = new();
        
        protected override void Awake()
        {
            if (!AddressableLoaderSettings.Instance) return;
            base.Awake();

        }
        /// <summary>
        /// 탭 버튼을 클리했을때, 변경사항이 있는지 체크하기
        /// </summary>
        /// <param name="target"></param>
        private void HandleConfirmRequested(UIToggleConfirmable target)
        {
            foreach (var data in _dictionaryLayer)
            {
                if (!data.Value.Show(false)) return;
            }
            
            target.SuppressConfirm = true;
            target.isOn = true;          // ToggleGroup 규칙에 따라 이전은 자동 Off
            target.SuppressConfirm = false;
        }

        private void OnDestroy()
        {
            if (_spawned == null) return;
            // 메모리 누수 방지: 이벤트 해제 및 객체 정리
            foreach (var b in _spawned)
            {
                if (b) b.onValueChanged.RemoveAllListeners();
            }
            _spawned.Clear();
        }
        protected override void Start()
        {
            base.Start();
            gameObject.SetActive(false);
            
            if (popupManager == null)
            {
                popupManager = SceneGame.Instance.popupManager;
            }
            if (soundManager == null)
            {
                soundManager = SceneGame.Instance.soundManager;
            }
            
            // uiPanelOptionDefault에서 popupManager, soundManager를 사용한다
            uiPanelOptionDefault?.SetUIWindowOption(this);
            
            // BootstrapperOptionsControls 의 Awake에서 Regist 하고 있다.
            UIWindowOptionsExtensionRegistry uiWindowOptionsExtensionRegistry =
                gameObject.GetComponent<UIWindowOptionsExtensionRegistry>();
            uiWindowOptionsExtensionRegistry?.BuildAll();
            
            InitializeTabButton();
            
            SetIndexTabButton(IndexTapButton.Default);
        }

        private void InitializeTabButton()
        {
            if (!panelTabToggle) return;
            if (!toggleGroupTab) return;

            if (uiPanelOptionDefault)
            {
                _dictionaryLayer.Add(IndexTapButton.Default, uiPanelOptionDefault);
            }

            // 기존 자식 정리(필요 시)
            for (int i = panelTabToggle.childCount - 1; i >= 0; i--)
                Destroy(panelTabToggle.GetChild(i).gameObject);

            // 열거형 값 배열
            var values = (IndexTapButton[])Enum.GetValues(typeof(IndexTapButton));
            
            _spawned.Capacity = values.Length;
            
            var ordered = _dictionaryLayer
                .OrderBy(pair => pair.Key) // enum 값의 int 순서 기준
                .ToList();                 // 필요 시 List<KeyValuePair<...>>

            foreach (var data in ordered)
            {
                IndexTapButton indexTapButton = data.Key;
                var toggle = UIComponentHelper.CreateToggle(uiToggleTab, indexTapButton.ToString());
                if (!toggle) continue;
                toggle.name = $"Btn_{indexTapButton}";
                toggle.transform.SetParent(panelTabToggle);
                toggle.group = toggleGroupTab;
                int captured = (int)indexTapButton;
                toggle.SetIsOnWithoutNotify(false);
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        OnClickTapButton(values[captured]);
                });
                UIToggleConfirmable uiToggleConfirmable = toggle.GetComponent<UIToggleConfirmable>();
                if (uiToggleConfirmable)
                {
                    uiToggleConfirmable.RequireConfirm = true;
                    uiToggleConfirmable.OnConfirmRequested += HandleConfirmRequested;
                }

                _spawned.Add(toggle);
            }

            // 기본 선택 탭의 시각 상태를 즉시 반영
            foreach (var t in toggleGroupTab.gameObject.GetComponentsInChildren<Toggle>())
                t.OnPointerExit(null); // 상태 갱신 트리거 (필요 시)
        }
        private void OnClickTapButton(IndexTapButton tab)
        {
            SetIndexTabButton(tab);
            // switch (tab)
            // {
            //     case IndexTapButton.Default:
            //         Debug.Log("Default 탭 클릭");
            //         break;
            //     case IndexTapButton.Control:
            //         Debug.Log("Control 탭 클릭");
            //         break;
            //     default:
            //         Debug.Log($"Unhandled: {tab}");
            //         break;
            // }
        }

        public void AddLayer(IndexTapButton index, UIPanelOptionBase layer)
        {
            _dictionaryLayer.TryAdd(index, layer);
        }
        private void SetIndexTabButton(IndexTapButton index)
        {
            _currentIndexTabButton = index;
            foreach (var data in _dictionaryLayer)
            {
                data.Value.Show(false);
            }

            var selected = _dictionaryLayer.GetValueOrDefault(index);
            if (selected == null) return;
            selected.Show(true);
        }

        public override bool Show(bool show)
        {
            if (!show)
            {
                // 변경한 내역이 있는지 체크한 후 닫기
                foreach (var data in _dictionaryLayer)
                {
                    if (!data.Value.Show(false)) return false;
                }
            }
            else
            {
                // 현재 선택된 탭 열기
                SetIndexTabButton(_currentIndexTabButton);
            }

            return base.Show(show);
        }
    }
}