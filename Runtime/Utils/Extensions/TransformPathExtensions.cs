using System.Text;
using UnityEngine;

namespace GGemCo2DCore
{
    public static class TransformPathExtensions
    {
        /// <summary>씬 루트부터의 경로("Root/Child/Target")</summary>
        private static string GetHierarchyPath(this Transform transform)
        {
            if (transform == null) return string.Empty;

            var sb = new StringBuilder(128);
            Build(transform, sb);
            return sb.ToString();

            static void Build(Transform t, StringBuilder sb)
            {
                if (t.parent != null)
                {
                    Build(t.parent, sb);
                    sb.Append('/');
                }
                sb.Append(t.name);
            }
        }

        public static string GetHierarchyPath(this GameObject go) => go.transform.GetHierarchyPath();
    }
}