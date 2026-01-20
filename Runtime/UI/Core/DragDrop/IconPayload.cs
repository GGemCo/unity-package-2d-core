using System;

namespace GGemCo2DCore
{
    [Serializable]
    public readonly struct IconPayload : IEquatable<IconPayload>
    {
        public readonly int Uid;
        public readonly int Count;
        public readonly long InstanceId;

        public IconPayload(int uid, int count, long instanceId)
        {
            Uid = uid;
            Count = count;
            InstanceId = instanceId;
        }

        public bool Equals(IconPayload other)
        {
            return Uid == other.Uid
                   && Count == other.Count
                   && InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj) => obj is IconPayload other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Uid;
                hash = (hash * 397) ^ Count;
                hash = (hash * 397) ^ InstanceId.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"uid={Uid} count={Count} inst={InstanceId}";
        }
    }
}
