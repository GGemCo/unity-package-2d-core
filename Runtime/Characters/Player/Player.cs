using System;
using System.Collections.Generic;
using UnityEngine;
using R3;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 
    /// </summary>
    public class Player : CharacterBase
    {
        // 공격할 몬스터 
        private GameObject _targetMonster;
        private GGemCoSettings _gGemCoSettings; 
        private EquipController _equipController;
        private ControllerPlayer _controllerPlayer;
        private UIWindowHud _uiWindowHud;
        private UIWindowPlayerInfo _uiWindowPlayerInfo;
        private UIWindowPlayerBuffInfo _uiWindowPlayerBuffInfo;
        private PlayerData _playerData;
        private SceneGame _sceneGame;
        // 충돌 체크할 몬스터 수  
        private const int CountCollider = 10;
        private Collider2D[] _collider2Ds;
            
        [Serializable]
        private struct StatUIBinding
        {
            public UIWindowPlayerInfo.IndexPlayerInfo textUI;
            public Func<Player, BehaviorSubject<long>> GetStat;
            public string label;
        }
        private readonly List<StatUIBinding> _statBindings = new();
        protected override void Awake()
        {
            // 먼저 선언한다.
            IsUseSkill = true;
            base.Awake();
            _collider2Ds = new Collider2D[CountCollider];
        }
        protected override void Start()
        {
            base.Start();
            _sceneGame = SceneGame.Instance;
            _playerData = _sceneGame.saveDataManager.Player;
            _uiWindowHud = _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowHud>(UIWindowConstants.WindowUid.Hud);
            _uiWindowPlayerInfo =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerInfo>(UIWindowConstants.WindowUid.PlayerInfo);
            _uiWindowPlayerBuffInfo =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowPlayerBuffInfo>(UIWindowConstants.WindowUid
                    .PlayerBuffInfo);
            // 연출중 체크를 위해 추가
            _controllerPlayer.Initialize(_sceneGame.CutsceneManager);
            
            // TotalHp, Mp 가 바뀌어도 현재 값이 바뀌면 안된다.
            TotalHp
                .Subscribe(_ => SetWindowHudSliderHp(CurrentHp.Value))
                .AddTo(this);
            CurrentHp
                .Subscribe(_ => SetWindowHudSliderHp(CurrentHp.Value))
                .AddTo(this);
            TotalMp
                .Subscribe(_ => SetWindowHudSliderMp(CurrentMp.Value))
                .AddTo(this);
            CurrentMp
                .Subscribe(_ => SetWindowHudSliderMp(CurrentMp.Value))
                .AddTo(this);

            LoadEquipItems();
            InitializeStatBindings();
        }
        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.Player);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected override void InitComponents()
        {
            // AddComponent 순서 중요
            base.InitComponents();
            _controllerPlayer = gameObject.AddComponent<ControllerPlayer>();
            _equipController = gameObject.AddComponent<EquipController>();
            ComponentController.AddRigidbody2D(gameObject);
            
            GameObject attackRange = new GameObject("AttackRange");
            CharacterAttackRange characterAttackRange = attackRange.AddComponent<CharacterAttackRange>();
            characterAttackRange.Initialize(this);
            
            // 공격 범위 안에 몬스터 찾는 용도
            // include layer : 타일맵 wall 제외하고 모두 포함
            // exclude layer : 타일맵 wall
            Vector2 offset = Vector2.zero;
            Vector2 size = new Vector2(500, 250);
            colliderCheckCharacter = ComponentController.AddCapsuleCollider2D(attackRange, true, offset, size);
            
            // hit area
            GameObject hitArea = new GameObject("HitArea");
            CharacterHitArea characterHitArea = hitArea.AddComponent<CharacterHitArea>();
            characterHitArea.Initialize(this);
            
            
            // 맵 object 충돌 체크용
            // include layer : 타일맵 wall
            // exclude layer : 타일맵 wall 제외하고 모두 포함
            offset = Vector2.zero;
            size = new Vector2(264,132);
            ComponentController.AddCapsuleCollider2D(gameObject, false, offset, size,
                LayerMask.GetMask(ConfigLayer.GetValue(ConfigLayer.Keys.TileMapWall)),
                ~ (1 << LayerMask.NameToLayer(ConfigLayer.GetValue(ConfigLayer.Keys.TileMapWall))));
        }

        /// <summary>
        /// GGemCoPlayerSettings 에서 가져온 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            GGemCoPlayerSettings playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            SetBaseInfos(playerSettings.statAtk, playerSettings.statDef, playerSettings.statHp, playerSettings.statMp,
                playerSettings.statMoveSpeed, playerSettings.statAttackSpeed, playerSettings.statRegistFire,
                playerSettings.statRegistCold, playerSettings.statRegistLightning);
            CurrentHp.OnNext(TotalHp.Value);
            CurrentMp.OnNext(TotalMp.Value);
            currentMoveStep = playerSettings.statMoveStep;
            originalScaleX = transform.localScale.x;
            SetScale(playerSettings.startScale);
        }
        /// <summary>
        /// 세이브 데이터에 있는 장착 아이템 정보 가져와서 장착 시키기
        /// </summary>
        private void LoadEquipItems()
        {
            Dictionary<int, SaveDataIcon> dictionary =
                _sceneGame.saveDataManager.Equip.GetAllItemCounts();
            foreach (var info in dictionary)
            {
                if (info.Value == null) continue;
                int itemUid = info.Value.Uid;
                int itemCount = info.Value.Count;
                if (itemUid <= 0) continue;
                EquipItem(info.Key, itemUid, itemCount);
            }
        }
        protected void OnTriggerEnter2D(Collider2D collision)
        {
            // 워프 일때
            if (collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapObjectWarp)))
            {
                ObjectWarp objectWarp = collision.gameObject.GetComponent<ObjectWarp>();
                _sceneGame.mapManager.LoadMapByWarp(objectWarp);
            }
            // 드랍 아이템 일때
            else if (collision.gameObject.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.DropItem)))
            {
                _sceneGame.ItemManager.PlayerTaken(collision.gameObject);
            }
        }
        /// <summary>
        /// 장비 장착하기
        /// </summary>
        /// <param name="partIndex"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        public void EquipItem(int partIndex, int itemUid, int itemCount)
        {
            bool result = _equipController.EquipItem(partIndex, itemUid);
            if (!result) return;
            if (itemUid <= 0)
            {
                UnEquipItem(partIndex);
                return;
            }
            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(itemUid);
            if (info == null) return;
            
            ItemConstants.PartsType partsType = (ItemConstants.PartsType)partIndex;
            List<string> slotNames = ItemConstants.SlotNameByPartsType[partsType];

            List<StruckChangeSlotImage> changeImages = new List<StruckChangeSlotImage>();
            foreach (var slotName in slotNames)
            {
                string attachmentName = ItemConstants.AttachmentNameBySlotName[slotName];
                
                string changeSpritePath = $"{info.FileName}_{slotName}";
                var sprite = AddressableLoaderItem.Instance.GetImageEquipByName(changeSpritePath);
                
                StruckChangeSlotImage struckChangeSlotImage = new StruckChangeSlotImage(slotName, attachmentName, sprite);
                changeImages.Add(struckChangeSlotImage);
            }
            CharacterAnimationController.ChangeCharacterImageInSlot(changeImages);
        }
        /// <summary>
        /// 장비 해제 하기
        /// </summary>
        /// <param name="partIndex"></param>
        public void UnEquipItem(int partIndex)
        {
            bool result = _equipController.UnEquipItem(partIndex);
            if (!result) return;
            
            ItemConstants.PartsType partsType = (ItemConstants.PartsType)partIndex;
            List<string> slotNames = ItemConstants.SlotNameByPartsType[partsType];

            List<StruckChangeSlotImage> changeImages = new List<StruckChangeSlotImage>();
            foreach (var slotName in slotNames)
            {
                string attachmentName = ItemConstants.AttachmentNameBySlotName[slotName];
                
                string changeSpritePath = attachmentName;
                var sprite = AddressableLoaderItem.Instance.GetImageEquipByName(changeSpritePath);

                StruckChangeSlotImage struckChangeSlotImage = new StruckChangeSlotImage(slotName, attachmentName, sprite);
                changeImages.Add(struckChangeSlotImage);
            }
            CharacterAnimationController.ChangeCharacterImageInSlot(changeImages);
        }
        /// <summary>
        /// attack 이벤트 처리 
        /// </summary>
        public override void OnEventAttack()
        {
            if (IsStatusDead()) return;
            // GcLogger.Log(@event);
            long totalDamage = _sceneGame.calculateManager.GetPlayerTotalAtk();
        
            // 캡슐 콜라이더 2D와 충돌 중인 모든 콜라이더를 검색
            Vector2 size = new Vector2(colliderCheckCharacter.size.x * Mathf.Abs(transform.localScale.x), colliderCheckCharacter.size.y * transform.localScale.y);
            Vector2 point = (Vector2)transform.position + colliderCheckCharacter.offset * transform.localScale;
            
            int countDamageMonster = 0;
#if UNITY_6000_0_OR_NEWER
            int hitCount = Physics2D.OverlapCapsule(point, size, colliderCheckCharacter.direction, 0f,
                new ContactFilter2D().NoFilter(), _collider2Ds);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
#else
            Physics2D.OverlapCapsuleNonAlloc(point, size, colliderCheckCharacter.direction, 0f, _collider2Ds);
            foreach (var hit in _collider2Ds)
            {
#endif
                if (!hit || !hit.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster))) continue;
                CharacterAttackRange characterAttackRange = hit.GetComponent<CharacterAttackRange>();
                if (characterAttackRange == null) continue;
                // GcLogger.Log("Player attacked the monster after animation!");
                CharacterBase monster = characterAttackRange.target;
                // 몬스터와 마주보고 있으면 공격 
                if (AreFacingEachOther(monster.transform))
                {
                    monster.TakeDamage(totalDamage, gameObject);
                    ++countDamageMonster;
                }
                // 몬스터와 같은 곳을 바라보고 있으면,
                else if (IsFlipped() == monster.IsFlipped())
                {
                    // flip 일때는 
                    // monster.x >= player.x
                    if (IsFlipped() && monster.transform.position.x >= transform.position.x)
                    {
                        monster.TakeDamage(totalDamage, gameObject);
                        ++countDamageMonster;
                    }
                    // flip 이 아닐때는
                    // monster.x <= player.x
                    else if (IsFlipped() != true && monster.transform.position.x <= transform.position.x)
                    {
                        monster.TakeDamage(totalDamage, gameObject);
                        ++countDamageMonster; 
                    }
                }
                        
                // CountCollider 마리 한테만 데미지 준다 
                if (countDamageMonster > CountCollider)
                {
                    break;
                }
            }
        }
        private void SetWindowHudSliderHp(long value)
        {
            if (_uiWindowHud == null)
            {
                return;
            }
            _uiWindowHud.SetSliderHp(value, TotalHp.Value);
        }
        private void SetWindowHudSliderMp(long value)
        {
            if (_uiWindowHud == null) 
            {
                return;
            }
            _uiWindowHud.SetSliderMp(value, TotalMp.Value);
        }
        /// <summary>
        /// Player의 스탯과 UI를 매핑하여 리스트에 저장
        /// </summary>
        private void InitializeStatBindings()
        {
            _statBindings.AddRange(new[]
            {
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Atk, GetStat = p => p.TotalAtk, label = "ATK" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Def, GetStat = p => p.TotalDef, label = "DEF" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Hp, GetStat = p => p.TotalHp, label = "HP" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.Mp, GetStat = p => p.TotalMp, label = "MP" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.MoveSpeed, GetStat = p => p.TotalMoveSpeed, label = "Move Speed" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.AttackSpeed, GetStat = p => p.TotalAttackSpeed, label = "Attack Speed" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.CriticalDamage, GetStat = p => p.TotalCriticalDamage, label = "Critical DMG" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.CriticalProbability, GetStat = p => p.TotalCriticalProbability, label = "Critical Rate" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.RegistFire, GetStat = p => p.TotalRegistFire, label = "Regist Fire" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.RegistCold, GetStat = p => p.TotalRegistCold, label = "Regist Cold" },
                new StatUIBinding { textUI = UIWindowPlayerInfo.IndexPlayerInfo.RegistLightning, GetStat = p => p.TotalRegistLightning, label = "Regist Lighting" },
            });
            foreach (var binding in _statBindings)
            {
                binding.GetStat(this).DistinctUntilChanged()
                    .Subscribe(value => UpdatePlayerInfoText(binding.textUI, binding.label, value))
                    .AddTo(this);
            }
        }
        /// <summary>
        /// UIWindowPlayerInfo 에 text 업데이트 하기
        /// </summary>
        /// <param name="textUI"></param>
        /// <param name="label"></param>
        /// <param name="value"></param>
        private void UpdatePlayerInfoText(UIWindowPlayerInfo.IndexPlayerInfo textUI, string label, long value)
        {
            if (_uiWindowPlayerInfo == null) return;
            _uiWindowPlayerInfo.UpdateText(textUI, label, value);
        }
        /// <summary>
        /// 현재 생명력이 최대치인지
        /// </summary>
        /// <returns></returns>
        public bool IsMaxHp()
        {
            return CurrentHp.Value >= TotalHp.Value;
        }
        /// <summary>
        /// 현재 생명력 더하기
        /// </summary>
        /// <param name="value"></param>
        public void AddHp(int value)
        {
            long newVale = CurrentHp.Value + value;
            if (newVale > TotalHp.Value)
            {
                newVale = TotalHp.Value;
            }
            CurrentHp.OnNext(newVale);
        }
        /// <summary>
        /// 현재 마력이 최대치 인지
        /// </summary>
        /// <returns></returns>
        public bool IsMaxMp()
        {
            return CurrentMp.Value >= TotalMp.Value;
        }
        /// <summary>
        /// 현재 마력이 최대치 인지
        /// </summary>
        /// <returns></returns>
        public bool CheckNeedMp(int needMp)
        {
            return CurrentMp.Value >= needMp;
        }
        /// <summary>
        /// 플레이어 죽었을때 end 상태로 변경
        /// </summary>
        protected override void OnDead()
        {
            base.OnDead();
            _sceneGame.SetState(SceneGame.GameState.End);
        }
        
        /// <summary>
        /// 스킬 사용하기
        /// </summary>
        /// <param name="skillUid"></param>
        /// <param name="skillLevel"></param>
        public void UseSkill(int skillUid, int skillLevel)
        {
            SkillController.MakeSkill(skillUid, skillLevel);
        }

        public bool IsRequireLevel(int compareLevel)
        {
            bool result = _playerData?.CurrentLevel >= compareLevel;
            if (!result)
            {
                _sceneGame.systemMessageManager.ShowMessageWarning($"플레이어 레벨이 부족합니다. 필요 레벨 : {compareLevel}");
            }
            return result;
        }
        /// <summary>
        /// 어펙트 발동시 UIWindowPlayerBuffInfo 에 추가하기
        /// </summary>
        /// <param name="affectUid"></param>
        protected override void OnAffect(int affectUid)
        {
            if (_uiWindowPlayerBuffInfo == null) return;
            _uiWindowPlayerBuffInfo.AddAffectIcon(affectUid);
        }
        /// <summary>
        /// localScale 이 적용된 캐릭터 크기 가져오기
        /// </summary>
        /// <returns></returns>
        public override float GetHeightByScale()
        {
            return CharacterAnimationController.GetCharacterHeight() * Math.Abs(transform.localScale.x);
        }

        public void SetMapSize(Vector2 mapSize)
        {
            _controllerPlayer?.ChangeMapSize(mapSize);
        }
    }
}