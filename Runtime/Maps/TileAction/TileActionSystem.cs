using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타일 액션 시스템
    /// - ApplyAction: 도구/이벤트로 들어온 액션 처리
    /// - OnTick: 시간 경과에 따른 상태 만료 처리
    /// - GridInformation에 상태/타이머를 저장하고 외형은 타일 교체만 수행(Per-Cell GO 생성 금지)
    /// </summary>
    [DisallowMultipleComponent]
    public class TileActionSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("메타데이터 저장용 GridInformation (2D Tilemap Extras)")]
        private GridInformation _gridInfo;

        [Tooltip("대상 Tilemap")]
        private Tilemap _tilemap;

        [Tooltip("상태/전이 정의 DB (ScriptableObject)")]
        [SerializeField] private TileStateDB db;

        [Tooltip("게임 시간 매니저 (씬에 1개)")]
        private GameTimeManager _timeManager;

        [Header("키 이름(고급)")]
        [SerializeField, Tooltip("셀별 상태 저장 키")] private string keyState = "state";
        [SerializeField, Tooltip("셀별 만료 틱 저장 키")] private string keyExpireTick = "expireTick";

        // 최근 변경된 셀만 대상(만료 검사 최적화)
        private readonly HashSet<Vector3Int> _touched = new HashSet<Vector3Int>();

        private void Reset()
        {
#if UNITY_EDITOR
            // _gridInfo = GetComponentInParent<GridInformation>();
            // tilemap  = GetComponent<Tilemap>();
            // _timeManager = FindAnyObjectByType<GameTimeManager>();
#endif
        }

        private void Start()
        {
            if (SceneGame.Instance)
            {
                if (!_gridInfo)
                    _gridInfo = SceneGame.Instance.mapManager.GetGridInformation();
                if (!_timeManager)
                    _timeManager = SceneGame.Instance.gameTimeManager;
            }
            _tilemap  = GetComponent<Tilemap>();
            if (_timeManager != null)
                _timeManager.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            if (_timeManager != null)
                _timeManager.OnTick -= HandleTick;
        }

        /// <summary>
        /// 외부에서 호출: 셀에 액션 적용(예: Water, Hoe...)
        /// - 전이 룰이 있으면 타일 교체 + 상태/만료 저장
        /// </summary>
        public bool ApplyAction(Vector3Int cell, TileAction action)
        {
            if (!_gridInfo || !_tilemap || !db || _timeManager == null) return false;

            var curState = (TileState)_gridInfo.GetPositionProperty(cell, keyState, (int)TileState.Dry);
            if (!db.TryGet(curState, action, out var tr)) return false;

            // 외형 교체
            if (tr.nextTile) _tilemap.SetTile(cell, tr.nextTile);

            // 상태 기록
            _gridInfo.SetPositionProperty(cell, keyState, (int)tr.to);

            // 만료 틱 설정(필요한 경우)
            if (tr.durationGameSeconds > 0f)
            {
                long expire = _timeManager.NowTick + _timeManager.SecondsToTicks(tr.durationGameSeconds);
                _gridInfo.SetPositionProperty(cell, keyExpireTick, (int)expire);
                _touched.Add(cell);
            }
            else
            {
                // 만료 사용 안함이면 기존 값 제거(선택)
                var positionProperty = _gridInfo.GetPositionProperty(cell, keyExpireTick, -1);
                if (positionProperty != -1)
                    _gridInfo.ErasePositionProperty(cell, keyExpireTick);
                _touched.Remove(cell);
            }

            // (선택) VFX/SFX 훅
            // PlayFx(tr, tilemap.CellToWorld(cell) + tilemap.tileAnchor);

            return true;
        }

        /// <summary>
        /// GameTimeManager의 고정 틱에서 만료 검사
        /// - '최근 갱신된 셀'만 확인하여 퍼포먼스 최적화
        /// - 만료 시 DB에 등록된 (현재상태, TimeTick) 전이를 적용
        /// </summary>
        private void HandleTick(long nowTick)
        {
            if (_touched.Count == 0) return;

            // 재할당 없이 순회하기 위해 임시 배열 활용(필요시 StaticList로 교체)
            Scratch.Clear();
            Scratch.AddRange(_touched);

            foreach (var cell in Scratch)
            {
                int expire = _gridInfo.GetPositionProperty(cell, keyExpireTick, int.MinValue);
                if (expire == int.MinValue) { _touched.Remove(cell); continue; } // 더이상 타이머 없음

                if (nowTick >= expire)
                {
                    // 만료: (현재 상태, TimeTick) 전이 시도
                    var cur = (TileState)_gridInfo.GetPositionProperty(cell, keyState, (int)TileState.Dry);
                    if (db.TryGet(cur, TileAction.TimeTick, out var tr))
                    {
                        if (tr.nextTile) _tilemap.SetTile(cell, tr.nextTile);
                        _gridInfo.SetPositionProperty(cell, keyState, (int)tr.to);

                        // 후속 타이머
                        if (tr.durationGameSeconds > 0f)
                        {
                            long nextExpire = nowTick + _timeManager.SecondsToTicks(tr.durationGameSeconds);
                            _gridInfo.SetPositionProperty(cell, keyExpireTick, (int)nextExpire);
                            _touched.Add(cell);
                        }
                        else
                        {
                            _gridInfo.ErasePositionProperty(cell, keyExpireTick);
                            _touched.Remove(cell);
                        }
                    }
                    else
                    {
                        // 만료 전이 정의가 없으면 타이머만 제거
                        _gridInfo.ErasePositionProperty(cell, keyExpireTick);
                        _touched.Remove(cell);
                    }
                }
            }
        }
        /// <summary>
        /// 빈 셀을 초기 상태/타일로 준비합니다.
        /// - baseTile이 지정되어 있고 셀이 비어있으면 타일을 생성
        /// - GridInformation에 초기 상태 저장
        /// - 만료 타이머 키가 남아있다면 제거
        /// </summary>
        public bool InitializeCell(Vector3Int cell, TileState initialState, TileBase baseTile)
        {
            if (!_gridInfo || !_tilemap) return false;

            // 1) 타일 생성(비어있는 경우)
            if (baseTile && !_tilemap.HasTile(cell))
            {
                _tilemap.SetTile(cell, baseTile);
            }

            // 2) 상태 기록
            _gridInfo.SetPositionProperty(cell, keyState, (int)initialState);

            // 3) 만료 타이머 제거(깨끗한 시작)
            var positionProperty = _gridInfo.GetPositionProperty(cell, keyExpireTick, -1);
            if (positionProperty != -1)
                _gridInfo.ErasePositionProperty(cell, keyExpireTick);

            // 4) 만료 검사 대상 세트에서 제거
            // (초기화만 하고 지속 상태가 아니므로)
            _touched.Remove(cell);

            return true;
        }
        // 임시 버퍼
        private static readonly List<Vector3Int> Scratch = new List<Vector3Int>(128);
    }
}
