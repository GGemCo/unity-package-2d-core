using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    ///  기본 설정 
    /// </summary>
    public class OptionDefault : MonoBehaviour
    {
        private UIWindowOption _windowOption;
        
        [Header("기본 옵션 설정")]
        [Tooltip("언어 선택 드롭 다운 메뉴")]
        [SerializeField] private TMP_Dropdown dropdownLanguage;
        [Tooltip("변경한 내용 적용 버튼")]
        [SerializeField] private Button buttonConfirm;
        [Tooltip("디폴트 값으로 초기화 버튼")]
        [SerializeField] private Button buttonReset;
        [Tooltip("변경한 내용 취소 버튼")]
        [SerializeField] private Button buttonCancel;
        
        [Header("사운드 옵션 설정")]
        [Tooltip("메인 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeMaster;
        [Tooltip("BGM 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeBgm;
        [Tooltip("효과음 볼륨 조절 슬라이더")]
        [SerializeField] private Slider sliderVolumeSfx;
        
        public void Initialize(UIWindowOption uiWindowOption)
        {
            _windowOption = uiWindowOption;
        }
    }
}