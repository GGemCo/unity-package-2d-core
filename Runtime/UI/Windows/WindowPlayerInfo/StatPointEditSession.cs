namespace GGemCo2DCore
{
    public sealed class StatPointEditSession
    {
        private readonly Player _player;

        private readonly int _originalUnspent;
        private readonly int _originalAtk;
        private readonly int _originalDef;
        private readonly int _originalHp;
        private readonly int _originalMp;
        private readonly int _originalStamina;

        public int DraftUnspent { get; private set; }
        public int DraftAtk { get; private set; }
        public int DraftDef { get; private set; }
        public int DraftHp { get; private set; }
        public int DraftMp { get; private set; }
        public int DraftStamina { get; private set; }

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
            _originalUnspent = player != null ? player.UnspentStatPoints : 0;
            _originalAtk = player != null ? player.InvestedStatPointAtk : 0;
            _originalDef = player != null ? player.InvestedStatPointDef : 0;
            _originalHp = player != null ? player.InvestedStatPointHp : 0;
            _originalMp = player != null ? player.InvestedStatPointMp : 0;
            _originalStamina = player != null ? player.InvestedStatPointStamina : 0;

            ResetToOriginal();
        }

        public bool IsSamePlayer(Player player) => ReferenceEquals(_player, player);

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

        private int GetMinimumAllowedInvested(CharacterConstants.IndexPlayerInfo type)
        {
            if (_player != null && !_player.CanRefundCommittedStatPoints())
            {
                return GetOriginalInvested(type);
            }

            return 0;
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

            // +
            if (delta > 0)
            {
                if (DraftUnspent < delta) return false;

                switch (type)
                {
                    case CharacterConstants.IndexPlayerInfo.Atk: DraftAtk += delta; break;
                    case CharacterConstants.IndexPlayerInfo.Def: DraftDef += delta; break;
                    case CharacterConstants.IndexPlayerInfo.Hp: DraftHp += delta; break;
                    case CharacterConstants.IndexPlayerInfo.Mp: DraftMp += delta; break;
                    case CharacterConstants.IndexPlayerInfo.Stamina: DraftStamina += delta; break;
                    default: return false;
                }

                DraftUnspent -= delta;
                return true;
            }

            // -
            int amount = -delta;
            int currentValue = GetDraftInvested(type);
            int nextValue = currentValue - amount;
            if (nextValue < GetMinimumAllowedInvested(type)) return false;

            switch (type)
            {
                case CharacterConstants.IndexPlayerInfo.Atk:
                    DraftAtk = nextValue;
                    break;
                case CharacterConstants.IndexPlayerInfo.Def:
                    DraftDef = nextValue;
                    break;
                case CharacterConstants.IndexPlayerInfo.Hp:
                    DraftHp = nextValue;
                    break;
                case CharacterConstants.IndexPlayerInfo.Mp:
                    DraftMp = nextValue;
                    break;
                case CharacterConstants.IndexPlayerInfo.Stamina:
                    DraftStamina = nextValue;
                    break;
                default:
                    return false;
            }

            DraftUnspent += amount;
            return true;
        }
    }
}