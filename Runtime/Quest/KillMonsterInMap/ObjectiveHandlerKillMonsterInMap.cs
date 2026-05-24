using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 지정한 맵에 배치된 몬스터를 모두 처치하는 퀘스트 목표를 처리합니다.
    /// </summary>
    public class ObjectiveHandlerKillMonsterInMap : ObjectiveHandlerBase
    {
        private readonly HashSet<int> _targetMonsterVids = new HashSet<int>();
        private readonly HashSet<int> _killedMonsterVids = new HashSet<int>();

        private QuestStep _currentStep;
        private QuestData _questData;
        private int _currentQuestUid;
        private int _currentCount;
        private bool _isRegisteredMonsterKilled;
        private bool _isRegisteredMapEntered;

        /// <summary>
        /// 기본 맵 전체 몬스터 처치 목표 처리기를 생성합니다.
        /// </summary>
        public ObjectiveHandlerKillMonsterInMap()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 맵 전체 몬스터 처치 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        public ObjectiveHandlerKillMonsterInMap(IObjectiveCompletionSink completionSink) : base(completionSink)
        {
        }

        /// <summary>
        /// 맵 전체 몬스터 처치 목표를 시작하고 현재 맵 상태를 기준으로 대상 몬스터를 수집합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">현재 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">이 목표에서는 사용하지 않는 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            _currentQuestUid = questUid;
            _currentStep = step;
            _questData = SceneGame.Instance?.saveDataManager?.Quest;
            _currentCount = _questData != null ? _questData.GetCount(_currentQuestUid) : 0;

            RegisterEvents();
            TryCaptureCurrentMapTargets();
        }

        /// <summary>
        /// 목표 달성 여부를 확인합니다.
        /// </summary>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <returns>처치한 몬스터 수가 목표 몬스터 수 이상이면 true입니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return step != null && step.count > 0 && _currentCount >= step.count;
        }

        /// <summary>
        /// 필요한 게임 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterEvents()
        {
            if (!_isRegisteredMonsterKilled)
            {
                GameEventManager.MonsterKilledEvent += OnMonsterKilled;
                _isRegisteredMonsterKilled = true;
            }

            if (!_isRegisteredMapEntered)
            {
                GameEventManager.MapEnteredEvent += OnMapEntered;
                _isRegisteredMapEntered = true;
            }
        }

        /// <summary>
        /// 현재 로드된 맵이 목표 맵이면 해당 맵의 살아있는 몬스터 VID를 목표 목록으로 기록합니다.
        /// </summary>
        private void TryCaptureCurrentMapTargets()
        {
            MapManager mapManager = SceneGame.Instance?.mapManager;
            if (mapManager == null || _currentStep == null) return;
            if (mapManager.GetCurrentMapUid() != _currentStep.mapUid) return;

            CaptureMonsterTargets(mapManager.GetCurrentMapMonsterEntries());
        }

        /// <summary>
        /// 맵 입장 완료 시 목표 맵에 들어온 경우 몬스터 목표 목록을 다시 구성합니다.
        /// </summary>
        /// <param name="eventData">입장한 맵 정보입니다.</param>
        private void OnMapEntered(MapEnteredEventData eventData)
        {
            if (_currentStep == null || eventData.MapUid != _currentStep.mapUid) return;
            TryCaptureCurrentMapTargets();
        }

        /// <summary>
        /// 현재 맵의 살아있는 몬스터들을 이번 목표의 처치 대상으로 확정합니다.
        /// </summary>
        /// <param name="monsterEntries">현재 맵의 몬스터 VID와 게임 오브젝트 쌍 목록입니다.</param>
        private void CaptureMonsterTargets(List<KeyValuePair<int, GameObject>> monsterEntries)
        {
            _targetMonsterVids.Clear();
            _killedMonsterVids.Clear();

            if (monsterEntries != null)
            {
                foreach (KeyValuePair<int, GameObject> entry in monsterEntries)
                {
                    Monster monster = entry.Value != null ? entry.Value.GetComponent<Monster>() : null;
                    if (monster == null || monster.IsStatusDead()) continue;
                    _targetMonsterVids.Add(entry.Key);
                }
            }

            _currentStep.count = _targetMonsterVids.Count;
            _currentCount = Mathf.Clamp(_currentCount, 0, _currentStep.count);
            _questData?.SaveCount(_currentQuestUid, _currentCount);

            if (_currentStep.count <= 0 || _currentCount >= _currentStep.count)
            {
                CompleteObjective();
            }
        }

        /// <summary>
        /// 몬스터 사망 이벤트를 받아 목표 맵의 대상 몬스터 처치 수를 갱신합니다.
        /// </summary>
        /// <param name="eventData">사망한 몬스터 정보입니다.</param>
        private void OnMonsterKilled(MonsterKilledEventData eventData)
        {
            if (_currentStep == null || _questData == null) return;
            if (eventData.mapUid != _currentStep.mapUid) return;
            if (!_targetMonsterVids.Contains(eventData.monsterVid)) return;
            if (!_killedMonsterVids.Add(eventData.monsterVid)) return;

            _currentCount = Mathf.Clamp(_currentCount + 1, 0, _currentStep.count);
            _questData.SaveCount(_currentQuestUid, _currentCount);

            if (_currentCount >= _currentStep.count)
            {
                CompleteObjective();
            }
        }

        /// <summary>
        /// 맵 전체 몬스터 처치 목표를 완료하고 소유 퀘스트 매니저에 다음 단계 진행을 요청합니다.
        /// </summary>
        private void CompleteObjective()
        {
            int completedQuestUid = _currentQuestUid;
            OnDispose();
            CompleteObjectiveThroughOwner(completedQuestUid);
        }

        /// <summary>
        /// 구독한 이벤트를 해제합니다.
        /// </summary>
        public override void OnDispose()
        {
            if (_isRegisteredMonsterKilled)
            {
                GameEventManager.MonsterKilledEvent -= OnMonsterKilled;
                _isRegisteredMonsterKilled = false;
            }

            if (_isRegisteredMapEntered)
            {
                GameEventManager.MapEnteredEvent -= OnMapEntered;
                _isRegisteredMapEntered = false;
            }
        }
    }
}
