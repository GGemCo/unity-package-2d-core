using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 공용
    /// </summary>
    public class UIIcon : MonoBehaviour
    {
        [Header("오브젝트")]
        [Tooltip("개수를 표현할 텍스트")]
        public TextMeshProUGUI textCount;
        [Tooltip("쿨타임 게이지")]
        public Image imageCoolTimeGauge;
        [Tooltip("선택되었을때 보여줄 이미지")]
        [HideInInspector] public Image imageSelected;
        [Tooltip("잠금 표시 이미지")]
        public Image imageLock;

        private bool _isSelected;
        
        // 윈도우 
        [HideInInspector] public UIWindow window;
        // 윈도우 고유번호
        [HideInInspector] public UIWindowManager.WindowUid windowUid;
        // 번호
        [HideInInspector] public int index;
        // 슬롯 번호
        [HideInInspector] public int slotIndex;
        // 고유번호 (아이템일때는 아이템 고유번호)
        [HideInInspector] public int uid;
        
        // 부모 윈도우 uid
        private UIWindowManager.WindowUid _parentWindowUid;
        // 부모 아이콘 슬롯 index
        private int _parentSlotIndex;

        protected IconConstants.Type IconType;
        private IconConstants.Status _iconStatus;

        // 아이콘 이미지
        protected Image ImageIcon;
        // 개수
        private int _count;
        // 레벨
        private int _level;
        // 배웠는지
        private bool _isLearn;
        // 등급
        private IconConstants.Grade _grade;
        // 등급 아이콘
        [HideInInspector] public Image imageGrade;
            
        // 드래그 핸들러
        private UIDragHandler _dragHandler;
        private RectTransform _rectTransform;
        protected bool PossibleClick;

        protected virtual void Awake()
        {
            PossibleClick = true;
            uid = 0;
            _count = 0;
            _level = 0;
            index = 0;
            slotIndex = 0;
            _parentWindowUid = 0;
            _parentSlotIndex = 0;
            _isLearn = false;
            window = null;
            windowUid = UIWindowManager.WindowUid.None;
            _iconStatus = IconConstants.Status.Normal;
            IconType = IconConstants.Type.None;
            
            ImageIcon = GetComponent<Image>();
            _dragHandler = gameObject.AddComponent<UIDragHandler>();
            _rectTransform = GetComponent<RectTransform>();

            if (imageCoolTimeGauge != null)
            {
                imageCoolTimeGauge.gameObject.SetActive(false);
            }
            SetSelected(false);
            SetIconLock(false);
        }

        protected virtual void Start()
        {
            // 선택되었을때 보여줄 이미지 크기를 slot size 로 변경
            if (imageSelected == null) return;
            imageSelected.rectTransform.sizeDelta = window.slotSize;
        }

        public void Initialize(UIWindow pwindow, UIWindowManager.WindowUid pwindowUid, int pindex, int pslotIndex, 
            Vector2 iconSize, Vector2 slotSize)
        {
            window = pwindow;
            windowUid = pwindowUid;
            index = pindex;
            slotIndex = pslotIndex;
            SetCount(0);
            ChangeIconImageSize(iconSize, slotSize);
        }

        public bool IsItem() => IconType == IconConstants.Type.Item;
        public bool IsSkill() => IconType == IconConstants.Type.Skill;
        public bool IsBuff() => IconType == IconConstants.Type.Buff;

        /// <summary>
        /// 장비 타입인지
        /// </summary>
        /// <returns></returns>
        public virtual bool IsEquipType()
        {
            return false;
        }
        /// <summary>
        /// 물약 타입인지
        /// </summary>
        /// <returns></returns>
        public virtual bool IsPotionType()
        {
            return false;
        }
        /// <summary>
        /// 생명력 물약인지
        /// </summary>
        /// <returns></returns>
        public virtual bool IsHpPotionType()
        {
            return false;
        }
        /// <summary>
        /// 마력 물약인지
        /// </summary>
        /// <returns></returns>
        public virtual bool IsMpPotionType()
        {
            return false;
        }
        /// <summary>
        /// 어펙트 옵션이 있는지 
        /// </summary>
        /// <returns></returns>
        public virtual bool IsAffectUid()
        {
            return false;
        }
        public IconConstants.Type GetIconType() => IconType;
        public IconConstants.Grade GetGrade() => _grade;
        private void SetStatus(IconConstants.Status status) => this._iconStatus = status;

        protected bool IsLock()
        {
            return _iconStatus == IconConstants.Status.Lock;
        }

        protected void UpdateInfo()
        {
            if (uid <= 0) return;
            UpdateIconImage();
        }
        /// <summary>
        /// 다른 uid 로 변경하기
        /// </summary>
        /// <param name="iconUid"></param>
        /// <param name="iconCount"></param>
        /// <param name="iconLevel"></param>
        /// <param name="iconIsLearn"></param>
        /// <param name="remainCoolTime"></param>
        public virtual bool ChangeInfoByUid(int iconUid, int iconCount = 0, int iconLevel = 0, bool iconIsLearn = false, int remainCoolTime = 0)
        {
            SceneGame.Instance.uIIconCoolTimeManager.SetRemainCoolTime(windowUid, iconUid, remainCoolTime);
            
            if (iconUid == 0 && iconCount == 0)
            {
                ClearIconInfos();
                return false;
            }
            uid = iconUid;
            SetCount(iconCount);
            SetLevel(iconLevel);
            SetIsLearn(iconIsLearn);
            return true;
        }
        /// <summary>
        /// 개수 추가하기
        /// </summary>
        /// <param name="value"></param>
        public void AddCount(int value) => SetCount(_count + value);
        /// <summary>
        /// 총 개수 가져오기
        /// </summary>
        /// <param name="value"></param>
        public void SetCount(int value)
        {
            _count = value;
            if (textCount != null)
            {
                textCount.text = _count <= 1 ? "" : _count.ToString();
            }
        }
        /// <summary>
        /// 아이템 잠금
        /// </summary>
        /// <param name="set"></param>
        public void SetIconLock(bool set)
        {
            SetStatus(set ? IconConstants.Status.Lock : IconConstants.Status.Normal);

            if (imageLock != null)
            {
                imageLock.gameObject.SetActive(set);
            }

            SetDrag(!set);
        }
        /// <summary>
        /// 드래그 가능 여부 on/off
        /// </summary>
        /// <param name="set"></param>
        public void SetDrag(bool set)
        {
            if (_dragHandler == null) return;
            _dragHandler.SetIsPossibleDrag(set);
        }
        /// <summary>
        /// 아이템 정보 지우기
        /// </summary>
        public void ClearIconInfos()
        {
            SceneGame.Instance.uIIconCoolTimeManager.ResetCoolTime(windowUid, uid);
            
            uid = 0;
            Sprite newSprite = AddressableLoaderItem.Instance.GetImageIconItemByName("blank");
            if (ImageIcon != null)
            {
                ImageIcon.sprite = newSprite;
            }
            if (imageGrade != null)
            {
                imageGrade.sprite = newSprite;
            }

            SetIconLock(false);
            SetCount(0);
        }
        /// <summary>
        /// 아이콘 이미지 경로 가져오기 
        /// </summary>
        /// <returns></returns>
        protected virtual string GetIconImagePath()
        {
            return "";
        }
        /// <summary>
        /// 아이콘 이미지 업데이트 하기
        /// </summary>
        protected virtual void UpdateIconImage()
        {
            if (ImageIcon == null) return;
            string path = GetIconImagePath();
            if (string.IsNullOrEmpty(path))
            {
                ImageIcon.sprite = null;
                return;
            }

            ImageIcon.sprite = AddressableLoaderItem.Instance.GetImageIconItemByName(path);
        }
        /// <summary>
        /// 이미지 사이즈 변경하기
        /// ray cast 사이즈를 슬롯 사이즈와 같게 변경하기 
        /// </summary>
        /// <param name="size"></param>
        /// <param name="slotSize"></param>
        private void ChangeIconImageSize(Vector2 size, Vector2 slotSize)
        {
            _rectTransform.sizeDelta = size;
            var diff = (slotSize.x - size.x)/2;
            ImageIcon.raycastPadding = new Vector4(-diff, -diff, -diff, -diff);
        }
        /// <summary>
        /// 아이콘의 원래 위치 가져오기
        /// </summary>
        /// <returns></returns>
        public Vector3 GetDragOriginalPosition()
        {
            return _dragHandler.GetOriginalPosition();
        }
        public void SetSelected(bool selected)
        {
            if (imageSelected == null) return;
            _isSelected = selected;
            ShowSelected(selected);
        }

        protected void ShowSelected(bool selected)
        {
            if (imageSelected == null) return;
            // 선택된 아이콘이면 끄지 않는다.
            if (_isSelected && !selected) return;
            imageSelected.gameObject.SetActive(selected);
        }
        public virtual ItemConstants.PartsType GetPartsType()
        {
            return ItemConstants.PartsType.None;
        }
        /// <summary>
        /// item 테이블에 StatusValue1 컬럼값 가져오기
        /// </summary>
        /// <returns></returns>
        public virtual int GetStatusValue1()
        {
            return 0;
        }
        /// <summary>
        /// item 테이블에 StatusID1 컬럼값 가져오기
        /// </summary>
        /// <returns></returns>
        public virtual string GetStatusId1()
        {
            return "";
        }
        /// <summary>
        /// item 테이블에 duration 컬럼값 가져오기
        /// </summary>
        /// <returns></returns>
        public virtual float GetDuration()
        {
            return 0;
        }

        public void SetPosition(Vector3 position)
        {
            transform.localPosition = position;
        }

        public virtual bool CheckRequireLevel()
        {
            return false;
        }

        private void SetLevel(int value)
        {
            _level = value;
        }

        private void SetIsLearn(bool value)
        {
            _isLearn = value;
        }
        public int GetLevel() => _level;
        public int GetCount() => _count;

        public bool IsLearn() => _isLearn;
        public Sprite GetImageIconSprite() => ImageIcon.sprite;

        public virtual ConfigCommon.SuffixType GetStatusSuffix1()
        {
            return ConfigCommon.SuffixType.None;
        }

        public virtual void CheckStatusAffect()
        {
        }
        /// <summary>
        /// 쿨타임 시작하기
        /// </summary>
        /// <param name="coolTime"></param>
        /// <returns></returns>
        public bool PlayCoolTime(float coolTime)
        {
            float time = SceneGame.Instance.uIIconCoolTimeManager.GetCurrentCoolTime(windowUid, uid);
            if (time > 0)
            {
                SceneGame.Instance.systemMessageManager.ShowMessageWarning("쿨타임 중에는 사용할 수 없습니다.");
                return false;
            }
            
            return SceneGame.Instance.uIIconCoolTimeManager.StartHandler(windowUid, this, coolTime);
        }
        /// <summary>
        /// Raycast Target 설정
        /// </summary>
        /// <param name="set"></param>
        public void SetRaycastTarget(bool set)
        {
            if (ImageIcon == null) return;
            ImageIcon.raycastTarget = set;
        }

        public virtual bool IsAntiFlag(ItemConstants.AntiFlag flag)
        {
            return false;
        }
        /// <summary>
        /// Regist 되었을때 부모 윈도우와 slot index 정보 셋팅하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        public void SetParentInfo(UIWindowManager.WindowUid fromWindowUid, int fromIndex)
        {
            _parentWindowUid = fromWindowUid;
            _parentSlotIndex = fromIndex;
        }
        public (UIWindowManager.WindowUid, int) GetParentInfo()
        {
            return (_parentWindowUid, _parentSlotIndex);
        }

        public virtual float GetCoolTime()
        {
            return 0;
        }
        /// <summary>
        /// lock 이미지를 사용안하도록 삭제처리 하기
        /// </summary>
        public void RemoveLockImage()
        {
            imageLock = null;
        }

        public void SetClick(bool set)
        {
            PossibleClick = set;
        }
    }
}
