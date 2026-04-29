using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 UI 표시 정책을 관리하는 ScriptableObject 설정입니다.
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObject.WorldMap.FileName,
        menuName = ConfigScriptableObject.WorldMap.MenuName, order = ConfigScriptableObject.WorldMap.Ordering)]
    public class GGemCoWorldMapSettings : ScriptableObject
    {
        [Header("노드 타입 데코레이션")]
        [Tooltip("월드맵 노드 타입별로 아이콘 위에 덧씌울 데코레이션 스프라이트입니다.")]
        public List<WorldMapNodeTypeDecorationData> nodeTypeDecorations = new List<WorldMapNodeTypeDecorationData>();

        /// <summary>
        /// 지정한 월드맵 노드 타입에 연결된 데코레이션 스프라이트를 반환합니다.
        /// </summary>
        /// <param name="nodeType">데코레이션을 조회할 월드맵 노드 타입입니다.</param>
        /// <returns>등록된 데코레이션 스프라이트입니다. 등록값이 없으면 null을 반환합니다.</returns>
        public Sprite GetDecorationSprite(WorldMapNodeType nodeType)
        {
            if (nodeTypeDecorations == null)
            {
                return null;
            }

            for (int i = 0; i < nodeTypeDecorations.Count; i++)
            {
                WorldMapNodeTypeDecorationData data = nodeTypeDecorations[i];
                if (data != null && data.nodeType == nodeType)
                {
                    return data.decoSprite;
                }
            }

            return null;
        }

        /// <summary>
        /// 처음 생성될 때 기본 노드 타입 항목을 준비합니다.
        /// </summary>
        private void Reset()
        {
            nodeTypeDecorations = new List<WorldMapNodeTypeDecorationData>();
            Array nodeTypes = Enum.GetValues(typeof(WorldMapNodeType));
            for (int i = 0; i < nodeTypes.Length; i++)
            {
                nodeTypeDecorations.Add(new WorldMapNodeTypeDecorationData
                {
                    nodeType = (WorldMapNodeType)nodeTypes.GetValue(i),
                    decoSprite = null,
                });
            }
        }
    }

    /// <summary>
    /// 월드맵 노드 타입과 데코레이션 스프라이트를 연결하는 설정 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapNodeTypeDecorationData
    {
        [Tooltip("데코레이션을 적용할 월드맵 노드 타입입니다.")]
        public WorldMapNodeType nodeType;

        [Tooltip("해당 노드 타입 아이콘 위에 표시할 데코레이션 스프라이트입니다.")]
        public Sprite decoSprite;
    }
}
