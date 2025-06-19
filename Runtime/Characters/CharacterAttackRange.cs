using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 캐릭터 공격 거리 관리
    /// </summary>
    public class CharacterAttackRange : MonoBehaviour
    {
        public CharacterBase target;
        
        public void Initialize(CharacterBase character)
        {
            target = character;
            transform.SetParent(target.gameObject.transform);
            transform.SetParent(gameObject.transform);
            tag = character.tag;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }
    }
}