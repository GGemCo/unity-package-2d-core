using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 드랍 아이템
    /// </summary>
    public class Item : MonoBehaviour
    {
        [Header("드랍 애니메이션 설정")]
        [Tooltip("최소 드랍 거리 (픽셀 단위). 너무 작으면 아이템들이 겹칠 수 있습니다.")]
        [SerializeField] private float minDistance = 40f;

        [Tooltip("최대 드랍 거리 (픽셀 단위). 아이템이 퍼지는 최대 반경입니다.")]
        [SerializeField] private float maxDistance = 80f;

        [Tooltip("비행 시간 (초 단위). 짧을수록 빠르게 떨어집니다.")]
        [SerializeField] private float flightTime = 0.3f;

        [Tooltip("중력 가속도 값. 클수록 빠르게 떨어집니다.")]
        [SerializeField] private float gravity = 25f;

        [Tooltip("최고점에서의 크기 증가 배율. 예: 1.2f = 20% 커짐.")]
        [SerializeField] private float scaleMultiplier = 1.2f;

        [Tooltip("드랍된 아이템 간의 최소 간격 (픽셀 단위).")]
        [SerializeField] private float minSpacing = 20f;

        [Tooltip("착지 시 살짝 튀는 바운스 높이.")]
        [SerializeField] private float bounceHeight = 5f;

        [Tooltip("회전 속도 (도/초 단위). 떨어질 때의 회전 효과.")]
        [SerializeField] private float rotationSpeed = 180f;
        
        [Tooltip("네임 태그 생성 여부")]
        [SerializeField] private bool useNameTag = true;

        [Header("Sorting Layer")]
        [SerializeField] private ConfigSortingLayer.Keys sortingLayerName = ConfigSortingLayer.Keys.CharacterTop;

        private static readonly List<Vector2> DroppedItemPositions = new List<Vector2>(); // 드랍된 아이템 위치 저장
        
        private int _itemUid;
        private long _itemCount;
        private long _instanceId;
        private string _sourceKey;
        private long _runtimeToken;
        private bool _disableAutoDespawn;
        private WorldItemPickupPolicy _pickupPolicy;
        private GameObject _containerItemName;
        private GameObject _objectTagNameItem;
        private Vector2 _startPos;
        private Vector2 _targetPos;
        private float _timeElapsed;
        private Vector2 _velocity;
        private Vector3 _originalScale;
        private float _peakTime;
        private bool _isBouncing; // 바운스 여부 체크
        private float _bounceTime; // 바운스 지속 시간
        private float _rotationDirection; // 랜덤 회전 방향
        private float _mapSizeHeight;

        // 드랍 후 dropItemDestroyTimeSec 초가 지나면 파괴되는 코루틴.
        private Coroutine _coroutineDropItemDestroy;
        //드랍된 후 자동 파괴되기까지의 시간 (초 단위). AddressableLoaderSettings에서 가져옴.
        private int _dropItemDestroyTimeSec;

        private Renderer _itemRenderer;
        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _circleCollider2D;
        private DropItemVisualHost _visualHost;

        private bool _isStart;
        private ItemManager _itemManager;
        
        private void Awake()
        {
            _timeElapsed = 0f;
            _isBouncing = false;
            _bounceTime = 0.1f;
            _originalScale = transform.localScale; // 원래 크기 저장
            
            tag = ConfigTags.GetValue(ConfigTags.Keys.DropItem);
            
            _itemRenderer = GetComponent<Renderer>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _circleCollider2D = GetComponent<CircleCollider2D>();
            _visualHost = GetComponent<DropItemVisualHost>();
            if (_visualHost == null)
            {
                _visualHost = gameObject.AddComponent<DropItemVisualHost>();
            }
            _circleCollider2D.enabled = false;
            
            _dropItemDestroyTimeSec = AddressableLoaderSettings.Instance.settings.dropItemDestroyTimeSec;
        }

        private void Start()
        {
            _itemManager = SceneGame.Instance.ItemManager;
        }
        /// <summary>
        /// 맵에 드랍하기 시작 
        /// </summary>
        public void StartDrop()
        {
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            if (_itemUid <= 0) return;
            var info = TableLoaderManager.Instance.GetItemData(_itemUid);
            if (info == null || info.Uid <= 0) return;
            
            _itemRenderer.sortingLayerName = ConfigSortingLayer.GetValue(sortingLayerName);
            _itemRenderer.sortingOrder = 1;
            _visualHost?.ApplySorting(_itemRenderer.sortingLayerName, _itemRenderer.sortingOrder);
            _timeElapsed = 0f;
            _isBouncing = false;
            transform.localScale = Vector3.one;

            _visualHost?.Bind(_itemUid, info.FileName);

            // 특정 반경 내에서 랜덤한 위치 선택 (X, Y 축 모두 분산)
            int maxAttempts = 10; // 겹치지 않도록 최대 시도 횟수
            bool positionValid = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                // 랜덤한 원형 반경 내에서 위치 선택
                Vector2 randomOffset = Random.insideUnitCircle * Random.Range(minDistance, maxDistance);
                Vector2 potentialTargetPos = _startPos + randomOffset;

                // 아이템 간 거리 검사 (겹치지 않도록)
                bool tooClose = false;
                foreach (Vector2 pos in DroppedItemPositions)
                {
                    if (Vector2.Distance(potentialTargetPos, pos) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    _targetPos = potentialTargetPos;
                    positionValid = true;
                    break;
                }
            }

            if (!positionValid)
            {
                _targetPos = _startPos + new Vector2(Random.Range(minDistance, maxDistance), 0f); // 겹칠 경우 대략적인 위치 설정
            }

            DroppedItemPositions.Add(_targetPos); // 새로운 아이템 위치 저장

            // 속도 계산 (목표 지점까지 flightTime 내에 도달하도록)
            _velocity.x = (_targetPos.x - _startPos.x) / flightTime;
            _velocity.y = (_targetPos.y - _startPos.y) / flightTime + (0.5f * gravity * flightTime); // 최고점 고려

            _peakTime = flightTime / 2; // 최고점 도달 시간

            // 랜덤한 회전 방향 설정
            _rotationDirection = Random.Range(-1f, 1f);

            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            _mapSizeHeight = size.y;
            
            _isStart = true;

            CreateTagName();
        }
        /// <summary>
        /// 아이템 이름 tag 만들기
        /// </summary>
        private void CreateTagName()
        {
            if (!useNameTag) return;
            GameObject prefabTagNameItem = ConfigResources.TextDropItemNameTag.Load();
            if (prefabTagNameItem == null) return;
            if (_containerItemName == null)
            {
                _containerItemName = SceneGame.Instance.containerDropItemName;
            }
            _objectTagNameItem = Instantiate(prefabTagNameItem, _containerItemName.transform);
            if (_objectTagNameItem == null) return;
            TagNameItem tagNameItem = _objectTagNameItem.GetComponent<TagNameItem>();
            if (tagNameItem == null) return;
            tagNameItem.Initialize(gameObject, _itemCount);
        }
        /// <summary>
        /// 드랍 애니메이션 처리  
        /// </summary>
        private void Update()
        {
            if (!_isStart) return;
            
            _timeElapsed += Time.deltaTime;

            if (!_isBouncing)
            {
                // 포물선 이동 계산
                float x = _startPos.x + _velocity.x * _timeElapsed;
                float y = _startPos.y + (_velocity.y * _timeElapsed) - (0.5f * gravity * _timeElapsed * _timeElapsed);
                transform.position = new Vector2(x, y);

                // 최고점 도달 시 크기 증가
                float scaleLerp = Mathf.Lerp(1f, scaleMultiplier, Mathf.Sin((_timeElapsed / _peakTime) * Mathf.PI));
                transform.localScale = _originalScale * scaleLerp;

                // 회전 효과 추가 (자연스러운 낙하)
                transform.Rotate(0, 0, _rotationDirection * rotationSpeed * Time.deltaTime);
            }

            // 착지하면 바운스 효과 적용
            if (_timeElapsed >= flightTime && !_isBouncing)
            {
                StartCoroutine(BounceEffect());
            }
        }
        /// <summary>
        /// 드랍된 후 bounce 애니메이션 처리
        /// </summary>
        /// <returns></returns>
        private IEnumerator BounceEffect()
        {
            _isBouncing = true;
            Vector2 groundPos = transform.position;
            Vector2 bouncePos = groundPos + new Vector2(0, bounceHeight);

            // 위로 살짝 튀기기
            float elapsed = 0f;
            while (elapsed < _bounceTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector2.Lerp(groundPos, bouncePos, Mathf.Sin((elapsed / _bounceTime) * Mathf.PI));
                yield return null;
            }

            // 원래 위치로 복귀
            elapsed = 0f;
            while (elapsed < _bounceTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector2.Lerp(bouncePos, groundPos, elapsed / _bounceTime);
                yield return null;
            }

            // 최종적으로 원래 크기로 복귀
            OnEnd();
        }
        /// <summary>
        /// 드랍 완료
        /// </summary>
        private void OnEnd()
        {
            _isStart = false;
            transform.localScale = _originalScale;
            _isBouncing = false;
            // 드랍된 후에는 캐릭터 layer 로 적용한다.
            _itemRenderer.sortingLayerName = ConfigSortingLayer.GetValue(sortingLayerName);
            _itemRenderer.sortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            _circleCollider2D.enabled = true;
            _circleCollider2D.isTrigger = true;

            if (!_disableAutoDespawn && _dropItemDestroyTimeSec > 0)
                _coroutineDropItemDestroy = StartCoroutine(CheckDestroyTime());
        }
        /// <summary>
        /// 플레이어가 아이템을 먹거나 맵에서 없어졌을때 
        /// </summary>
        private void OnDisable()
        {
            StopCoroutineDropItemDestroy();
            _circleCollider2D.enabled = false;
            if (_objectTagNameItem != null)
            {
                _objectTagNameItem.SetActive(false);
            }
        }
        /// <summary>
        /// 플레이어가 아이템을 먹 후, 사라지는 시간이 다 되었을때 처리
        /// </summary>
        public void ResetInfos(bool removeInstanceFromDb)
        {
            if (removeInstanceFromDb && _instanceId > 0)
            {
                // 바닥에서 사라지는 등 미획득 처리 시 인스턴스 데이터도 함께 제거한다.
                SceneGame.Instance?.saveDataManager?.ItemInstances?.Remove(_instanceId);
            }

            // DropItem 비활성화 전, 자식으로 붙어있는 VFX를 먼저 정리한다.
            // OnDisable 시점에는 부모 오브젝트가 활성/비활성 전환 중일 수 있어서
            // 풀 반환 과정의 SetParent 가 Unity 에러를 발생시킬 수 있다.
            if (gameObject.activeInHierarchy)
                _visualHost?.ReleaseVisual();

            _itemUid = 0;
            _itemCount = 0;
            _instanceId = 0;
            _sourceKey = null;
            _runtimeToken = 0;
            _disableAutoDespawn = false;
            _pickupPolicy = WorldItemPickupPolicy.Default;
            gameObject.SetActive(false);
            (_itemManager ?? SceneGame.Instance?.ItemManager)?.AddPoolDropItem(this);
        }

        IEnumerator CheckDestroyTime()
        {
            yield return new WaitForSeconds(_dropItemDestroyTimeSec);
            // 바닥에서 사라질 때는 인스턴스도 함께 제거(미획득 처리)
            ResetInfos(true);
        }
        /// <summary>
        /// 시간 되면 자동으로 파괴되는 코루틴 정지 
        /// </summary>
        private void StopCoroutineDropItemDestroy()
        {
            if (_coroutineDropItemDestroy == null) return;
            StopCoroutine(_coroutineDropItemDestroy);
        }
        /// <summary>
        /// 기존 int 수량 기반 드랍 아이템 정보를 초기화합니다.
        /// </summary>
        public void Initialize(int itemUid, int itemCount, Vector2 startPos, long instanceId = 0)
        {
            Initialize(itemUid, (long)itemCount, startPos, instanceId);
        }

        /// <summary>
        /// 드랍 아이템의 수량, 위치와 런타임 식별 정보를 초기화합니다.
        /// </summary>
        /// <param name="itemUid">아이템 UID입니다.</param>
        /// <param name="itemCount">아이템 수량입니다.</param>
        /// <param name="startPos">드랍 애니메이션 시작 좌표입니다.</param>
        /// <param name="instanceId">아이템 인스턴스 ID입니다.</param>
        /// <param name="sourceKey">드랍을 생성한 상위 시스템의 출처 키입니다.</param>
        /// <param name="runtimeToken">현재 유효한 드랍을 식별하는 런타임 토큰입니다.</param>
        /// <param name="disableAutoDespawn">자동 제거 시간을 적용하지 않을지 여부입니다.</param>
        /// <param name="pickupPolicy">플레이어가 월드 아이템을 획득할 수 있는 조건입니다.</param>
        public void Initialize(
            int itemUid,
            long itemCount,
            Vector2 startPos,
            long instanceId = 0,
            string sourceKey = null,
            long runtimeToken = 0,
            bool disableAutoDespawn = false,
            WorldItemPickupPolicy pickupPolicy = WorldItemPickupPolicy.Default)
        {
            _itemUid = itemUid;
            _itemCount = itemCount;
            _startPos = startPos;
            _instanceId = instanceId;
            _sourceKey = sourceKey;
            _runtimeToken = runtimeToken;
            _disableAutoDespawn = disableAutoDespawn;
            _pickupPolicy = pickupPolicy;
        }

        /// <summary>
        /// 지정한 플레이어가 현재 월드 아이템을 획득할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="player">아이템 획득을 시도한 플레이어입니다.</param>
        /// <returns>현재 획득 정책을 만족하면 <see langword="true"/>입니다.</returns>
        public bool CanBeCollectedBy(Player player)
        {
            // 기본 정책은 기존 동작과 하위 호환성을 유지하며, 생존 조건이 명시된 드랍만 상태를 검사합니다.
            return _pickupPolicy != WorldItemPickupPolicy.RequirePlayerAlive ||
                   (player != null && !player.IsStatusDead());
        }

        /// <summary>
        /// 현재 드랍 아이템 UID를 반환합니다.
        /// </summary>
        public int GetItemUid()
        {
            return _itemUid;
        }

        /// <summary>
        /// 기존 int 기반 호출부와의 호환을 위한 아이템 수량을 반환합니다.
        /// </summary>
        public int GetItemCount()
        {
            return _itemCount >= int.MaxValue ? int.MaxValue : (int)_itemCount;
        }

        /// <summary>
        /// 현재 드랍 아이템의 실제 long 수량을 반환합니다.
        /// </summary>
        public long GetItemCountLong()
        {
            return _itemCount;
        }

        /// <summary>
        /// 현재 드랍 아이템 인스턴스 ID를 반환합니다.
        /// </summary>
        public long GetInstanceId()
        {
            return _instanceId;
        }

        /// <summary>
        /// 현재 드랍 아이템의 런타임 출처 키를 반환합니다.
        /// </summary>
        public string GetSourceKey()
        {
            return _sourceKey;
        }

        /// <summary>
        /// 현재 드랍 아이템의 런타임 토큰을 반환합니다.
        /// </summary>
        public long GetRuntimeToken()
        {
            return _runtimeToken;
        }
    }
}
