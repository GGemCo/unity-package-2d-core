#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Trap 프리팹을 기반으로 오브젝트를 생성하는 팩토리
    /// - 프리팹 템플릿이 있으면 우선 사용
    /// - 프리팹이 없으면(로드 실패 시) null 반환
    /// - 생성 후 특정 Trap 타입으로 확장 (Fixed/Timer/Infinity)
    /// </summary>
    internal static class TrapFactory
    {
        // 기본 Trap 템플릿 경로
        private const string TemplateDefault =
            "Packages/com.ggemco.2d.core/Editor/GGemCoCreator/Templates/Prefabs/Trap/TrapDefault.prefab";

        /// <summary>
        /// Trap 기본 프리팹을 인스턴스화하고 Prefab을 해제(Unpack)한 뒤 반환
        /// </summary>
        private static GameObject CreateDefault(MenuCommand cmd)
        {
            // 1) 템플릿 프리팹 인스턴스화 시도
            GameObject inst = TryInstantiate(TemplateDefault, cmd);
            if (!inst) return null;

            // 2) Prefab 해제 → 씬 안에서 자유롭게 수정 가능하도록 함
            PrefabUtility.UnpackPrefabInstance(
                inst,
                PrefabUnpackMode.Completely,
                InteractionMode.UserAction
            );

            return inst;
        }

        /// <summary>
        /// 고정형(Animator 기반) Trap 생성
        /// </summary>
        public static GameObject CreateFixed(MenuCommand cmd, bool useTrigger = false)
        {
            var inst = CreateDefault(cmd);
            if (!inst) return null;

            inst.name = nameof(ObjectTrapFixed);

            // Trap 스크립트 부착 및 AttackRange 연결
            var trap = ObjectFactoryBase.Add<ObjectTrapFixed>(inst);
            var attackRange = inst.GetComponentInChildren<ObjectTrapAttackRange>();
            trap.SetObjectTrapAttackRange(attackRange);

            // Trigger 여부 설정
            SetTrigger(inst, trap, useTrigger);

            return inst;
        }

        /// <summary>
        /// 타이머형(Animator 기반) Trap 생성
        /// </summary>
        public static GameObject CreateTimer(MenuCommand cmd, bool useTrigger = false)
        {
            var inst = CreateDefault(cmd);
            if (!inst) return null;

            inst.name = nameof(ObjectTrapTimer);

            var trap = ObjectFactoryBase.Add<ObjectTrapTimer>(inst);
            var attackRange = inst.GetComponentInChildren<ObjectTrapAttackRange>();
            trap.SetObjectTrapAttackRange(attackRange);

            SetTrigger(inst, trap, useTrigger);

            return inst;
        }

        /// <summary>
        /// 무한 반복형(Animator 기반) Trap 생성
        /// </summary>
        public static GameObject CreateInfinity(MenuCommand cmd)
        {
            var inst = CreateDefault(cmd);
            if (!inst) return null;

            inst.name = nameof(ObjectTrapInfinity);

            var trap = ObjectFactoryBase.Add<ObjectTrapInfinity>(inst);
            var attackRange = inst.GetComponentInChildren<ObjectTrapAttackRange>();
            trap.SetObjectTrapAttackRange(attackRange);

            return inst;
        }
        
        /// <summary>
        /// 특정 위치를 이동하는 Trap 생성
        /// </summary>
        public static GameObject CreateMove(MenuCommand cmd, bool useTrigger = false)
        {
            var inst = CreateDefault(cmd);
            if (!inst) return null;

            inst.name = nameof(ObjectTrapMoving);

            var trap = ObjectFactoryBase.Add<ObjectTrapMoving>(inst);
            var attackRange = inst.GetComponentInChildren<ObjectTrapAttackRange>();
            trap.SetObjectTrapAttackRange(attackRange);

            SetTrigger(inst, trap, useTrigger);

            return inst;
        }

        /// <summary>
        /// TriggerDetector 추가/삭제 제어
        /// - useTrigger = true : TrapTriggerDetector 유지 및 연결
        /// - useTrigger = false : 존재 시 즉시 제거
        /// </summary>
        private static void SetTrigger(GameObject inst, DefaultObjectTrap trap, bool useTrigger)
        {
            var triggerDetector = inst.GetComponentInChildren<TrapTriggerDetector>();

            if (useTrigger)
            {
                // TriggerDetector가 있으면 연결
                trap.SetTrapTriggerDetector(triggerDetector);
            }
            else
            {
                // TriggerDetector 필요 없으면 제거
                if (triggerDetector)
                    Object.DestroyImmediate(triggerDetector.gameObject);
            }
        }

        /// <summary>
        /// 지정된 경로의 프리팹을 인스턴스화
        /// - 실패 시 null 반환
        /// </summary>
        private static GameObject TryInstantiate(string path, MenuCommand cmd)
            => ObjectFactoryBase.TryInstantiatePrefab(path, cmd);

    }
}
#endif
