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

        /// <summary>현재 페이지 번호 (1부터 시작)</summary>
        private int _currentPage;

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
        /// Start()에서 pageContainer 자식 중 UISlot을 전부 수집하고
        /// 첫 페이지를 구성한다.
        /// </summary>
        private void Start()
        {
            // 자식으로 붙어 있는 UISlot 컴포넌트들을 전부 가져온다.
            var uiSlots = pageContainer.GetComponentsInChildren<UISlot>(true);
            int len = uiSlots.Length;

            _objects = new GameObject[len];
            _slots   = new UISlot[len];

            // UISlot과 그 GameObject를 배열로 저장
            int idx = 0;
            for (int i = 0; i < uiSlots.Length; i++)
            {
                var slot = uiSlots[i];
                _slots[idx]   = slot;
                _objects[idx] = slot.gameObject;

                // 필터 미적용 상태에서는 모두 보이도록 기본값 true
                slot.isFiltering = true;
                idx++;
            }

            // 최초 페이지 UI 구성
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

            // 현재 필터링 상태에서 보이는 슬롯 인덱스만 수집
            _visibleSlotIndices.Clear();
            for (int i = 0; i < totalSlotCount; i++)
            {
                var go   = _objects[i];
                var slot = (_slots != null && i < _slots.Length) ? _slots[i] : null;

                if (!go) continue;

                // slot == null 이면 필터 없이 항상 보이는 것으로 처리
                bool isVisibleByFilter = slot == null || slot.isFiltering;
                if (isVisibleByFilter)
                    _visibleSlotIndices.Add(i);
            }

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
            buttonPrev?.onClick.RemoveAllListeners();
            buttonNext?.onClick.RemoveAllListeners();
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
    }
}
