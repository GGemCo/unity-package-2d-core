using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 단순한 페이징 UI 컨트롤러.
    /// pageContainer의 자식 오브젝트(UISlot)를 자동으로 수집하여
    /// countPerPage 단위로 나누고, Prev/Next 버튼으로 페이지를 전환한다.
    ///
    /// ✔ 자동 기능
    /// - 자식 UISlot을 자동 수집하여 페이지 구성
    /// - 페이지 범위 자동 클램프
    /// - Prev/Next 버튼 상태 자동 업데이트
    /// - "현재 페이지/전체 페이지" 텍스트 자동 표시
    ///
    /// ✔ 사용 환경
    /// - GridLayoutGroup 또는 VerticalLayoutGroup 아래의 UISlot 리스트 페이지 구성에 적합
    /// </summary>
    public class UIPageController : MonoBehaviour
    {
        [Header("페이지 구조")]
        [Tooltip("페이지를 구성할 슬롯들이 들어있는 부모 Transform")]
        public Transform pageContainer;

        [Tooltip("현재 페이지/전체 페이지를 표시할 TextMeshProUGUI")]
        public TextMeshProUGUI textPage;

        [Tooltip("이전 페이지 버튼")]
        public Button buttonPrev;

        [Tooltip("다음 페이지 버튼")]
        public Button buttonNext;

        [Tooltip("한 페이지에 보여줄 슬롯 개수")]
        public int countPerPage;

        /// <summary>수집된 슬롯(GameObject) 리스트</summary>
        private GameObject[] _objects;

        /// <summary>수집된 UISlot 컴포넌트 리스트</summary>
        private UISlot[] _slots;

        /// <summary>필터링 결과로 현재 보이는 슬롯들의 인덱스 버퍼</summary>
        private readonly List<int> _visibleSlotIndices = new List<int>(128);

        /// <summary>외부에서 지정한 슬롯 표시 순서입니다. 값은 UISlot.Index 기준입니다.</summary>
        private readonly List<int> _slotDisplayOrder = new List<int>(128);

        /// <summary>표시 순서 중복 검사용 버퍼입니다.</summary>
        private readonly HashSet<int> _slotDisplayOrderSet = new HashSet<int>();

        /// <summary>현재 표시 대상 오브젝트 인덱스 중복 검사용 버퍼입니다.</summary>
        private readonly HashSet<int> _visibleObjectIndexSet = new HashSet<int>();

        /// <summary>외부 지정 표시 순서를 사용할지 여부입니다.</summary>
        private bool _useCustomSlotDisplayOrder;

        /// <summary>현재 페이지 번호 (1부터 시작)</summary>
        private int _currentPage;

        /// <summary>외부에서 명시적으로 슬롯 목록을 주입했는지 여부</summary>
        private bool _isInitialized;

        /// <summary>
        /// 초기 설정.
        /// Prev/Next 버튼을 클릭 시 OnClickPrev, OnClickNext가 호출되도록 리스너를 등록한다.
        /// </summary>
        private void Awake()
        {
            _currentPage = 1;
            buttonPrev?.onClick.AddListener(OnClickPrev);
            buttonNext?.onClick.AddListener(OnClickNext);
        }

        /// <summary>
        /// 이미 생성된 슬롯 GameObject 배열을 기준으로 페이지를 구성한다.
        /// UIWindow.slots 처럼 index 순서가 보장된 배열을 넘기는 용도입니다.
        /// </summary>
        public void InitializeBySlotObjects(GameObject[] slotObjects)
        {
            if (slotObjects == null || slotObjects.Length == 0)
            {
                ClearSlots();
                return;
            }

            int len = 0;
            for (int i = 0; i < slotObjects.Length; i++)
            {
                if (slotObjects[i] != null && slotObjects[i].GetComponent<UISlot>() != null)
                {
                    len++;
                }
            }

            _objects = new GameObject[len];
            _slots   = new UISlot[len];

            int idx = 0;
            for (int i = 0; i < slotObjects.Length; i++)
            {
                if (slotObjects[i] == null)
                {
                    continue;
                }

                var slot = slotObjects[i].GetComponent<UISlot>();
                if (slot == null)
                {
                    continue;
                }

                _slots[idx]   = slot;
                _objects[idx] = slot.gameObject;

                // 필터 미적용 상태에서는 모두 보이도록 기본값 true
                slot.isFiltering = true;
                idx++;
            }

            _isInitialized = true;
            _currentPage = 1;
            UpdatePage();
        }

        private void ClearSlots()
        {
            _objects = System.Array.Empty<GameObject>();
            _slots = System.Array.Empty<UISlot>();
            _visibleSlotIndices.Clear();
            _slotDisplayOrder.Clear();
            _slotDisplayOrderSet.Clear();
            _visibleObjectIndexSet.Clear();
            _useCustomSlotDisplayOrder = false;
            _isInitialized = true;
            _currentPage = 1;
            UpdatePage();
        }

        /// <summary>
        /// 현재 페이지 상태에 따라 슬롯 활성화/비활성화,
        /// 페이지 번호 표시, Prev/Next 버튼 활성 여부 등을 갱신한다.
        /// </summary>
        private void UpdatePage()
        {
            // 기본 방어 코드
            if (_objects == null || _objects.Length == 0 || countPerPage <= 0)
            {
                if (textPage != null)
                    textPage.text = "0/0";

                if (buttonPrev != null) buttonPrev.interactable = false;
                if (buttonNext != null) buttonNext.interactable = false;
                return;
            }

            int totalSlotCount = _objects.Length;

            CollectVisibleSlotIndices(totalSlotCount);
            ApplyVisibleSiblingOrder(totalSlotCount);

            int filteredCount = _visibleSlotIndices.Count;

            // 필터 결과가 없으면 모두 비활성 + "0/0"
            if (filteredCount == 0)
            {
                for (int i = 0; i < totalSlotCount; i++)
                {
                    if (_objects[i])
                        _objects[i].SetActive(false);
                }

                if (textPage != null)
                    textPage.text = "0/0";

                if (buttonPrev != null) buttonPrev.interactable = false;
                if (buttonNext != null) buttonNext.interactable = false;
                return;
            }

            int totalPage = Mathf.CeilToInt(filteredCount / (float)countPerPage);

            // 현재 페이지 범위 보정
            _currentPage = Mathf.Clamp(_currentPage, 1, totalPage);

            // 전체 슬롯 비활성화
            for (int i = 0; i < totalSlotCount; i++)
            {
                if (_objects[i])
                    _objects[i].SetActive(false);
            }

            // 현재 페이지에 해당하는 "필터 통과 슬롯"만 활성화
            int startIndexInFiltered = (_currentPage - 1) * countPerPage;
            int endIndexInFiltered   = Mathf.Min(startIndexInFiltered + countPerPage, filteredCount);

            for (int i = startIndexInFiltered; i < endIndexInFiltered; i++)
            {
                int slotIndex = _visibleSlotIndices[i];
                var go = _objects[slotIndex];
                if (go)
                    go.SetActive(true);
            }

            // 페이지 텍스트 갱신
            if (textPage != null)
                textPage.text = $"{_currentPage}/{totalPage}";

            // 버튼 상태 갱신
            if (buttonPrev != null)
                buttonPrev.interactable = _currentPage > 1;

            if (buttonNext != null)
                buttonNext.interactable = _currentPage < totalPage;
        }

        /// <summary>
        /// 버튼에 등록한 콜백 제거 (메모리 누수 방지)
        /// </summary>
        private void OnDestroy()
        {
            buttonPrev?.onClick.RemoveListener(OnClickPrev);
            buttonNext?.onClick.RemoveListener(OnClickNext);
        }

        /// <summary>
        /// "다음 페이지" 버튼 클릭 처리
        /// </summary>
        private void OnClickNext()
        {
            _currentPage++;
            UpdatePage();
        }

        /// <summary>
        /// "이전 페이지" 버튼 클릭 처리
        /// </summary>
        private void OnClickPrev()
        {
            _currentPage--;
            UpdatePage();
        }

        public void ResetPage()
        {
            _currentPage = 1;
            UpdatePage();
        }

        /// <summary>
        /// 필터를 통과한 슬롯의 표시 순서를 외부에서 지정합니다.
        /// 인벤토리 데이터의 실제 슬롯 번호는 그대로 두고, 화면에 보이는 순서만 바꿀 때 사용합니다.
        /// </summary>
        public void SetSlotDisplayOrder(IList<int> slotIndices)
        {
            _slotDisplayOrder.Clear();
            _slotDisplayOrderSet.Clear();

            if (slotIndices != null)
            {
                for (int i = 0; i < slotIndices.Count; i++)
                {
                    int slotIndex = slotIndices[i];
                    if (!_slotDisplayOrderSet.Add(slotIndex))
                    {
                        continue;
                    }

                    _slotDisplayOrder.Add(slotIndex);
                }
            }

            _useCustomSlotDisplayOrder = _slotDisplayOrder.Count > 0;
            _currentPage = 1;
            UpdatePage();
        }

        /// <summary>
        /// 외부 지정 표시 순서를 제거하고 슬롯 원래 순서로 페이지를 구성합니다.
        /// </summary>
        public void ClearSlotDisplayOrder()
        {
            _slotDisplayOrder.Clear();
            _slotDisplayOrderSet.Clear();
            _useCustomSlotDisplayOrder = false;
            _currentPage = 1;
            UpdatePage();
        }

        /// <summary>
        /// 지정한 슬롯 인덱스가 포함된 페이지로 이동합니다.
        /// 인벤토리에서 기본 선택 아이템이 현재 페이지 밖에 있을 때 먼저 해당 페이지를 열기 위해 사용합니다.
        /// </summary>
        public bool ShowPageContainingSlot(int slotIndex)
        {
            if (!_isInitialized || _objects == null || _slots == null || countPerPage <= 0)
            {
                return false;
            }

            CollectVisibleSlotIndices(_objects.Length);

            for (int visibleIndex = 0; visibleIndex < _visibleSlotIndices.Count; visibleIndex++)
            {
                int objectIndex = _visibleSlotIndices[visibleIndex];
                UISlot slot = _slots[objectIndex];
                if (slot == null || slot.Index != slotIndex)
                {
                    continue;
                }

                _currentPage = Mathf.FloorToInt(visibleIndex / (float)countPerPage) + 1;
                UpdatePage();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 필터와 표시 순서를 기준으로 페이지에 사용할 슬롯 오브젝트 인덱스를 수집합니다.
        /// 커스텀 순서가 있으면 해당 순서를 먼저 적용하고, 누락된 필터 통과 슬롯은 기존 순서대로 뒤에 붙입니다.
        /// </summary>
        private void CollectVisibleSlotIndices(int totalSlotCount)
        {
            _visibleSlotIndices.Clear();
            _slotDisplayOrderSet.Clear();

            if (_useCustomSlotDisplayOrder)
            {
                for (int i = 0; i < _slotDisplayOrder.Count; i++)
                {
                    int slotIndex = _slotDisplayOrder[i];
                    if (!_slotDisplayOrderSet.Add(slotIndex))
                    {
                        continue;
                    }

                    int objectIndex = FindVisibleObjectIndexBySlotIndex(slotIndex, totalSlotCount);
                    if (objectIndex >= 0)
                    {
                        _visibleSlotIndices.Add(objectIndex);
                    }
                }
            }

            for (int i = 0; i < totalSlotCount; i++)
            {
                UISlot slot = (_slots != null && i < _slots.Length) ? _slots[i] : null;
                if (slot != null && _slotDisplayOrderSet.Contains(slot.Index))
                {
                    continue;
                }

                if (IsVisibleSlotObject(i))
                {
                    _visibleSlotIndices.Add(i);
                }
            }
        }

        /// <summary>
        /// UISlot.Index 기준 슬롯 번호를 실제 오브젝트 배열 인덱스로 변환합니다.
        /// preLoadSlots 중 일부가 비어 있는 경우를 고려해 배열 인덱스와 슬롯 번호를 분리해서 찾습니다.
        /// </summary>
        private int FindVisibleObjectIndexBySlotIndex(int slotIndex, int totalSlotCount)
        {
            for (int i = 0; i < totalSlotCount; i++)
            {
                UISlot slot = (_slots != null && i < _slots.Length) ? _slots[i] : null;
                if (slot == null || slot.Index != slotIndex)
                {
                    continue;
                }

                return IsVisibleSlotObject(i) ? i : -1;
            }

            return -1;
        }

        /// <summary>
        /// 지정한 슬롯 오브젝트가 현재 필터 기준으로 표시 가능한지 확인합니다.
        /// </summary>
        private bool IsVisibleSlotObject(int objectIndex)
        {
            if (_objects == null || objectIndex < 0 || objectIndex >= _objects.Length)
            {
                return false;
            }

            GameObject go = _objects[objectIndex];
            UISlot slot = (_slots != null && objectIndex < _slots.Length) ? _slots[objectIndex] : null;

            if (!go)
            {
                return false;
            }

            // slot == null 이면 필터 없이 항상 보이는 것으로 처리합니다.
            return slot == null || slot.isFiltering;
        }

        /// <summary>
        /// GridLayoutGroup이 커스텀 표시 순서대로 배치하도록 자식 sibling 순서를 맞춥니다.
        /// 커스텀 순서가 해제되면 원래 슬롯 배열 순서대로 다시 정렬됩니다.
        /// </summary>
        private void ApplyVisibleSiblingOrder(int totalSlotCount)
        {
            _visibleObjectIndexSet.Clear();

            int siblingIndex = 0;
            for (int i = 0; i < _visibleSlotIndices.Count; i++)
            {
                int objectIndex = _visibleSlotIndices[i];
                if (!_visibleObjectIndexSet.Add(objectIndex))
                {
                    continue;
                }

                GameObject go = _objects[objectIndex];
                if (go)
                {
                    go.transform.SetSiblingIndex(siblingIndex);
                    siblingIndex++;
                }
            }

            for (int i = 0; i < totalSlotCount; i++)
            {
                if (_visibleObjectIndexSet.Contains(i))
                {
                    continue;
                }

                GameObject go = _objects[i];
                if (go)
                {
                    go.transform.SetSiblingIndex(siblingIndex);
                    siblingIndex++;
                }
            }
        }
    }
}
