using System;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Core.Extensions;

namespace GGemCo2DCore
{
    [Serializable]
    public class RandomValueSource : ISource
    {
        [SerializeField] private string selector = "random";
        [SerializeField] private int minInclusive = 0;
        [SerializeField] private int maxExclusive = 3;

        public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
        {
            if (!string.Equals(selectorInfo.SelectorText, selector, StringComparison.Ordinal))
                return false;

            selectorInfo.Result = UnityEngine.Random.Range(minInclusive, maxExclusive);
            return true;
        }
    }
}