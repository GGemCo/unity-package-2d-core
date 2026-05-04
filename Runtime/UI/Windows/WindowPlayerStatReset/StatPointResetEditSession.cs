namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 스탯 초기화 창에서 사용하는 임시 편집 세션입니다.
    /// 원본 스탯 포인트 상태를 보존한 채, 화면에서는 모든 투자 포인트를 미사용 포인트로 되돌린 드래프트를 제공합니다.
    /// </summary>
    public sealed class StatPointResetEditSession
    {
        private readonly Player _player;

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

        /// <summary>
        /// 원본과 현재 드래프트의 스탯 포인트 상태가 다른지 반환합니다.
        /// </summary>
        public bool IsDirty =>
            DraftUnspent != _originalUnspent ||
            DraftAtk != _originalAtk ||
            DraftDef != _originalDef ||
            DraftHp != _originalHp ||
            DraftMp != _originalMp ||
            DraftStamina != _originalStamina;

        /// <summary>
        /// 지정한 플레이어의 현재 스탯 포인트 상태를 스냅샷으로 저장하고, 초기화 드래프트를 생성합니다.
        /// </summary>
        /// <param name="player">스탯 초기화를 미리보기할 플레이어입니다.</param>
        public StatPointResetEditSession(Player player)
        {
            _player = player;
            _originalUnspent = player != null ? player.UnspentStatPoints : 0;
            _originalAtk = player != null ? player.InvestedStatPointAtk : 0;
            _originalDef = player != null ? player.InvestedStatPointDef : 0;
            _originalHp = player != null ? player.InvestedStatPointHp : 0;
            _originalMp = player != null ? player.InvestedStatPointMp : 0;
            _originalStamina = player != null ? player.InvestedStatPointStamina : 0;
            _originalInvestedTotal = _originalAtk + _originalDef + _originalHp + _originalMp + _originalStamina;

            ResetToClearedDraft();
        }

        /// <summary>
        /// 현재 세션이 지정한 플레이어와 같은 인스턴스를 대상으로 하는지 확인합니다.
        /// </summary>
        /// <param name="player">비교할 플레이어입니다.</param>
        /// <returns>같은 플레이어를 대상으로 하면 true를 반환합니다.</returns>
        public bool IsSamePlayer(Player player) => ReferenceEquals(_player, player);

        /// <summary>
        /// 드래프트가 생성된 뒤 플레이어의 실제 스탯 포인트 상태가 외부에서 변경되었는지 확인합니다.
        /// </summary>
        /// <returns>원본 스냅샷과 현재 플레이어 데이터가 다르면 true를 반환합니다.</returns>
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

        /// <summary>
        /// 화면용 드래프트를 초기화 상태로 전환합니다.
        /// 기존 투자 포인트 전체를 미사용 포인트로 되돌리고, 각 스탯 투자값은 0으로 설정합니다.
        /// </summary>
        public void ResetToClearedDraft()
        {
            DraftUnspent = _originalUnspent + _originalInvestedTotal;
            DraftAtk = 0;
            DraftDef = 0;
            DraftHp = 0;
            DraftMp = 0;
            DraftStamina = 0;
        }

        /// <summary>
        /// 드래프트를 원본 스냅샷 상태로 되돌립니다.
        /// 취소 또는 창 닫기 시 실제 플레이어 데이터 변경 없이 임시 상태만 폐기할 때 사용합니다.
        /// </summary>
        public void ResetToOriginal()
        {
            DraftUnspent = _originalUnspent;
            DraftAtk = _originalAtk;
            DraftDef = _originalDef;
            DraftHp = _originalHp;
            DraftMp = _originalMp;
            DraftStamina = _originalStamina;
        }

        /// <summary>
        /// 지정한 스탯의 현재 드래프트 투자 포인트를 반환합니다.
        /// </summary>
        /// <param name="type">조회할 스탯 타입입니다.</param>
        /// <returns>드래프트에 투자된 포인트입니다.</returns>
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

        /// <summary>
        /// 지정한 스탯에 드래프트 포인트를 추가할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="type">투자할 스탯 타입입니다.</param>
        /// <returns>투자 가능한 미사용 포인트가 남아 있으면 true를 반환합니다.</returns>
        public bool CanIncrease(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CharacterConstants.IsStatPointTarget(type)) return false;
            return DraftUnspent > 0;
        }

        /// <summary>
        /// 지정한 스탯에서 드래프트 포인트를 회수할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="type">회수할 스탯 타입입니다.</param>
        /// <returns>해당 스탯에 드래프트 투자 포인트가 있으면 true를 반환합니다.</returns>
        public bool CanDecrease(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CharacterConstants.IsStatPointTarget(type)) return false;
            return GetDraftInvested(type) > 0;
        }

        /// <summary>
        /// 지정한 스탯의 드래프트 투자 포인트를 변경합니다.
        /// 변경 중 실패하면 호출 전 상태로 되돌립니다.
        /// </summary>
        /// <param name="type">변경할 스탯 타입입니다.</param>
        /// <param name="delta">증가 또는 감소할 포인트 수입니다.</param>
        /// <returns>변경에 성공하면 true를 반환합니다.</returns>
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

            int count = delta > 0 ? delta : -delta;
            for (int i = 0; i < count; i++)
            {
                bool ok = delta > 0 ? TryIncreaseOne(type) : TryDecreaseOne(type);
                if (ok) continue;

                DraftUnspent = backupUnspent;
                DraftAtk = backupAtk;
                DraftDef = backupDef;
                DraftHp = backupHp;
                DraftMp = backupMp;
                DraftStamina = backupStamina;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 지정한 스탯에 드래프트 포인트를 1개 투자합니다.
        /// </summary>
        /// <param name="type">투자할 스탯 타입입니다.</param>
        /// <returns>투자에 성공하면 true를 반환합니다.</returns>
        private bool TryIncreaseOne(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CanIncrease(type))
            {
                return false;
            }

            AddInvested(type, +1);
            DraftUnspent -= 1;
            return true;
        }

        /// <summary>
        /// 지정한 스탯에서 드래프트 포인트를 1개 회수합니다.
        /// </summary>
        /// <param name="type">회수할 스탯 타입입니다.</param>
        /// <returns>회수에 성공하면 true를 반환합니다.</returns>
        private bool TryDecreaseOne(CharacterConstants.IndexPlayerInfo type)
        {
            if (!CanDecrease(type))
            {
                return false;
            }

            AddInvested(type, -1);
            DraftUnspent += 1;
            return true;
        }

        /// <summary>
        /// 지정한 스탯의 드래프트 투자값에 증감량을 반영합니다.
        /// </summary>
        /// <param name="type">변경할 스탯 타입입니다.</param>
        /// <param name="delta">반영할 증감량입니다.</param>
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
