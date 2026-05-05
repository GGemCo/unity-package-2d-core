using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// interaction 전용 대화 그래프 진행 상태를 관리하는 런타임 세션입니다.
    /// </summary>
    public sealed class InteractionDialogueRuntimeSession
    {
        private readonly Dictionary<string, DialogueNodeData> _nodes = new(StringComparer.Ordinal);
        private DialogueNodeData _currentNode;
        private bool _isCompleted;

        /// <summary>
        /// 현재 대화 그래프가 로드되어 있는지 여부입니다.
        /// </summary>
        public bool HasDialogue => _nodes.Count > 0;

        /// <summary>
        /// 현재 노드가 활성 상태인지 여부입니다.
        /// </summary>
        public bool IsActive => HasDialogue && !_isCompleted && _currentNode != null;

        /// <summary>
        /// 현재 세션이 종료되었는지 여부입니다.
        /// </summary>
        public bool IsCompleted => _isCompleted;

        /// <summary>
        /// 현재 표시 중인 대사 노드입니다.
        /// </summary>
        public DialogueNodeData CurrentNode => _currentNode;

        /// <summary>
        /// 현재 노드가 선택지를 가지고 있는지 여부입니다.
        /// </summary>
        public bool HasCurrentOptions => _currentNode?.options != null && _currentNode.options.Count > 0;

        /// <summary>
        /// 세션을 초기화하고 지정한 DialogueData로 시작합니다.
        /// </summary>
        /// <param name="data">시작할 대화 데이터입니다.</param>
        /// <param name="startNodeGuid">시작 노드 GUID입니다. 비어 있으면 첫 번째 노드를 사용합니다.</param>
        public void Start(DialogueData data, string startNodeGuid = null)
        {
            Clear();
            if (data?.nodes == null || data.nodes.Count == 0)
            {
                _isCompleted = true;
                return;
            }

            foreach (DialogueNodeData node in data.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.guid))
                {
                    continue;
                }

                _nodes[node.guid] = node;
            }

            if (_nodes.Count == 0)
            {
                _isCompleted = true;
                return;
            }

            string resolvedStartGuid = string.IsNullOrWhiteSpace(startNodeGuid) ? data.nodes[0]?.guid : startNodeGuid;
            if (!TryMoveToNodeInternal(resolvedStartGuid))
            {
                DialogueNodeData firstNode = data.nodes[0];
                if (firstNode == null || !TryMoveToNodeInternal(firstNode.guid))
                {
                    _isCompleted = true;
                }
            }
        }

        /// <summary>
        /// 현재 노드의 기본 nextNodeGuid 기준으로 다음 노드로 이동합니다.
        /// </summary>
        /// <returns>다음 노드로 이동했으면 true입니다.</returns>
        public bool TryMoveNext()
        {
            if (_currentNode == null)
            {
                _isCompleted = true;
                return false;
            }

            if (HasCurrentOptions)
            {
                return false;
            }

            return TryMoveToNodeInternal(_currentNode.nextNodeGuid);
        }

        /// <summary>
        /// 현재 노드의 선택지를 선택해 다음 노드로 이동합니다.
        /// </summary>
        /// <param name="optionIndex">선택한 옵션 인덱스입니다.</param>
        /// <returns>다음 노드로 이동했으면 true입니다.</returns>
        public bool TrySelectOption(int optionIndex)
        {
            if (_currentNode?.options == null || optionIndex < 0 || optionIndex >= _currentNode.options.Count)
            {
                return false;
            }

            DialogueOption option = _currentNode.options[optionIndex];
            if (option == null)
            {
                return false;
            }

            return TryMoveToNodeInternal(option.nextNodeGuid);
        }

        /// <summary>
        /// 현재 세션 상태를 완전히 초기화합니다.
        /// </summary>
        public void Clear()
        {
            _nodes.Clear();
            _currentNode = null;
            _isCompleted = false;
        }

        /// <summary>
        /// 지정한 GUID의 노드로 이동합니다.
        /// GUID가 비어 있거나 찾을 수 없으면 세션을 종료 상태로 전환합니다.
        /// </summary>
        /// <param name="guid">이동할 노드 GUID입니다.</param>
        /// <returns>정상적으로 노드를 찾았으면 true입니다.</returns>
        private bool TryMoveToNodeInternal(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                _isCompleted = true;
                return false;
            }

            if (_nodes.TryGetValue(guid, out DialogueNodeData node) && node != null)
            {
                _currentNode = node;
                _isCompleted = false;
                return true;
            }

            GcLogger.LogError($"interaction dialogue node 를 찾지 못했습니다. guid: {guid}");
            _isCompleted = true;
            return false;
        }
    }
}
