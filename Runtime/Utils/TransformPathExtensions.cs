using System.Text;
using UnityEngine;

namespace GGemCo2DCore
{
    public static class TransformPathExtensions
    {
        /// <summary>씬 루트부터의 경로("Root/Child/Target")</summary>
        public static string GetHierarchyPath(this Transform t)
        {
            if (t == null) return string.Empty;
            var sb = new StringBuilder(t.name);
            var cur = t.parent;
            while (cur != null)
            {
                sb.Insert(0, '/').Insert(0, cur.name);
                cur = cur.parent;
            }
            return sb.ToString();
        }

        public static string GetHierarchyPath(this GameObject go) => go.transform.GetHierarchyPath();
    }
}