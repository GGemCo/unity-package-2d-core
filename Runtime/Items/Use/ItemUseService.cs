using System.Collections.Generic;
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

            ResolveItemUseReceivers(
                sceneGame.player,
                targetObject,
                out IItemUseSkillReceiver skillReceiver,
                out IItemUseSkillPassiveReceiver skillPassiveReceiver,
                out IItemUseMpReceiver mpReceiver);

            var ctx = new ItemUseContext(sceneGame, player, playerData,
                inventory: null, slotIndex: -1, itemUid: itemUid, consumeCount: consumeCount,
                skillReceiver: skillReceiver,
                targetObject: targetObject,
                skillPassiveReceiver: skillPassiveReceiver,
                mpReceiver: mpReceiver);

            // 사용할 수 있는지 먼저 체크
            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");
                var can = action.CanExecute(ctx);
                if (can == null || !can.IsSuccess())
                    return can ?? ResultCommon.Fail("ItemUse_CannotExecute");
            }

            // 실제 사용하기
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
        /// <summary>
        /// 인벤토리에 추가하지 않고 아이템 사용 효과를 즉시 실행합니다.
        /// 상점의 즉시 사용 구매처럼 아이템 획득과 소비를 분리하지 않는 흐름에서 사용합니다.
        /// </summary>
        /// <param name="sceneGame">현재 게임 씬입니다.</param>
        /// <param name="itemUid">즉시 사용할 아이템 UID입니다.</param>
        /// <param name="cooldownSeconds">아이템 사용 후 적용할 쿨타임입니다.</param>
        /// <param name="targetObject">효과 적용 대상입니다. null이면 플레이어를 대상으로 사용합니다.</param>
        /// <returns>아이템 사용 효과 실행 결과입니다.</returns>
        public static ResultCommon TryUseItemDirect(
            SceneGame sceneGame,
            int itemUid,
            out float cooldownSeconds,
            GameObject targetObject = null)
        {
            cooldownSeconds = 0;
            if (sceneGame == null || itemUid <= 0)
            {
                return ResultCommon.Fail("ItemUse_InvalidContext");
            }

            if (!TryBuildContext(
                    sceneGame,
                    inventory: null,
                    slotIndex: -1,
                    itemUid: itemUid,
                    targetObject: targetObject,
                    out ItemUseContext context,
                    out StruckTableItemUse useGroup,
                    out var actions,
                    out cooldownSeconds,
                    out ResultCommon failResult))
            {
                return failResult;
            }

            return ExecuteActionsWithoutInventoryConsume(context, actions);
        }

        public static ResultCommon TryUseInventoryItem(SceneGame sceneGame, InventoryData inventory, int slotIndex,
            out float cooldownSeconds)
        {
            cooldownSeconds = 0;
            if (sceneGame == null || inventory == null)
                return ResultCommon.Fail("ItemUse_InvalidContext");

            if (TableLoaderManager.Instance == null)
                return ResultCommon.Fail("ItemUse_NoTableLoader");

            if (!inventory.ItemCounts.TryGetValue(slotIndex, out var icon))
                return ResultCommon.Fail("Item_NoUsableCount");

            int itemUid = icon.Uid;
            int itemCount = icon.Count;
            if (itemUid <= 0 || itemCount <= 0)
                return ResultCommon.Fail("Item_NoUsableCount");

            if (!TryBuildContext(
                    sceneGame,
                    inventory,
                    slotIndex,
                    itemUid,
                    targetObject: null,
                    out ItemUseContext ctx,
                    out StruckTableItemUse useGroup,
                    out var actions,
                    out cooldownSeconds,
                    out ResultCommon failResult))
            {
                return failResult;
            }

            // consume
            int consumeCount = Mathf.Max(1, useGroup.ConsumeCount);
            if (itemCount < consumeCount)
                return ResultCommon.Fail("Item_NoUsableCount");

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

        /// <summary>
        /// 아이템 사용에 필요한 테이블, 대상, 액션 목록, 쿨타임 정보를 구성합니다.
        /// </summary>
        /// <param name="sceneGame">현재 게임 씬입니다.</param>
        /// <param name="inventory">인벤토리 소모가 필요한 경우 사용할 저장 데이터입니다.</param>
        /// <param name="slotIndex">인벤토리 슬롯 인덱스입니다. 직접 사용이면 -1입니다.</param>
        /// <param name="itemUid">사용할 아이템 UID입니다.</param>
        /// <param name="targetObject">효과 적용 대상입니다.</param>
        /// <param name="context">구성된 아이템 사용 컨텍스트입니다.</param>
        /// <param name="useGroup">item_use 테이블의 사용 그룹입니다.</param>
        /// <param name="actions">실행할 item_use_action 목록입니다.</param>
        /// <param name="cooldownSeconds">사용 후 적용할 쿨타임입니다.</param>
        /// <param name="failResult">구성 실패 시 반환할 결과입니다.</param>
        /// <returns>컨텍스트 구성이 완료되면 true입니다.</returns>
        private static bool TryBuildContext(
            SceneGame sceneGame,
            InventoryData inventory,
            int slotIndex,
            int itemUid,
            GameObject targetObject,
            out ItemUseContext context,
            out StruckTableItemUse useGroup,
            out List<StruckTableItemUseAction> actions,
            out float cooldownSeconds,
            out ResultCommon failResult)
        {
            context = null;
            useGroup = null;
            actions = null;
            cooldownSeconds = 0;
            failResult = null;

            if (TableLoaderManager.Instance == null)
            {
                failResult = ResultCommon.Fail("ItemUse_NoTableLoader");
                return false;
            }

            var tableItem = TableLoaderManager.Instance.TableItem;
            var tableItemUse = TableLoaderManager.Instance.TableItemUse;
            var tableItemUseAction = TableLoaderManager.Instance.TableItemUseAction;

            if (tableItem == null || tableItemUse == null || tableItemUseAction == null)
            {
                failResult = ResultCommon.Fail("ItemUse_NoTables");
                return false;
            }

            if (!tableItemUse.TryGetByItemUid(itemUid, out useGroup) || useGroup == null)
            {
                failResult = ResultCommon.Fail("Item_NotUsable");
                return false;
            }

            int consumeCount = Mathf.Max(1, useGroup.ConsumeCount);
            var itemRow = tableItem.GetDataByUid(itemUid);
            float cd = useGroup.CooldownOverride > 0 ? useGroup.CooldownOverride : (itemRow != null ? itemRow.CoolTime : 0);
            cooldownSeconds = Mathf.Max(0, cd);

            actions = tableItemUseAction.GetActions(useGroup.Uid);
            if (actions == null || actions.Count == 0)
            {
                failResult = ResultCommon.Fail("ItemUse_NoActions");
                return false;
            }

            var player = sceneGame.player != null ? sceneGame.player.GetComponent<Player>() : null;
            var playerData = sceneGame.saveDataManager != null ? sceneGame.saveDataManager.Player : null;
            if (playerData == null)
            {
                failResult = ResultCommon.Fail("ItemUse_NoPlayerData");
                return false;
            }

            ResolveItemUseReceivers(
                sceneGame.player,
                targetObject,
                out IItemUseSkillReceiver skillReceiver,
                out IItemUseSkillPassiveReceiver skillPassiveReceiver,
                out IItemUseMpReceiver mpReceiver);

            context = new ItemUseContext(
                sceneGame,
                player,
                playerData,
                inventory,
                slotIndex,
                itemUid,
                consumeCount,
                skillReceiver,
                targetObject: targetObject,
                skillPassiveReceiver: skillPassiveReceiver,
                mpReceiver: mpReceiver);

            return true;
        }

        /// <summary>
        /// 인벤토리 소모 없이 아이템 사용 액션을 검증하고 실행합니다.
        /// </summary>
        /// <param name="context">아이템 사용 컨텍스트입니다.</param>
        /// <param name="actions">실행할 액션 목록입니다.</param>
        /// <returns>액션 실행 결과입니다.</returns>
        private static ResultCommon ExecuteActionsWithoutInventoryConsume(
            ItemUseContext context,
            System.Collections.Generic.List<StruckTableItemUseAction> actions)
        {
            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");

                var can = action.CanExecute(context);
                if (can == null || !can.IsSuccess())
                {
                    return can ?? ResultCommon.Fail("ItemUse_CannotExecute");
                }
            }

            foreach (var row in actions)
            {
                var action = ItemUseActionFactory.Create(row);
                if (action == null) return ResultCommon.Fail("ItemUse_InvalidAction");

                var exec = action.Execute(context);
                if (exec == null || !exec.IsSuccess())
                {
                    return exec ?? ResultCommon.Fail("ItemUse_Execute_Fail");
                }
            }

            return ResultCommon.Success();
        }

        /// <summary>
        /// 아이템 사용 액션에서 사용할 선택형 수신자들을 찾습니다.
        /// </summary>
        /// <param name="playerObject">기본 수신자를 찾을 플레이어 오브젝트입니다.</param>
        /// <param name="targetObject">효과 적용 대상 오브젝트입니다. null이면 플레이어만 검사합니다.</param>
        /// <param name="skillReceiver">액티브 스킬 지급 수신자입니다.</param>
        /// <param name="skillPassiveReceiver">패시브 스킬 지급 수신자입니다.</param>
        /// <param name="mpReceiver">MP 회복 규칙 수신자입니다.</param>
        private static void ResolveItemUseReceivers(
            GameObject playerObject,
            GameObject targetObject,
            out IItemUseSkillReceiver skillReceiver,
            out IItemUseSkillPassiveReceiver skillPassiveReceiver,
            out IItemUseMpReceiver mpReceiver)
        {
            skillReceiver = null;
            skillPassiveReceiver = null;
            mpReceiver = null;

            // 대부분의 아이템 사용 규칙은 플레이어 오브젝트에 붙은 컴포넌트가 처리합니다.
            CollectItemUseReceivers(playerObject, ref skillReceiver, ref skillPassiveReceiver, ref mpReceiver);

            // 대상 오브젝트가 별도로 지정된 경우 대상 전용 수신자도 검사합니다.
            // 같은 오브젝트를 두 번 검사하지 않도록 참조를 비교합니다.
            if (targetObject != null && targetObject != playerObject)
            {
                CollectItemUseReceivers(targetObject, ref skillReceiver, ref skillPassiveReceiver, ref mpReceiver);
            }
        }

        /// <summary>
        /// 지정한 오브젝트에서 아이템 사용용 인터페이스 구현체를 수집합니다.
        /// </summary>
        /// <param name="sourceObject">검사할 오브젝트입니다.</param>
        /// <param name="skillReceiver">액티브 스킬 지급 수신자입니다.</param>
        /// <param name="skillPassiveReceiver">패시브 스킬 지급 수신자입니다.</param>
        /// <param name="mpReceiver">MP 회복 규칙 수신자입니다.</param>
        private static void CollectItemUseReceivers(
            GameObject sourceObject,
            ref IItemUseSkillReceiver skillReceiver,
            ref IItemUseSkillPassiveReceiver skillPassiveReceiver,
            ref IItemUseMpReceiver mpReceiver)
        {
            if (sourceObject == null)
            {
                return;
            }

            // Unity 버전/환경별 인터페이스 GetComponent 동작 차이를 피하기 위해 MonoBehaviour 목록에서 직접 캐스팅합니다.
            var behaviours = sourceObject.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                skillReceiver ??= behaviour as IItemUseSkillReceiver;
                skillPassiveReceiver ??= behaviour as IItemUseSkillPassiveReceiver;
                mpReceiver ??= behaviour as IItemUseMpReceiver;

                if (skillReceiver != null && skillPassiveReceiver != null && mpReceiver != null)
                {
                    return;
                }
            }
        }
    }
}
