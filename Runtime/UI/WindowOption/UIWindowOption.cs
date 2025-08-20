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
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("탭 토글을 넣을 panel")]
        [SerializeField] private RectTransform tabTogglePanel;   // 버튼을 담을 부모(레이아웃 그룹 권장)
        [Tooltip("탭 토글 그룹")]
        [SerializeField] private ToggleGroup tabToggleGroup;
        [Tooltip("탭 토글 프리팹")]
        [SerializeField] private UIToggleConfirmable tabTogglePrefab;
        [Tooltip("하위 패널을 넣을 공간")]
        [SerializeField] private Transform parentPanel;
        
        [Header("매니저")]
        // 인트로 씬에서는 수동으로 넣어주고 있다.
        [Tooltip("팝업 매니저")] [SerializeField]
        public PopupManager popupManager;
        public void SetPopupManager(PopupManager value) => popupManager = value;
        [Tooltip("사운드 매니저")] [SerializeField] public SoundManager soundManager;
        public void SetSoundManager(SoundManager value) => soundManager = value;
        
        [Header("하위 패널 프리팹")]
        [SerializeField] private List<GameObject> listPrefabPanel;
        private List<UIPanelOptionBase> listPanelOptionBase;

        private int _currentIndexTabButton;
        private readonly List<Toggle> _spawned = new();
        
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
            // 인트로 씬에서는 window 데이터가 없어서, 여기서 SetActive 처리 한다.
            gameObject.SetActive(false);
            
            if (popupManager == null)
            {
                popupManager = SceneGame.Instance.popupManager;
            }
            if (soundManager == null)
            {
                soundManager = SceneGame.Instance.soundManager;
            }
            
            InitializePanelBase();
            InitializeTabButton();
            SetIndexTabButton(0);
        }

        private void InitializePanelBase()
        {
            if (listPrefabPanel.Count <= 0)
            {
                GcLogger.LogError($"UIPanel 프리팹을 등록해주세요.");
                return;
            }
            listPanelOptionBase = new List<UIPanelOptionBase>();
            foreach (var prefab in listPrefabPanel)
            {
                var objectPanel = Instantiate(prefab, parentPanel);
                UIPanelOptionBase uiPanelOptionBase = objectPanel.GetComponent<UIPanelOptionBase>();
                if (uiPanelOptionBase == null)
                {
                    GcLogger.LogError($"프리팹에 UIPanelOptionBase 클래스가 없습니다.");
                    continue;
                }
                uiPanelOptionBase.SetUIWindowOption(this);
                listPanelOptionBase.Add(uiPanelOptionBase);
            }
        }

        private void InitializeTabButton()
        {
            if (!tabTogglePanel) return;
            if (!tabToggleGroup) return;

            // 기존 자식 정리(필요 시)
            for (int i = tabTogglePanel.childCount - 1; i >= 0; i--)
                Destroy(tabTogglePanel.GetChild(i).gameObject);
            
            for (int i = 0; i < listPrefabPanel.Count; i++)
            {
                var uiPanelOptionBase = listPanelOptionBase[i];
                if (uiPanelOptionBase == null) continue;
                MetaDataToggle metaDataToggle = new MetaDataToggle(tabTogglePrefab.gameObject, uiPanelOptionBase.Title,
                    LocalizationConstants.Tables.UIWindowOption, uiPanelOptionBase.Title);
                var toggle = UIComponentHelper.CreateToggle(metaDataToggle);
                if (!toggle) continue;
                toggle.name = $"Btn_{i}";
                toggle.transform.SetParent(tabTogglePanel);
                toggle.group = tabToggleGroup;
                int captured = i;
                toggle.SetIsOnWithoutNotify(false);
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                        OnClickTapButton(captured);
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
            foreach (var t in tabToggleGroup.gameObject.GetComponentsInChildren<Toggle>())
                t.OnPointerExit(null); // 상태 갱신 트리거 (필요 시)
        }
        /// <summary>
        /// 탭 버튼을 클리했을때, 변경사항이 있는지 체크하기
        /// </summary>
        /// <param name="target"></param>
        private void HandleConfirmRequested(UIToggleConfirmable target)
        {
            if (listPanelOptionBase.Count <= 0) return;
            foreach (var uiPanelOptionBase in listPanelOptionBase)
            {
                if (!uiPanelOptionBase.Show(false)) return;
            }
            
            target.SuppressConfirm = true;
            target.isOn = true;          // ToggleGroup 규칙에 따라 이전은 자동 Off
            target.SuppressConfirm = false;
        }
        private void OnClickTapButton(int tab)
        {
            SetIndexTabButton(tab);
        }

        private void SetIndexTabButton(int index)
        {
            if (listPanelOptionBase.Count <= 0) return;
            _currentIndexTabButton = index;
            foreach (var uiPanelOptionBase in listPanelOptionBase)
            {
                uiPanelOptionBase.Show(false);
            }

            var selected = listPanelOptionBase[index];
            if (selected == null) return;
            selected.Show(true);
        }

        public override bool Show(bool show)
        {
            if (!show)
            {
                if (listPanelOptionBase.Count <= 0) return false;
                // 변경한 내역이 있는지 체크한 후 닫기
                foreach (var uiPanelOptionBase in listPanelOptionBase)
                {
                    if (!uiPanelOptionBase.Show(false)) return false;
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