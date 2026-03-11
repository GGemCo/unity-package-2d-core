using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Play Mode 테스트용으로 생성한 더미 캐릭터를 식별하기 위한 마커입니다.
    /// 어떤 툴이 생성했는지와 원본 캐릭터 정보를 함께 보관합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterTestDummyMarker : MonoBehaviour
    {
        [SerializeField] private string ownerToolKey;
        [SerializeField] private string dummyName;
        [SerializeField] private int sourceInstanceId;
        [SerializeField] private int sourceUid;
        [SerializeField] private CharacterConstants.Type sourceType;

        public string OwnerToolKey => ownerToolKey;
        public string DummyName => dummyName;
        public int SourceInstanceId => sourceInstanceId;
        public int SourceUid => sourceUid;
        public CharacterConstants.Type SourceType => sourceType;

        public void Bind(string toolKey, string targetDummyName, CharacterBase sourceCharacter)
        {
            ownerToolKey = toolKey ?? string.Empty;
            dummyName = targetDummyName ?? string.Empty;

            if (sourceCharacter == null)
            {
                sourceInstanceId = 0;
                sourceUid = 0;
                sourceType = CharacterConstants.Type.None;
                return;
            }

            sourceInstanceId = sourceCharacter.GetInstanceID();
            sourceUid = sourceCharacter.uid;
            sourceType = sourceCharacter.type;
        }

        public bool Matches(string toolKey, string targetDummyName)
        {
            return ownerToolKey == (toolKey ?? string.Empty) &&
                   dummyName == (targetDummyName ?? string.Empty);
        }
    }
}
