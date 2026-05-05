using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 좌우 버튼을 사용하여 옵션 목록의 이전/다음 값을 선택하는 공통 UI 컴포넌트입니다.
    /// </summary>
    public class UIOptionSelector : MonoBehaviour
    {
        /// <summary>
        /// 옵션이 변경되었을 때 호출되는 UnityEvent입니다.
        /// </summary>
        [Serializable]
        public class OptionChangedEvent : UnityEvent<int, string, string>
        {
        }

        /// <summary>
        /// 선택 가능한 옵션 1건을 표현하는 직렬화 데이터입니다.
        /// </summary>
        [Serializable]
        public struct OptionItem
        {
            [Tooltip("저장 또는 비교에 사용할 고정 식별자입니다.")]
            [SerializeField] private string id;

            [Tooltip("화면에 표시할 문구입니다.")]
            [SerializeField] private string displayText;

            /// <summary>저장 또는 비교에 사용할 고정 식별자입니다.</summary>
            public string Id => id;

            /// <summary>화면에 표시할 문구입니다.</summary>
            public string DisplayText => displayText;

            /// <summary>
            /// 옵션 항목을 생성합니다.
            /// </summary>
            /// <param name="id">저장 또는 비교에 사용할 고정 식별자입니다.</param>
            /// <param name="displayText">화면에 표시할 문구입니다.</param>
            public OptionItem(string id, string displayText)
            {
                this.id = id ?? string.Empty;
                this.displayText = displayText ?? string.Empty;
            }
        }

        [Header("UI 참조")]
        [Tooltip("이전 옵션으로 이동하는 버튼입니다.")]
        [SerializeField] private Button buttonPrev;

        [Tooltip("다음 옵션으로 이동하는 버튼입니다.")]
        [SerializeField] private Button buttonNext;

        [Tooltip("현재 선택된 옵션 문구를 표시하는 TMP 텍스트입니다.")]
        [SerializeField] private TextMeshProUGUI textValue;

        [Header("옵션 데이터")]
        [Tooltip("인스펙터에서 기본으로 사용할 옵션 목록입니다.")]
        [SerializeField] private List<OptionItem> items = new List<OptionItem>();

        [Tooltip("현재 선택된 옵션 인덱스입니다.")]
        [SerializeField] private int currentIndex;

        [Tooltip("끝에서 다음을 누르면 처음으로, 처음에서 이전을 누르면 끝으로 순환할지 여부입니다.")]
        [SerializeField] private bool loop = true;

        [Tooltip("옵션이 없거나 표시 문구가 비어있을 때 사용할 대체 문구입니다.")]
        [SerializeField] private string emptyText = "-";

        [Header("이벤트")]
        [Tooltip("옵션 변경 시 index, id, displayText 순서로 호출됩니다.")]
        [SerializeField] private OptionChangedEvent onValueChanged = new OptionChangedEvent();

        /// <summary>옵션 변경 시 index, id, displayText 순서로 호출되는 이벤트입니다.</summary>
        public OptionChangedEvent OnValueChanged => onValueChanged;

        /// <summary>현재 선택된 인덱스입니다.</summary>
        public int CurrentIndex => currentIndex;

        /// <summary>현재 옵션 목록의 개수입니다.</summary>
        public int Count => items != null ? items.Count : 0;

        /// <summary>옵션 목록을 끝에서 처음으로 순환할지 여부입니다.</summary>
        public bool Loop
        {
            get => loop;
            set
            {
                loop = value;
                RefreshVisual(false);
            }
        }

        /// <summary>
        /// 버튼 리스너를 등록하고 현재 상태를 화면에 반영합니다.
        /// </summary>
        private void Awake()
        {
            BindButtons();
            RefreshVisual(false);
        }

        /// <summary>
        /// 오브젝트가 활성화될 때 인스펙터 변경사항을 화면에 다시 반영합니다.
        /// </summary>
        private void OnEnable()
        {
            RefreshVisual(false);
        }

        /// <summary>
        /// 컴포넌트가 제거될 때 버튼 리스너를 해제하여 중복 호출을 방지합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnbindButtons();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터에서 값이 변경되었을 때 인덱스 범위와 화면 표시를 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            ClampCurrentIndex();

            if (!Application.isPlaying)
            {
                RefreshVisual(false);
            }
        }
