using UnityEngine;

namespace GGemCo.Scripts
{
    [CreateAssetMenu(fileName = "GGemCoMapSettings", menuName = "GGemCo/Settings/GGemCoMapSettings", order = 3)]
    public class GGemCoMapSettings : ScriptableObject
    {
        [Header("맵 설정")] 
        
        [Header("타일맵에 사용되는 Grid 오브젝트의 Cell Size")]
        public Vector2 tilemapGridCellSize;

        [Header("첫 게임 실행시 시작 맵의 고유번호")]
        public int startMapUid;

        /// <summary>
        /// 기존 값이 비어있을 때만 기본값을 설정
        /// </summary>
        private void OnEnable()
        {
        }
        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
        }
    }
}