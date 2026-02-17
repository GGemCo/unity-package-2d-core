using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용 서비스
    /// - UI/단축키/퀵슬롯 등 다양한 진입점을 "단일" 로직으로 통합하기 위한 런타임 서비스
    /// - 아이템 소모/쿨타임/효과 실행 순서를 관리합니다.
    /// </summary>
    public static class ItemUseService
    {
#if UNITY_EDITOR        
        /// <summary>
        /// 인벤토리 소모 없이, 특정 ItemUid의 사용 효과만 테스트/실행합니다.
        /// - 에디터 툴(UseItem) 등에서 사용
        /// - Consume은 수행하지 않습니다.
        /// </summary>
        public static ResultCommon TryUseItem(SceneGame sceneGame, int itemUid, out float cooldownSeconds, GameObject targetObject = null)
        {
            cooldownSeconds = 0;
            if (sceneGame == null || itemUid <= 0)
                return ResultCommon.Fail("ItemUse_InvalidContext");

            if (TableLoaderManager.Instance == null)
                return ResultCommon.Fail("ItemUse_NoTableLoader");

            var tableItem = TableLoaderManager.Instance.TableItem;
            var tableItemUse = TableLoaderManager.Instance.TableItemUse;
            var tableItemUseAction = TableLoaderManager.Instance.TableItemUseAction;

            if (tableItem == null || tableItemUse == null || tableItemUseAction == null)
                return ResultCommon.Fail("ItemUse_NoTables");

            if (!tableItemUse.TryGetByItemUid(itemUid, out var useGroup) || useGroup == null)
                return ResultCommon.Fail("Item_NotUsable");

            int consumeCount = Mathf.Max(1, useGroup.ConsumeCount);

            // cooldown (테스트 용이므로 반환만)
            var itemRow = tableItem.GetDataByUid(itemUid);
            float cd = useGroup.CooldownOverride > 0 ? useGroup.CooldownOverride : (itemRow != null ? itemRow.CoolTime : 0);
            cooldownSeconds = Mathf.Max(0, cd);

            var actions = tableItemUseAction.GetActions(useGroup.Uid);
            if (actions == null || actions.Count == 0)
                return ResultCommon.Fail("ItemUse_NoActions");

            var player = sceneGame.player != null ? sceneGame.player.GetComponent<Player>() : null;
            var playerData = sceneGame.saveDataManager != null ? sceneGame.saveDataManager.Player : null;
            if (playerData == null)
                return ResultCommon.Fail("ItemUse_NoPlayerData");

            IItemUseSkillReceiver skillReceiver = null;
            if (sceneGame.player != null)
            {
                var behaviours = sceneGame.player.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IItemUseSkillReceiver receiver)
                    {
                        skillReceiver = receiver;
                        break;
                    }
                }
            }

            var ctx = new ItemUseContext(sceneGame, player, playerData,
                inventory: null, slotIndex: -1, itemUid: itemUid, consumeCount: consumeCount,
                skillReceiver: skillReceiver, targetObject: targetObject);

            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");
                var can = action.CanExecute(ctx);
                if (can == null || !can.IsSuccess())
                    return can ?? ResultCommon.Fail("ItemUse_CannotExecute");
            }

            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");
                var exec = action.Execute(ctx);
                if (exec == null || !exec.IsSuccess())
                    return exec ?? ResultCommon.Fail("ItemUse_Execute_Fail");
            }

            return ResultCommon.SuccessWithIcons(null);
        }
#endif
        public static ResultCommon TryUseInventoryItem(SceneGame sceneGame, InventoryData inventory, int slotIndex,
            out float cooldownSeconds)
        {
            cooldownSeconds = 0;
            if (sceneGame == null || inventory == null)
                return ResultCommon.Fail("ItemUse_InvalidContext");

            if (TableLoaderManager.Instance == null)
                return ResultCommon.Fail("ItemUse_NoTableLoader");

            var tableItem = TableLoaderManager.Instance.TableItem;
            var tableItemUse = TableLoaderManager.Instance.TableItemUse;
            var tableItemUseAction = TableLoaderManager.Instance.TableItemUseAction;

            if (tableItem == null || tableItemUse == null || tableItemUseAction == null)
                return ResultCommon.Fail("ItemUse_NoTables");

            if (!inventory.ItemCounts.TryGetValue(slotIndex, out var icon))
                return ResultCommon.Fail("Item_NoUsableCount");

            int itemUid = icon.Uid;
            int itemCount = icon.Count;
            if (itemUid <= 0 || itemCount <= 0)
                return ResultCommon.Fail("Item_NoUsableCount");

            if (!tableItemUse.TryGetByItemUid(itemUid, out var useGroup) || useGroup == null)
                return ResultCommon.Fail("Item_NotUsable");

            // consume
            int consumeCount = Mathf.Max(1, useGroup.ConsumeCount);
            if (itemCount < consumeCount)
                return ResultCommon.Fail("Item_NoUsableCount");

            // cooldown
            var itemRow = tableItem.GetDataByUid(itemUid);
            float cd = useGroup.CooldownOverride > 0 ? useGroup.CooldownOverride : (itemRow != null ? itemRow.CoolTime : 0);
            cooldownSeconds = Mathf.Max(0, cd);

            // action list
            var actions = tableItemUseAction.GetActions(useGroup.Uid);
            if (actions == null || actions.Count == 0)
                return ResultCommon.Fail("ItemUse_NoActions");

            // context
            var player = sceneGame.player != null ? sceneGame.player.GetComponent<Player>() : null;
            var playerData = sceneGame.saveDataManager != null ? sceneGame.saveDataManager.Player : null;
            if (playerData == null)
                return ResultCommon.Fail("ItemUse_NoPlayerData");

            // SkillReceiver는 Core에서 강제하지 않고, 외부 패키지에서 주입하는 형태를 권장
            // 기본 구현: 플레이어 오브젝트에 구현체가 붙어있다면 자동 연결
            IItemUseSkillReceiver skillReceiver = null;
            if (sceneGame.player != null)
            {
                // Unity의 GetComponent<T>()는 인터페이스를 직접 지원하지 않으므로,
                // MonoBehaviour 중에서 인터페이스 구현체를 찾아 연결합니다.
                var behaviours = sceneGame.player.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IItemUseSkillReceiver receiver)
                    {
                        skillReceiver = receiver;
                        break;
                    }
                }
            }

            var ctx = new ItemUseContext(sceneGame, player, playerData, inventory, slotIndex, itemUid, consumeCount, skillReceiver);

            // 1) CanExecute: 전체 사전 검증
            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");

                var can = action.CanExecute(ctx);
                if (can == null || !can.IsSuccess())
                {
                    return can ?? ResultCommon.Fail("ItemUse_CannotExecute");
                }
            }

            // 2) Execute
            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");
                var exec = action.Execute(ctx);
                if (exec == null || !exec.IsSuccess())
                {
                    // AllOrNothing 정책이라면 여기서 종료(현재 단계 이전 적용분 rollback은 하지 않음)
                    // - 실 운영에서는 "Execute가 실패하지 않도록" CanExecute에서 최대한 검증하는 것을 권장
                    return exec ?? ResultCommon.Fail("ItemUse_Execute_Fail");
                }
            }

            // 3) Consume
            var minus = inventory.MinusItem(slotIndex, itemUid, consumeCount);
            if (minus == null || !minus.IsSuccess())
                return minus ?? ResultCommon.Fail("ItemUse_Consume_Fail");

            return minus;
        }
    }
}
