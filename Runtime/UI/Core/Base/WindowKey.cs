using System;

namespace GGemCo2DCore
{
    [Serializable]
    public class WindowKey : IEquatable<WindowKey>
    {
        public int uid;
        public UIWindow uiWindow;

        public bool Equals(WindowKey other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            return uid == other.uid && uiWindow == other.uiWindow;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WindowKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = uid;
                hash = (hash * 397) ^ (uiWindow != null ? uiWindow.GetHashCode() : 0);
                return hash;
            }
        }
    }
}