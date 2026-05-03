namespace GGemCo2DCore
{
    public static class UIWindowConstants
    {
        // 윈도우 고유번호 
        public enum WindowUid 
        {
            None,
            Hud,
            Inventory,
            ItemInfo,
            Equip,
            PlayerInfo,
            ItemSplit,
            PlayerBuffInfo,
            QuickSlot,
            Skill,
            SkillInfo,
            InteractionDialogue,
            Shop,
            ItemBuy,
            Stash,
            ShopSale,
            ItemUpgrade,
            ItemSalvage,
            ItemCraft,
            Dialogue,
            HudQuest,
            QuestReward,
            SaveData,
            Option,
            QuickSlotSimulation,
            InputField,
            TcgGameMenu,
            TcgCardCollection,
            TcgMyDeck,
            TcgMyDeckCard,
            TcgCardInfo,
            TcgHandEnemy,
            TcgHandPlayer,
            TcgFieldEnemy,
            TcgFieldPlayer,
            TcgBattleHud,
            BattleHudMonster,
            SkillPassive,
            WorldMap,
            WorldMapInfo,
            PlayerStatReset,
            TimingBattleRest = 100,
            TimingBattleSkillSettingTapMenu,
            TimingBattleTapMenu,
            TimingBattleWarp,
            TimingBattleExit,
            TimingBattleSkillMap,
            TimingBattleBuySlot,
            TimingBattleSkillSettingCombo,
            TimingBattleSkillSettingJustGuard,
            TimingBattleSkillSettingCounter,
            TimingBattleSkillSettingNormal,
            TimingBattleSkillSettingPassive,
        }
        public const string TitleHeaderCommon = "[공통속성]";
        public const string TitleHeaderIndividual = "[개별속성]";
        
        /// <summary>
        /// UIWindowManager가 윈도우 표시 상태를 적용할 때 사용할 모드입니다.
        /// </summary>
        public enum UIWindowVisibilityApplyMode
        {
            /// <summary>
            /// 기존 Show/Hide 경로를 사용합니다.
            /// 애니메이션, OnShow, 연계 윈도우 처리까지 모두 수행합니다.
            /// </summary>
            Normal = 0,

            /// <summary>
            /// 애니메이션과 OnShow 호출 없이 즉시 표시 상태만 변경합니다.
            /// </summary>
            ImmediateSilent = 1,
        }
    }
}