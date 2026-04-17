namespace GGemCo2DCore
{
    /// <summary>
    /// 게임별 커스텀 인터렉션 실행기입니다.
    /// Core는 구현을 모르고, 상위 게임 런타임이 등록만 합니다.
    /// </summary>
    public interface IInteractionCustomHandler
    {
        bool TryGetDisplayName(int value, out string displayName);
        bool TryExecute(SceneGame sceneGame, CharacterBase npc, int value);
    }
}