#endif

        /// <summary>
        /// 외부에서 옵션 목록을 교체합니다.
        /// </summary>
        /// <param name="newItems">새로 사용할 옵션 목록입니다.</param>
        /// <param name="startIndex">초기 선택 인덱스입니다.</param>
        /// <param name="notify">변경 이벤트를 호출할지 여부입니다.</param>
        public void SetItems(IReadOnlyList<OptionItem> newItems, int startIndex = 0, bool notify = true)
        {
            items.Clear();

            if (newItems != null)
            {
                for (int i = 0; i < newItems.Count; i++)
                {
                    items.Add(newItems[i]);
                }
            }

            SetIndex(startIndex, notify);
        }

        /// <summary>
        /// 문자열 배열을 옵션 목록으로 변환하여 설정합니다.
        /// </summary>
        /// <param name="displayTexts">id와 displayText로 함께 사용할 문자열 목록입니다.</param>
        /// <param name="startIndex">초기 선택 인덱스입니다.</param>
        /// <param name="notify">변경 이벤트를 호출할지 여부입니다.</param>
        public void SetItems(IReadOnlyList<string> displayTexts, int startIndex = 0, bool notify = true)
        {
            items.Clear();

            if (displayTexts != null)
            {
                for (int i = 0; i < displayTexts.Count; i++)
                {
                    string value = displayTexts[i] ?? string.Empty;
                    items.Add(new OptionItem(value, value));
                }
            }

            SetIndex(startIndex, notify);
        }

        /// <summary>
        /// 현재 선택 인덱스를 지정합니다.
        /// </summary>
        /// <param name="index">선택할 인덱스입니다.</param>
        /// <param name="notify">변경 이벤트를 호출할지 여부입니다.</param>
        public void SetIndex(int index, bool notify = true)
        {
            int previousIndex = currentIndex;
            currentIndex = index;
            ClampCurrentIndex();
            RefreshVisual(false);

            if (notify && previousIndex != currentIndex)
            {
                NotifyChanged();
            }
        }

        /// <summary>
        /// 현재 선택 값을 id 기준으로 지정합니다.
        /// </summary>
        /// <param name="id">찾을 옵션 id입니다.</param>
        /// <param name="notify">변경 이벤트를 호출할지 여부입니다.</param>
        /// <returns>id와 일치하는 옵션을 찾았으면 true입니다.</returns>
        public bool SetValueById(string id, bool notify = true)
        {
            int index = IndexOfId(id);
            if (index < 0)
            {
                return false;
            }

            SetIndex(index, notify);
            return true;
        }

        /// <summary>
        /// 이전 옵션으로 이동합니다.
        /// </summary>
        public void MovePrev()
        {
            Move(-1);
        }

        /// <summary>
        /// 다음 옵션으로 이동합니다.
        /// </summary>
        public void MoveNext()
        {
            Move(1);
        }

        /// <summary>
        /// 현재 선택된 옵션의 id를 반환합니다.
        /// </summary>
        /// <returns>선택된 옵션이 없으면 빈 문자열입니다.</returns>
        public string GetCurrentId()
        {
            return TryGetCurrentItem(out OptionItem item) ? item.Id : string.Empty;
        }

        /// <summary>
        /// 현재 선택된 옵션의 표시 문구를 반환합니다.
        /// </summary>
        /// <returns>선택된 옵션이 없으면 emptyText입니다.</returns>
        public string GetCurrentDisplayText()
        {
            return TryGetCurrentItem(out OptionItem item) ? ResolveDisplayText(item) : emptyText;
        }

        /// <summary>
        /// 현재 선택된 옵션을 반환합니다.
        /// </summary>
        /// <param name="item">현재 선택된 옵션입니다.</param>
        /// <returns>유효한 옵션이 있으면 true입니다.</returns>
        public bool TryGetCurrentItem(out OptionItem item)
        {
            if (items != null && currentIndex >= 0 && currentIndex < items.Count)
            {
                item = items[currentIndex];
                return true;
            }

            item = default;
            return false;
        }

        /// <summary>
        /// 버튼 클릭 리스너를 등록합니다.
        /// </summary>
        private void BindButtons()
        {
            buttonPrev?.onClick.RemoveListener(MovePrev);
            buttonNext?.onClick.RemoveListener(MoveNext);
            buttonPrev?.onClick.AddListener(MovePrev);
            buttonNext?.onClick.AddListener(MoveNext);
        }

        /// <summary>
        /// 버튼 클릭 리스너를 해제합니다.
        /// </summary>
        private void UnbindButtons()
        {
            buttonPrev?.onClick.RemoveListener(MovePrev);
            buttonNext?.onClick.RemoveListener(MoveNext);
        }

        /// <summary>
        /// 지정한 방향으로 현재 인덱스를 이동합니다.
        /// </summary>
        /// <param name="delta">-1이면 이전, 1이면 다음 방향입니다.</param>
        private void Move(int delta)
        {
            int count = Count;
            if (count <= 1)
            {
                RefreshVisual(false);
                return;
            }

            int nextIndex = currentIndex + delta;

            if (loop)
            {
                nextIndex = Mod(nextIndex, count);
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, count - 1);
            }

            SetIndex(nextIndex, true);
        }

        /// <summary>
        /// 현재 선택 인덱스가 옵션 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void ClampCurrentIndex()
        {
            int count = Count;
            if (count <= 0)
            {
                currentIndex = 0;
                return;
            }

            currentIndex = Mathf.Clamp(currentIndex, 0, count - 1);
        }

        /// <summary>
        /// 현재 선택 상태를 텍스트와 버튼 활성 상태에 반영합니다.
        /// </summary>
        /// <param name="notify">갱신 후 변경 이벤트를 호출할지 여부입니다.</param>
        private void RefreshVisual(bool notify)
        {
            ClampCurrentIndex();
            RefreshText();
            RefreshButtons();

            if (notify)
            {
                NotifyChanged();
            }
        }

        /// <summary>
        /// 현재 선택된 옵션 문구를 텍스트에 반영합니다.
        /// </summary>
        private void RefreshText()
        {
            if (textValue == null)
            {
                return;
            }

            textValue.text = TryGetCurrentItem(out OptionItem item)
                ? ResolveDisplayText(item)
                : emptyText;
        }

        /// <summary>
        /// 현재 옵션 개수와 순환 정책에 따라 이전/다음 버튼 상태를 갱신합니다.
        /// </summary>
        private void RefreshButtons()
        {
            int count = Count;
            bool hasMultipleItems = count > 1;

            if (buttonPrev != null)
            {
                buttonPrev.interactable = hasMultipleItems && (loop || currentIndex > 0);
            }

            if (buttonNext != null)
            {
                buttonNext.interactable = hasMultipleItems && (loop || currentIndex < count - 1);
            }
        }

        /// <summary>
        /// 현재 선택된 옵션 정보를 외부 구독자에게 전달합니다.
        /// </summary>
        private void NotifyChanged()
        {
            if (!TryGetCurrentItem(out OptionItem item))
            {
                onValueChanged?.Invoke(-1, string.Empty, emptyText);
                return;
            }

            onValueChanged?.Invoke(currentIndex, item.Id, ResolveDisplayText(item));
        }

        /// <summary>
        /// 옵션 표시 문구를 계산합니다.
        /// DisplayText가 비어 있으면 Id를 사용하고, Id도 비어 있으면 emptyText를 사용합니다.
        /// </summary>
        /// <param name="item">표시할 옵션 항목입니다.</param>
        /// <returns>화면에 표시할 최종 문구입니다.</returns>
        private string ResolveDisplayText(OptionItem item)
        {
            if (!string.IsNullOrEmpty(item.DisplayText))
            {
                return item.DisplayText;
            }

            if (!string.IsNullOrEmpty(item.Id))
            {
                return item.Id;
            }

            return emptyText;
        }

        /// <summary>
        /// id와 일치하는 옵션 인덱스를 찾습니다.
        /// </summary>
        /// <param name="id">찾을 옵션 id입니다.</param>
        /// <returns>찾은 인덱스입니다. 없으면 -1입니다.</returns>
        private int IndexOfId(string id)
        {
            if (items == null)
            {
                return -1;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 음수 입력도 정상 순환되도록 나머지 값을 계산합니다.
        /// </summary>
        /// <param name="value">원본 값입니다.</param>
        /// <param name="mod">나눌 기준 값입니다.</param>
        /// <returns>0 이상 mod 미만의 나머지 값입니다.</returns>
        private static int Mod(int value, int mod)
        {
            if (mod <= 0)
            {
                return 0;
            }

            int result = value % mod;
            return result < 0 ? result + mod : result;
        }
    }
}
