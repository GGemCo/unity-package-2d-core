using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 검증 메시지 한 건을 나타냅니다.
    /// </summary>
    internal readonly struct WorldMapValidationMessage
    {
        /// <summary>메시지 심각도입니다.</summary>
        public WorldMapValidationSeverity Severity { get; }

        /// <summary>사용자에게 표시할 메시지입니다.</summary>
        public string Message { get; }

        /// <summary>관련 노드 또는 연결선 ID입니다.</summary>
        public string TargetId { get; }

        /// <summary>
        /// 검증 메시지를 초기화합니다.
        /// </summary>
        /// <param name="severity">메시지 심각도입니다.</param>
        /// <param name="message">표시할 메시지입니다.</param>
        /// <param name="targetId">관련 대상 ID입니다.</param>
        public WorldMapValidationMessage(WorldMapValidationSeverity severity, string message, string targetId = null)
        {
            Severity = severity;
            Message = message;
            TargetId = targetId;
        }

        /// <summary>
        /// Unity IMGUI HelpBox에서 사용할 메시지 타입으로 변환합니다.
        /// </summary>
        /// <returns>Unity 메시지 타입입니다.</returns>
        public MessageType ToMessageType()
        {
            switch (Severity)
            {
                case WorldMapValidationSeverity.Error:
                    return MessageType.Error;
                case WorldMapValidationSeverity.Warning:
                    return MessageType.Warning;
                default:
                    return MessageType.Info;
            }
        }
    }

    /// <summary>
    /// 월드맵 검증 결과 전체를 보관합니다.
    /// </summary>
    internal sealed class WorldMapValidationReport
    {
        private readonly List<WorldMapValidationMessage> _messages = new List<WorldMapValidationMessage>();

        /// <summary>검증 메시지 목록입니다.</summary>
        public IReadOnlyList<WorldMapValidationMessage> Messages => _messages;

        /// <summary>오류 메시지가 하나 이상 있는지 여부입니다.</summary>
        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].Severity == WorldMapValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 검증 메시지를 결과 목록에 추가합니다.
        /// </summary>
        /// <param name="severity">메시지 심각도입니다.</param>
        /// <param name="message">표시할 메시지입니다.</param>
        /// <param name="targetId">관련 대상 ID입니다.</param>
        public void Add(WorldMapValidationSeverity severity, string message, string targetId = null)
        {
            _messages.Add(new WorldMapValidationMessage(severity, message, targetId));
        }
    }
}
