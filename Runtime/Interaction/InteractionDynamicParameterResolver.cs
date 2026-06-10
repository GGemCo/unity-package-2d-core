namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 인터랙션에서 사용하는 동적 대사 파라미터를 런타임 값으로 해석합니다.
    /// </summary>
    public static class InteractionDynamicParameterResolver
    {
        /// <summary>
        /// 지정한 동적 파라미터 키를 현재 런타임 값으로 해석합니다.
        /// </summary>
        /// <param name="dynamicParameterKey">NPC 테이블에 저장된 동적 파라미터 키입니다.</param>
        /// <returns>대사 포맷에 사용할 동적 텍스트 컨텍스트입니다.</returns>
        public static InteractionDialogueTextContext Resolve(string dynamicParameterKey)
        {
            if (string.IsNullOrWhiteSpace(dynamicParameterKey))
            {
                return InteractionDialogueTextContext.Empty;
            }

            string normalizedKey = dynamicParameterKey.Trim();
            switch (normalizedKey)
            {
                case InteractionDynamicParameterKeys.PlayerStatPointResetCost:
                    return ResolvePlayerStatPointResetCost();
                default:
                    GcLogger.Log($"알 수 없는 InteractionDynamicParameterKey 입니다. key: {normalizedKey}");
                    return InteractionDialogueTextContext.Empty;
            }
        }

        /// <summary>
        /// 플레이어 스탯 초기화 비용을 대사 파라미터로 변환합니다.
        /// </summary>
        /// <returns>스탯 초기화 비용이 들어있는 텍스트 컨텍스트입니다.</returns>
        private static InteractionDialogueTextContext ResolvePlayerStatPointResetCost()
        {
            GGemCoPlayerStatSettings playerStatSettings = AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.playerStatSettings
                : null;

            if (playerStatSettings == null)
            {
                return InteractionDialogueTextContext.Empty;
            }

            return InteractionDialogueTextContext.FromArgs(playerStatSettings.statPointResetCost);
        }
    }
}
