using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo2DCore
{
    public sealed class UIWindowOptionsExtensionRegistry : MonoBehaviour
    {
        [SerializeField] private Transform parentPanel;
        private readonly List<IOptionsSectionProvider> _providers = new();

        public void Register(IOptionsSectionProvider p) => _providers.Add(p);
        public void BuildAll()
        {
            UIWindowOption uiWindowOption = GetComponent<UIWindowOption>();
            foreach (var p in _providers.OrderBy(x => x.Order))
            {
                p.BuildSection(parentPanel, uiWindowOption);
            }
        }
    }
}
