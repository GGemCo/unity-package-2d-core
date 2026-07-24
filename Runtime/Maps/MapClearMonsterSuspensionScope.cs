using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 종료 연출 동안 현재 맵의 활성 생존 몬스터가 판단하거나 이동하지 못하도록 잠금을 관리합니다.
    /// </summary>
    internal sealed class MapClearMonsterSuspensionScope
    {
        private const int MapClearSkillMotionCancelReason = 9951;

        private readonly List<Monster> _suspendedMonsters = new(16);
        private object _owner;

        /// <summary>
        /// 현재 맵에서 활성화된 생존 몬스터의 Brain과 이동을 중단합니다.
        /// </summary>
        /// <param name="mapManager">현재 맵 몬스터 목록을 제공하는 맵 관리자입니다.</param>
        /// <param name="owner">잠금을 소유할 맵 종료 컨트롤러입니다.</param>
        /// <param name="cancelRunningSkills">실행 중인 스킬과 스킬 이동 모션을 취소할지 여부입니다.</param>
        /// <param name="switchToIdle">중단 직후 대기 상태와 애니메이션으로 전환할지 여부입니다.</param>
        /// <returns>중단 잠금을 적용한 몬스터 수입니다.</returns>
        public int Suspend(
            MapManager mapManager,
            object owner,
            bool cancelRunningSkills,
            bool switchToIdle)
        {
            Release();
            if (mapManager == null || owner == null)
            {
                return 0;
            }

            _owner = owner;
            List<KeyValuePair<int, GameObject>> monsterEntries =
                mapManager.GetCurrentMapMonsterEntries();
            for (int i = 0; i < monsterEntries.Count; i++)
            {
                GameObject monsterObject = monsterEntries[i].Value;
                if (monsterObject == null || !monsterObject.activeInHierarchy)
                {
                    continue;
                }

                Monster monster = monsterObject.GetComponent<Monster>();
                if (monster == null || monster.IsStatusDead() || monster.IsDeathPending)
                {
                    continue;
                }

                SuspendMonster(monster, cancelRunningSkills, switchToIdle);
                _suspendedMonsters.Add(monster);
            }

            return _suspendedMonsters.Count;
        }

        /// <summary>
        /// 이 범위에서 획득한 모든 몬스터 Brain 및 이동 잠금을 해제합니다.
        /// </summary>
        public void Release()
        {
            if (_owner == null)
            {
                _suspendedMonsters.Clear();
                return;
            }

            for (int i = 0; i < _suspendedMonsters.Count; i++)
            {
                Monster monster = _suspendedMonsters[i];
                if (monster == null)
                {
                    continue;
                }

                monster.ReleaseBrainLock(_owner);
                monster.ReleaseMovementLock(_owner);
            }

            _suspendedMonsters.Clear();
            _owner = null;
        }

        /// <summary>
        /// 단일 몬스터에 Brain 및 이동 잠금을 적용하고 이미 전달된 이동 명령을 즉시 정리합니다.
        /// </summary>
        /// <param name="monster">중단할 활성 생존 몬스터입니다.</param>
        /// <param name="cancelRunningSkills">실행 중인 스킬과 스킬 이동 모션을 취소할지 여부입니다.</param>
        /// <param name="switchToIdle">대기 상태와 애니메이션으로 전환할지 여부입니다.</param>
        private void SuspendMonster(
            Monster monster,
            bool cancelRunningSkills,
            bool switchToIdle)
        {
            monster.AcquireBrainLock(_owner);
            monster.AcquireMovementLock(_owner);

            // Brain 잠금 이전 프레임에 BT가 전달한 연속 이동 의도를 제거해야
            // 낮은 BT Tick 주기에서도 맵 종료와 같은 프레임에 이동이 멈춥니다.
            ControllerMonster controller = monster.GetComponent<ControllerMonster>();
            controller?.RequestStopMoveIntent();
            controller?.StopAttackCoroutine();

            if (cancelRunningSkills)
            {
                ISkillCancelableDriver skillDriver = monster.GetComponent<ISkillCancelableDriver>();
                skillDriver?.RequestCancelSkill(SkillCancelReason.ForcedBySystem);

                ICharacterMotionController motionController =
                    monster.GetComponent<ICharacterMotionController>();
                motionController?.CancelMotion(
                    MotionChannel.Skill,
                    MapClearSkillMotionCancelReason);
            }

            monster.directionNormalize = Vector3.zero;
            if (monster.characterRigidbody2D != null)
            {
                // 이동 의도를 제거해도 직전 프레임의 물리 속도가 남을 수 있으므로 관성을 함께 정리합니다.
                monster.characterRigidbody2D.linearVelocity = Vector2.zero;
                monster.characterRigidbody2D.angularVelocity = 0f;
            }

            if (switchToIdle)
            {
                monster.Stop(isForce: true);
                controller?.RequestWait();
            }
        }
    }
}
