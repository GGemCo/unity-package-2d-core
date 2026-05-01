using UnityEngine;

namespace GGemCo2DCore
{
    [CreateAssetMenu(fileName = ConfigScriptableObject.Map.FileName, menuName = ConfigScriptableObject.Map.MenuName, order = ConfigScriptableObject.Map.Ordering)]
    public class GGemCoMapSettings : ScriptableObject
    {
        [Header("맵 사용여부")] 
        [Tooltip("맵 기능 사용여부")]
        public bool useMap;
        
        [Header("타일맵 Cell Size 설정")] 
        [Tooltip("타일맵에 사용되는 Grid 오브젝트의 Cell 크기 (X, Y 단위)")]
        public Vector2 tilemapGridCellSize;

        [Header("게임 시작 맵 설정")]
        [Tooltip("첫 게임 실행 시 로딩되는 맵의 고유번호 (테이블 참조)")]
        public int startMapUid;
        
        [Header("Debug")]
        [SerializeField, DebugOption("맵 디버그 기능 전체 사용 여부입니다.")]
        private bool enableMapDebug;
        public bool EnableMapDebug => DebugOptionRuntimeUtility.Resolve(enableMapDebug);
        
        [SerializeField, DebugOption("맵 이름 옆에 Uid 출력")]
        private bool enableMapUid;
        public bool EnableMapUid => EnableMapDebug && DebugOptionRuntimeUtility.Resolve(enableMapUid);
        

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            useMap = true;
            enableMapDebug = false;
            enableMapUid = false;
        }
    }
}