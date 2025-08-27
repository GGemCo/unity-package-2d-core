using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<int, Toggle> _tabToggles = new();
        
        private void OnDestroy()
        {
            if (_tabToggles == null) return;
            // 메모리 누수 방지: 이벤트 해제 및 객체 정리
            foreach (var data in _tabToggles)
            {
                if (data.Value) data.Value.onValueChanged.RemoveAllListeners();
            }
            _tabToggles.Clear();
        }

        protected override void Awake()
        {
            base.Awake();
            CreatePanelBase();
            CreateTabButton();
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

            foreach (var uiPanelOptionBase in listPanelOptionBase)
            {
                uiPanelOptionBase.SetWindowOption(this);
            }
            
            SelectFirstTab();
        }
        private void SelectFirstTab()
        {
            var first = listPanelOptionBase.FirstOrDefault();
            if (first== null) return;
            _tabToggles[first.PanelIndex].SetIsOnWithoutNotify(true);
            TrySwitchTo(first.PanelIndex, skipGuard:true);
        }
        private bool TrySwitchTo(int panelIndex, bool skipGuard = false)
        {
            if (!skipGuard && !GuardCloseOrSwitch(() => DoSwitch(panelIndex)))
            {
                // 가드에서 취소됨 → 기존 토글 복구
                if (_currentIndexTabButton != -1 && _tabToggles.TryGetValue(_currentIndexTabButton, out var t))
                    t.SetIsOnWithoutNotify(true);
                return false;
            }
            DoSwitch(panelIndex);
            return true;
        }
        private void DoSwitch(int panelIndex)
        {
            foreach (var uiPanelOptionBase in listPanelOptionBase) uiPanelOptionBase.Show(false);
            var target = listPanelOptionBase.First(x => x.PanelIndex == panelIndex);
            target.Show(true);
            _currentIndexTabButton = panelIndex;
        }
        private bool GuardCloseOrSwitch(Action proceed)
        {
            foreach (var uiPanelOptionBase in listPanelOptionBase)
            {
                if (uiPanelOptionBase.IsDirty)
                {
                    PopupMetadata popupMetadata = new PopupMetadata
                    {
                        PopupType = PopupManager.Type.Default,
                        MessageColor = Color.red,
                        Title = "취소하기", //슬롯 삭제
                        Message = "변경한 내용을 저장하지 않았습니다.\n취소하시겠습니까?",
                        OnConfirm = () => {
                            GcLogger.Log($"GuardCloseOrSwitch popup confirm.");
                            // uiPanelOptionBase.Revert(); // 되돌리고 진행 (또는 TryApply로 저장 후 진행)
                            // proceed();
                        },
                        ShowCancelButton = true
                    };
                    popupManager.ShowPopup(popupMetadata);
                    return false;
                }
            }
            proceed();
            return true;
        }
        private void CreatePanelBase()
        {
            if (listPrefabPanel.Count <= 0)
            {
                GcLogger.LogError($"UIPanel 프리팹을 등록해주세요.");
                return;
            }
            listPanelOptionBase = new List<UIPanelOptionBase>();
            int index = 0;
            foreach (var prefab in listPrefabPanel)
            {
                var objectPanel = Instantiate(prefab, parentPanel);
                UIPanelOptionBase uiPanelOptionBase = objectPanel.GetComponent<UIPanelOptionBase>();
                if (uiPanelOptionBase == null)
                {
                    GcLogger.LogError($"프리팹에 UIPanelOptionBase 클래스가 없습니다.");
                    continue;
                }

                uiPanelOptionBase.PanelIndex = index;
                listPanelOptionBase.Add(uiPanelOptionBase);
                index++;
            }
        }
        /// <summary>
        /// 탭 버튼 만들기
        /// </summary>
        private void CreateTabButton()
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
                MetaDataToggle metaDataToggle = new MetaDataToggle(
                    tabTogglePrefab.gameObject, 
                    uiPanelOptionBase.Title, 
                    LocalizationConstants.Tables.UIWindowOption, 
                    uiPanelOptionBase.Title
                    );
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
                // 옵션이 변경된 것이 있을때 체크하기위해 추가
                UIToggleConfirmable uiToggleConfirmable = toggle.GetComponent<UIToggleConfirmable>();
                if (uiToggleConfirmable)
                {
                    uiToggleConfirmable.RequireConfirm = true;
                    uiToggleConfirmable.OnConfirmRequested += HandleConfirmRequested;
                }

                _tabToggles.TryAdd(i, toggle);
            }

            // 기본 선택 탭의 시각 상태를 즉시 반영
            foreach (var t in tabToggleGroup.gameObject.GetComponentsInChildren<Toggle>())
                t.OnPointerExit(null); // 상태 갱신 트리거 (필요 시)
        }
        /// <summary>
        /// 탭 버튼을 클릭했을때, 변경사항이 있는지 체크하기
        /// </summary>
        /// <param name="target"></param>
        private void HandleConfirmRequested(UIToggleConfirmable target)
        {
            if (listPanelOptionBase.Count <= 0) return;
            
            foreach (var uiPanelOptionBase in listPanelOptionBase)
            {
                if (uiPanelOptionBase.IsDirty)
                {
                    PopupMetadata popupMetadata = new PopupMetadata
                    {
                        PopupType = PopupManager.Type.Default,
                        MessageColor = Color.red,
                        Title = "저장하기", //슬롯 삭제
                        Message = "변경한 내용을 저장하지 않았습니다.\n저장하시겠습니까?",
                        OnConfirm = () => {
                            GcLogger.Log($"GuardCloseOrSwitch popup confirm.");
                            uiPanelOptionBase.TryApply(); // 되돌리고 진행 (또는 TryApply로 저장 후 진행)
                            // proceed();
                            uiPanelOptionBase.MarkDirty(false);
                        },
                        ShowCancelButton = true
                    };
                    popupManager.ShowPopup(popupMetadata);
                    return;
                }
            }
            
            target.SuppressConfirm = true;
            target.isOn = true;          // ToggleGroup 규칙에 따라 이전은 자동 Off
            target.SuppressConfirm = false;
        }
        /// <summary>
        /// 탭 버튼 클릭했을때, 패널 보임/안보임 처리
        /// </summary>
        /// <param name="tab"></param>
        private void OnClickTapButton(int tab)
        {
            ShowPanelByIndex(tab);
        }
        /// <summary>
        /// 탭 버튼 클릭했을때, 패널 보임/안보임 처리
        /// </summary>
        /// <param name="index"></param>
        private void ShowPanelByIndex(int index)
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
                    if (uiPanelOptionBase.IsDirty)
                    {
                        PopupMetadata popupMetadata = new PopupMetadata
                        {
                            PopupType = PopupManager.Type.Default,
                            MessageColor = Color.red,
                            Title = "저장하기", //슬롯 삭제
                            Message = "변경한 내용을 저장하지 않았습니다.\n저장하시겠습니까?",
                            OnConfirm = () => {
                                uiPanelOptionBase.TryApply();
                                uiPanelOptionBase.MarkDirty(false);
                                Show(false);
                            },
                            OnCancel = () =>
                            {
                                uiPanelOptionBase.Revert();
                                uiPanelOptionBase.MarkDirty(false);
                                Show(false);
                            },
                            ShowCancelButton = true
                        };
                        popupManager.ShowPopup(popupMetadata);
                        return false;
                    }
                }
            }
            else
            {
                // 현재 선택된 탭 열기
                ShowPanelByIndex(_currentIndexTabButton);
            }

            return base.Show(show);
        }
    }
}