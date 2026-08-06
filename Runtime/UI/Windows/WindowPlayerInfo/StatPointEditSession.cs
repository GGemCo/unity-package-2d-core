namespace GGemCo2DCore
{
    public sealed class StatPointEditSession
    {
        private readonly Player _player;
        private readonly bool _useReservedGoldBudget;

        private readonly int _originalUnspent;
        private readonly int _originalAtk;
        private readonly int _originalDef;
        private readonly int _originalHp;
        private readonly int _originalMp;
        private readonly int _originalStamina;
        private readonly int _originalInvestedTotal;

        public int DraftUnspent { get; private set; }
        public int DraftAtk { get; private set; }
        public int DraftDef { get; private set; }
        public int DraftHp { get; private set; }
        public int DraftMp { get; private set; }
        public int DraftStamina { get; private set; }
        public long DraftReservedGoldCost { get; private set; }

        public bool IsDirty =>
            DraftUnspent != _originalUnspent ||
            DraftAtk != _originalAtk ||
            DraftDef != _originalDef ||
            DraftHp != _originalHp ||
            DraftMp != _originalMp ||
            DraftStamina != _originalStamina;

        public StatPointEditSession(Player player)
        {
            _player = player;
            _useReservedGoldBudget = player != null && player.UsesReservedGoldBudgetForStatPointDraft();
            _originalUnspent = player != null ? player.UnspentStatPoints : 0;
            _originalAtk = player != null ? player.InvestedStatPointAtk : 0;
            _originalDef = player != null ? player.InvestedStatPointDef : 0;
            _originalHp = player != null ? player.InvestedStatPointHp : 0;
            _originalMp = player != null ? player.InvestedStatPointMp : 0;
            _originalStamina = player != null ? player.InvestedStatPointStamina : 0;
            _originalInvestedTotal = _originalAtk + _originalDef + _originalHp + _originalMp + _originalStamina;

            ResetToOriginal();
        }

        public bool IsSamePlayer(Player player) => ReferenceEquals(_player, player);

        public bool UsesReservedGoldBudget() => _useReservedGoldBudget;

        public long GetPreviewGoldAfterReservation()
        {
            if (_player == null) return 0;
            return _player.GetPreviewGoldAfterReservedStatPointDraft(DraftReservedGoldCost);
        }

        public long GetNextRequiredGoldForIncrease()
        {
            if (!_useReservedGoldBudget || _player == null) return 0;
            if (DraftUnspent > 0) return 0;

            // 현재 드래프트에서 새로 투자한 총량을 기준으로 검사하여,
            // 여러 스탯 항목에 나누어 투자해도 플레이어 최대 레벨을 넘지 않도록 합니다.
            int nextAdditionalInvestCount = GetDraftAdditionalInvestedCount() + 1;
            return _player.GetReservedStatPointDraftPriceForAdditionalInvestCount(nextAdditionalInvestCount);
        }

        /// <summary>
        /// 레벨업 등 외부 요인으로 Player의 실제 포인트 값이 바뀌었는데,
        /// 세션이 이전 스냅샷을 유지하고 있는지 여부를 반환합니다.
        /// 드래프트가 없는 상태에서는 이 값이 true면 세션을 재생성하여 UI를 최신 상태로 동기화합니다.
        /// </summary>
        public bool IsStaleSnapshot()
        {
            if (_player == null) return false;
            return _originalUnspent != _player.UnspentStatPoints
                   || _originalAtk != _player.InvestedStatPointAtk
                   || _originalDef != _player.InvestedStatPointDef
                   || _originalHp != _player.InvestedStatPointHp
                   || _originalMp != _player.InvestedStatPointMp
                   || _originalStamina != _player.InvestedStatPointStamina;
        }

        public void ResetToOriginal()
        {
            DraftUnspent = _originalUnspent;
            DraftAtk = _originalAtk;
            DraftDef = _originalDef;
            DraftHp = _originalHp;
            DraftMp = _originalMp;
            DraftStamina = _originalStamina;
            DraftReservedGoldCost = 0;
        }

        public int GetDraftInvested(CharacterConstants.IndexPlayerInfo type)
        {
            return type switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => DraftAtk,
                CharacterConstants.IndexPlayerInfo.Def => DraftDef,
                CharacterConstants.IndexPlayerInfo.Hp => DraftHp,
                CharacterConstants.IndexPlayerInfo.Mp => DraftMp,
                CharacterConstants.IndexPlayerInfo.Stamina => DraftStamina,
                _ => 0
            };
        }

        private int GetOriginalInvested(CharacterConstants.IndexPlayerInfo type)
        {
            return type switch
            {
                CharacterConstants.IndexPlayerInfo.Atk => _originalAtk,
                CharacterConstants.IndexPlayerInfo.Def => _originalDef,
                CharacterConstants.IndexPlayerInfo.Hp => _originalHp,
                CharacterConstants.IndexPlayerInfo.Mp => _originalMp,
                CharacterConstants.IndexPlayerInfo.Stamina => _originalStamina,
                _ => 0
            };
        }

        private int GetDraftInvestedTotal()
        {
            return DraftAtk + DraftDef + DraftHp + DraftMp + DraftStamina;
        }

        private int GetDraftAdditionalInvestedCount()
        {
            int additionalInvested = GetDraftInvestedTotal() - _originalInvestedTotal;
            return additionalInvested > 0 ? additionalInvested : 0;
        }

        private int GetMinimumAllowedInvested(CharacterConstants.IndexPlayerInfo type)
        {
            if (_useReservedGoldBudget)
            {
                return GetOriginalInvested(type);
            }

            if (_player != null && !_player.CanRefundCommittedStatPoints())
            {
                return GetOriginalInvested(type);
            }

            return 0;
        }

        public bool CanIncrease(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CharacterConstants.IsStatPointTarget(type)) return false;
            if (_player == null) return false;

            int nextAdditionalInvestCount = GetDraftAdditionalInvestedCount() + 1;
            if (!_player.CanInvestAdditionalStatPoints(nextAdditionalInvestCount))
            {
                return false;
            }

            if (!_useReservedGoldBudget)
            {
                return DraftUnspent > 0;
            }

            if (DraftUnspent > 0)
            {
                return true;
            }

            long nextRequiredGold = GetNextRequiredGoldForIncrease();
            if (nextRequiredGold <= 0)
            {
                return false;
            }

            return _player.CanAffordReservedStatPointDraftCost(DraftReservedGoldCost + nextRequiredGold);
        }

        public bool CanDecrease(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CharacterConstants.IsStatPointTarget(type)) return false;
            return GetDraftInvested(type) > GetMinimumAllowedInvested(type);
        }

        public bool TryChange(CharacterConstants.IndexPlayerInfo type, int delta)
        {
            if (delta == 0) return false;
            if (!CharacterConstants.IsStatPointTarget(type)) return false;

            int backupUnspent = DraftUnspent;
            int backupAtk = DraftAtk;
            int backupDef = DraftDef;
            int backupHp = DraftHp;
            int backupMp = DraftMp;
            int backupStamina = DraftStamina;
            long backupReservedGold = DraftReservedGoldCost;

            int count = delta > 0 ? delta : -delta;
            bool ok = true;

            for (int i = 0; i < count; i++)
            {
                ok = delta > 0 ? TryIncreaseOne(type) : TryDecreaseOne(type);
                if (!ok)
                {
                    DraftUnspent = backupUnspent;
                    DraftAtk = backupAtk;
                    DraftDef = backupDef;
                    DraftHp = backupHp;
                    DraftMp = backupMp;
                    DraftStamina = backupStamina;
                    DraftReservedGoldCost = backupReservedGold;
                    return false;
                }
            }

            return true;
        }

        private bool TryIncreaseOne(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CanIncrease(type))
            {
                return false;
            }

            AddInvested(type, +1);

            if (_useReservedGoldBudget)
            {
                SyncReservedGoldBudget();
                return true;
            }

            DraftUnspent -= 1;
            return true;
        }

        private bool TryDecreaseOne(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CanDecrease(type))
            {
                return false;
            }

            AddInvested(type, -1);

            if (_useReservedGoldBudget)
            {
                SyncReservedGoldBudget();
                return true;
            }

            DraftUnspent += 1;
            return true;
        }

        private void SyncReservedGoldBudget()
        {
            int additionalInvested = GetDraftAdditionalInvestedCount();
            int remainingFreePoints = _originalUnspent - additionalInvested;
            DraftUnspent = remainingFreePoints > 0 ? remainingFreePoints : 0;
            DraftReservedGoldCost = _player != null
                ? _player.CalculateReservedStatPointDraftGoldCost(_originalUnspent, _originalInvestedTotal, GetDraftInvestedTotal())
                : 0;
        }

        private void AddInvested(CharacterConstants.IndexPlayerInfo type, int delta)
        {
            switch (type)
            {
                case CharacterConstants.IndexPlayerInfo.Atk:
                    DraftAtk += delta;
                    break;
                case CharacterConstants.IndexPlayerInfo.Def:
                    DraftDef += delta;
                    break;
                case CharacterConstants.IndexPlayerInfo.Hp:
                    DraftHp += delta;
                    break;
                case CharacterConstants.IndexPlayerInfo.Mp:
                    DraftMp += delta;
                    break;
                case CharacterConstants.IndexPlayerInfo.Stamina:
                    DraftStamina += delta;
                    break;
            }
        }
    }
}
