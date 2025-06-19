using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class DialogueBalloonPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly List<GameObject> pool = new();

        public DialogueBalloonPool(Transform parent = null)
        {
            prefab =
                AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables
                    .KeyPrefabDialogueBalloon);
            this.parent = parent;
        }

        public GameObject Get()
        {
            GameObject balloon = pool.FirstOrDefault(b => !b.activeSelf);
            if (balloon == null)
            {
                balloon = Object.Instantiate(prefab, parent);
                pool.Add(balloon);
            }
            balloon.SetActive(true);
            return balloon;
        }

        public void Return(GameObject balloon)
        {
            balloon.SetActive(false);
        }

        public void DestroyAll()
        {
            foreach (var balloon in pool)
            {
                Object.Destroy(balloon);
            }
            pool.Clear();
        }

    }
}