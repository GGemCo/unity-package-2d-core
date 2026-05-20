using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에서 일반 대화창을 표시할 때 사용하는 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class DialogueWindowData
    {
        [Header("Dialogue")]
        [Tooltip("dialogue 테이블 UID입니다.")]
        public int dialogueUid;

        [Tooltip("대화 종료 이벤트에 전달할 NPC UID입니다. 0이면 NPC 없이 대화만 재생합니다.")]
        public int npcUid;

        [Header("Timeline")]
        [Tooltip("true이면 대화 종료 이벤트를 받을 때까지 컷신 타임라인 진행을 대기합니다.")]
        public bool waitUntilEnd = true;

        [Tooltip("대화 로드 실패 시에도 타임라인 대기를 자동 해제합니다.")]
        public bool releaseWaitOnLoadFailed = true;

        [Header("Window")]
        [Tooltip("true이면 대화 시작 전에 Dialogue를 제외한 다른 UIWindow를 닫습니다.")]
        public bool closeOtherWindows;
    }
}
