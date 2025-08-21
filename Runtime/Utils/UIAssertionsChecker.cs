using UnityEngine;

namespace GGemCo2DCore
{
    public static class UIAssertionsChecker
    {
        public static void Require(Object context, Object field, string fieldName)
        {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(field, 
                $"{context.name}: 필수 필드 '{fieldName}'가 할당되지 않았습니다. 인스펙터에서 값을 지정하세요.");
#endif
        }
    }
}