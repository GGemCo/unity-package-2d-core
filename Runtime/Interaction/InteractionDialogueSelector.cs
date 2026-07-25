using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// interaction row 에서 실제로 재생할 dialogue 를 선택합니다.
    /// </summary>
    public static class InteractionDialogueSelector
    {
        private static readonly char[] ListSeparators = { ',', ';', '\n', '\r' };

        /// <summary>
        /// interaction 데이터에서 이번 세션에 사용할 dialogue 를 선택합니다.
        /// </summary>
        /// <param name="interactionData">현재 interaction 데이터입니다.</param>
        /// <returns>선택된 dialogue 정보입니다.</returns>
        public static InteractionDialogueSelectionResult Select(StruckTableInteraction interactionData)
        {
            return Select(interactionData, preferFirstDialogue: false);
        }

        /// <summary>
        /// 첫 대화 우선 여부를 반영하여 이번 인터랙션 세션에 사용할 dialogue를 선택합니다.
        /// 첫 대화가 유효하지 않으면 기존 일반 또는 랜덤 dialogue 선택 규칙을 사용합니다.
        /// </summary>
        /// <param name="interactionData">현재 interaction 데이터입니다.</param>
        /// <param name="preferFirstDialogue">첫 대화를 우선 선택할지 여부입니다.</param>
        /// <returns>선택된 dialogue 정보입니다.</returns>
        public static InteractionDialogueSelectionResult Select(
            StruckTableInteraction interactionData,
            bool preferFirstDialogue)
        {
            if (interactionData == null)
            {
                return InteractionDialogueSelectionResult.None;
            }

            if (preferFirstDialogue && interactionData.FirstDialogueUid > 0)
            {
                return new InteractionDialogueSelectionResult(
                    interactionData.FirstDialogueUid,
                    NormalizeValue(interactionData.FirstDialogueStartNodeGuid));
            }

            List<InteractionDialogueSelectionResult> candidates = BuildCandidates(interactionData);
            if (candidates.Count == 0)
            {
                return InteractionDialogueSelectionResult.None;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[randomIndex];
        }

        /// <summary>
        /// interaction 데이터에서 후보 dialogue 목록을 구성합니다.
        /// </summary>
        /// <param name="interactionData">현재 interaction 데이터입니다.</param>
        /// <returns>재생 가능한 dialogue 후보 목록입니다.</returns>
        private static List<InteractionDialogueSelectionResult> BuildCandidates(StruckTableInteraction interactionData)
        {
            List<InteractionDialogueSelectionResult> candidates = new List<InteractionDialogueSelectionResult>();
            List<int> randomDialogueUids = ParseIntList(interactionData.DialogueUidRandomList);
            if (randomDialogueUids.Count <= 0)
            {
                TryAddCandidate(candidates, interactionData.DialogueUid, interactionData.DialogueStartNodeGuid);
                return candidates;
            }

            List<string> startNodeGuids = ParseStringList(interactionData.DialogueStartNodeGuidRandomList);
            for (int i = 0; i < randomDialogueUids.Count; i++)
            {
                string startNodeGuid = i < startNodeGuids.Count
                    ? startNodeGuids[i]
                    : interactionData.DialogueStartNodeGuid;

                TryAddCandidate(candidates, randomDialogueUids[i], startNodeGuid);
            }

            return candidates;
        }

        /// <summary>
        /// dialogue UID 가 유효할 때만 후보 목록에 추가합니다.
        /// </summary>
        /// <param name="candidates">후보 목록입니다.</param>
        /// <param name="dialogueUid">재생할 dialogue UID 입니다.</param>
        /// <param name="startNodeGuid">시작 노드 GUID 입니다.</param>
        private static void TryAddCandidate(
            List<InteractionDialogueSelectionResult> candidates,
            int dialogueUid,
            string startNodeGuid)
        {
            if (candidates == null || dialogueUid <= 0)
            {
                return;
            }

            candidates.Add(new InteractionDialogueSelectionResult(dialogueUid, NormalizeValue(startNodeGuid)));
        }

        /// <summary>
        /// 구분자 기반 문자열 목록을 int 목록으로 변환합니다.
        /// </summary>
        /// <param name="raw">원본 문자열입니다.</param>
        /// <returns>파싱된 int 목록입니다.</returns>
        private static List<int> ParseIntList(string raw)
        {
            List<int> values = new List<int>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return values;
            }

            string[] parts = raw.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int parsed = MathHelper.ParseInt(part.Trim());
                if (parsed > 0)
                {
                    values.Add(parsed);
                }
            }

            return values;
        }

        /// <summary>
        /// 구분자 기반 문자열 목록을 문자열 리스트로 변환합니다.
        /// </summary>
        /// <param name="raw">원본 문자열입니다.</param>
        /// <returns>파싱된 문자열 목록입니다.</returns>
        private static List<string> ParseStringList(string raw)
        {
            List<string> values = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return values;
            }

            string[] parts = raw.Split(ListSeparators, StringSplitOptions.None);
            foreach (string part in parts)
            {
                values.Add(NormalizeValue(part));
            }

            return values;
        }

        /// <summary>
        /// 테이블 문자열을 런타임에서 사용할 값으로 정규화합니다.
        /// </summary>
        /// <param name="value">원본 문자열입니다.</param>
        /// <returns>정규화된 문자열입니다.</returns>
        private static string NormalizeValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Trim();
            if (string.Equals(normalized, "None", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return normalized;
        }
    }
}
