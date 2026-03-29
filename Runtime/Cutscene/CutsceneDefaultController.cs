using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 연출에서 공통으로 사용하는 기본 기능을 제공하는 컨트롤러입니다.
    /// 캐릭터 유형과 식별자에 따라 대상 Transform을 조회하는 기능을 포함합니다.
    /// </summary>
    public class CutsceneDefaultController
    {
        /// <summary>
        /// 현재 컷신 연출을 관리하는 매니저입니다.
        /// </summary>
        protected CutsceneManager CutsceneManager;

        /// <summary>
        /// 캐릭터 유형과 고유 식별자를 기준으로 컷신 대상 Transform을 조회합니다.
        /// 플레이어는 단일 인스턴스를 사용하며, NPC와 몬스터는 UID를 통해 맵 매니저에서 검색합니다.
        /// </summary>
        /// <param name="type">조회할 대상의 캐릭터 유형입니다.</param>
        /// <param name="characterUid">NPC 또는 몬스터 조회에 사용하는 캐릭터 고유 식별자입니다.</param>
        /// <returns>
        /// 조회된 대상의 <see cref="Transform"/>입니다.
        /// 대상이 없거나 조회에 실패한 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        protected Transform GetTargetTransform(CharacterConstants.Type type, int characterUid)
        {
            // cam.gameObject.SetActive(true);
            Transform newTarget = null;

            if (type == CharacterConstants.Type.Player)
            {
                newTarget = SceneGame.Instance.player.transform;
            }
            else if (type == CharacterConstants.Type.Npc)
            {
                newTarget = SceneGame.Instance.mapManager.GetNpcByUid(characterUid)?.gameObject.transform;
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                newTarget = SceneGame.Instance.mapManager.GetMonsterByUid(characterUid)?.gameObject.transform;
            }

            return newTarget;
        }
    }
}