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
        public float minDistance = 40f;

        [Tooltip("최대 드랍 거리 (픽셀 단위). 아이템이 퍼지는 최대 반경입니다.")]
        public float maxDistance = 80f;

        [Tooltip("비행 시간 (초 단위). 짧을수록 빠르게 떨어집니다.")]
        public float flightTime = 0.3f;

        [Tooltip("중력 가속도 값. 클수록 빠르게 떨어집니다.")]
        public float gravity = 25f;

        [Tooltip("최고점에서의 크기 증가 배율. 예: 1.2f = 20% 커짐.")]
        public float scaleMultiplier = 1.2f;

        [Tooltip("드랍된 아이템 간의 최소 간격 (픽셀 단위).")]
        public float minSpacing = 20f;

        [Tooltip("착지 시 살짝 튀는 바운스 높이.")]
        public float bounceHeight = 5f;

        [Tooltip("회전 속도 (도/초 단위). 떨어질 때의 회전 효과.")]
        public float rotationSpeed = 180f;

        private static readonly List<Vector2> DroppedItemPositions = new List<Vector2>(); // 드랍된 아이템 위치 저장
        
        private int _itemUid;
        private int _itemCount;
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

        private bool _isStart;
        private ItemManager _itemManager;
        
        private void Awake()
        {
            _timeElapsed = 0f;
            _isBouncing = false;
            _bounceTime = 0.1f;
            _originalScale = transform.localScale; // 원래 크기 저장

            _itemRenderer = GetComponent<Renderer>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _circleCollider2D = GetComponent<CircleCollider2D>();
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
            
            _itemRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);
            _itemRenderer.sortingOrder = 1;
            _timeElapsed = 0f;
            _isBouncing = false;
            transform.localScale = Vector3.one;

            _spriteRenderer.sprite = AddressableLoaderItem.Instance.GetImageDropByName(info.FileName);

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
            _itemRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.Character);
            _itemRenderer.sortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            _circleCollider2D.enabled = true;
            _circleCollider2D.isTrigger = true;

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
        public void Reset()
        {
            _itemUid = 0;
            gameObject.SetActive(false);
            _itemManager.AddPoolDropItem(this);
        }

        IEnumerator CheckDestroyTime()
        {
            yield return new WaitForSeconds(_dropItemDestroyTimeSec);
            Reset();
        }
        /// <summary>
        /// 시간 되면 자동으로 파괴되는 코루틴 정지 
        /// </summary>
        private void StopCoroutineDropItemDestroy()
        {
            if (_coroutineDropItemDestroy == null) return;
            StopCoroutine(_coroutineDropItemDestroy);
        }
        public void Initialize(int itemUid, int itemCount, Vector2 startPos)
        {
            _itemUid = itemUid;
            _itemCount = itemCount;
            _startPos = startPos;
        }

        public int GetItemUid()
        {
            return _itemUid;
        }

        public int GetItemCount()
        {
            return _itemCount;
        }
    }
}