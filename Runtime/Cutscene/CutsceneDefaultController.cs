using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 공통 컨트롤러
    /// </summary>
    public class CutsceneDefaultController
    {
        protected CutsceneManager CutsceneManager;
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