using TMPro;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 stat 정보 보여주는 윈도우
    /// </summary>
    public class UIWindowPlayerInfo : UIWindow
    {
        /// <summary>
        /// Player 클레스에서 subscribe 를 위해 사용중
        /// </summary>
        public enum IndexPlayerInfo
        {
            None,
            Atk,
            Def,
            Hp,
            Mp,
            MoveSpeed,
            AttackSpeed,
            CriticalDamage,
            CriticalProbability,
            RegistFire,
            RegistCold,
            RegistLightning,
        }
        
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("공격력")]
        public TextMeshProUGUI textTotalAtk;
        [Tooltip("방어력")]
        public TextMeshProUGUI textTotalDef;
        [Tooltip("생명력")]
        public TextMeshProUGUI textTotalHp;
        [Tooltip("마력")]
        public TextMeshProUGUI textTotalMp;
        [Tooltip("이동속도")]
        public TextMeshProUGUI textTotalMoveSpeed;
        [Tooltip("공격속도")]
        public TextMeshProUGUI textTotalAttackSpeed;
        [Tooltip("불 저항력")]
        public TextMeshProUGUI textTotalRegistFire;
        [Tooltip("얼음 저항력")]
        public TextMeshProUGUI textTotalRegistCold;
        [Tooltip("전기 저항력")]
        public TextMeshProUGUI textTotalRegistLightning;
        [HideInInspector] public TextMeshProUGUI textTotalCriticalDamage;
        [HideInInspector] public TextMeshProUGUI textTotalCriticalProbability;

        private Dictionary<IndexPlayerInfo, TextMeshProUGUI> playerInfos = new();
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.PlayerInfo;
            if (TableLoaderManager.Instance == null) return;
            base.Awake();
            
            playerInfos = new()
            {
                { IndexPlayerInfo.Atk, textTotalAtk },
                { IndexPlayerInfo.Def, textTotalDef },
                { IndexPlayerInfo.Hp, textTotalHp },
                { IndexPlayerInfo.Mp, textTotalMp },
                { IndexPlayerInfo.MoveSpeed, textTotalMoveSpeed },
                { IndexPlayerInfo.AttackSpeed, textTotalAttackSpeed },
                { IndexPlayerInfo.CriticalDamage, textTotalCriticalDamage },
                { IndexPlayerInfo.CriticalProbability, textTotalCriticalProbability },
                { IndexPlayerInfo.RegistFire, textTotalRegistFire },
                { IndexPlayerInfo.RegistCold, textTotalRegistCold},
                { IndexPlayerInfo.RegistLightning, textTotalRegistLightning },
            };
        }
        public void UpdateText(IndexPlayerInfo index, string label, long value)
        {
            if (index == IndexPlayerInfo.None) return;
            if (playerInfos.TryGetValue(index, out var textUI) && textUI != null)
            {
                textUI.text = $"{label}: {value}";
            }
        }
    }
}
