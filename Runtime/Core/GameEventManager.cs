using System;

namespace GGemCo.Scripts
{
    public static class GameEventManager
    {
        public static event Action<int, int> OnMonsterKilled = null;
        public static event Action<int, int> OnItemCollected = null;
        public static event Action<int> OnDialogStart = null;
        public static event Action<int> OnDialogEnd = null;

        public static void MonsterKilled(int mapUid, int monsterUid)
        {
            OnMonsterKilled?.Invoke(mapUid, monsterUid);
        }

        public static void ItemCollected(int itemUid, int count)
        {
            OnItemCollected?.Invoke(itemUid, count);
        }
        public static void DialogStart(int npcUid)
        {
            OnDialogStart?.Invoke(npcUid);
        }
        public static void DialogEnd(int npcUid)
        {
            OnDialogEnd?.Invoke(npcUid);
        }

        public static void OnDestroy()
        {
            OnMonsterKilled = null;
            OnItemCollected = null;
            OnDialogStart = null;
            OnDialogEnd = null;
        }
    }
}