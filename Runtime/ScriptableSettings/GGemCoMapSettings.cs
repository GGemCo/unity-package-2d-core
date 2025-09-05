using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Map.FileName, menuName = ConfigScriptableObject.Map.MenuName, order = ConfigScriptableObject.Map.Ordering)]
    public class GGemCoMapSettings : ScriptableObject
    {
        [Header("타일맵 설정")]
        [Tooltip("타일맵에 사용되는 Grid 오브젝트의 Cell 크기 (X, Y 단위)")]
        public Vector2 tilemapGridCellSize;

        [Header("게임 시작 설정")]
        [Tooltip("첫 게임 실행 시 로딩되는 맵의 고유번호 (테이블 참조)")]
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