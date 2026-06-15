using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public class UIWindowBattleHudMonster : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("생명력 Slider")]
        public UISliderFlip uiSliderFlip;
        [Header("슈퍼아머")]
        [Tooltip("슈퍼 아머 아이콘이 들어갈 오브젝트")]
        public GameObject containerSuperArmor;
        [Tooltip("슈퍼 아머 아이콘 프리팹")]
        public GameObject prefabShield;

        public TMP_Text textCharacterName;
        public Image imageCharacterName;
        private List<GameObject> _shieldIcons;
        private readonly Dictionary<GameObject, Coroutine> _pendingShieldHideCoroutines =
            new Dictionary<GameObject, Coroutine>();
        private int _lastSuperArmorValue;
        private int _currentMonsterInstanceId;
        private AddressableLoaderCharacterImageName _addressableLoaderCharacterImageName;
        private Monster _boundMonster;
        private bool _boundShowSuperArmor;
        private IDisposable _currentHpSubscription;
        private IDisposable _maxHpSubscription;
        private IDisposable _currentSuperArmorSubscription;

        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowConstants.WindowUid.BattleHudMonster;
            base.Awake();
            _shieldIcons = new List<GameObject>();
            _lastSuperArmorValue = 0;
            _currentMonsterInstanceId = 0;
            _addressableLoaderCharacterImageName = AddressableLoaderCharacterImageName.Instance;
        }

        /// <summary>
        /// 전투 HUD에 표시할 몬스터 정보를 갱신합니다.
        /// </summary>
        /// <param name="monster">표시할 몬스터입니다.</param>
        /// <param name="showSuperArmor">Super Armor 아이콘 표시 여부입니다.</param>
        /// <remarks>
        /// HUD가 비활성 상태일 때 누락된 HP 변경 이벤트가 있을 수 있으므로,
        /// 정보 갱신 시점에 현재 HP/최대 HP를 함께 재동기화합니다.
        /// </remarks>
        public void UpdateInfo(Monster monster, bool showSuperArmor)
        {
            if (!monster) return;

            int monsterInstanceId = monster.GetInstanceID();
            if (_currentMonsterInstanceId != monsterInstanceId)
            {
                _currentMonsterInstanceId = monsterInstanceId;
                SetSuperArmorImmediate(0);
            }

            InitMonsterNameText(monster.uid);
            InitMonsterNameByImage(monster.uid);
            InitSuperArmor(monster, showSuperArmor);
            SetSliderHp(monster.CurrentHp.Value, monster.MaxHp.Value);
        }

        /// <summary>
        /// 전투 HUD를 지정한 몬스터에 바인딩하고 리소스 변경 이벤트를 구독합니다.
        /// </summary>
        /// <param name="monster">표시할 몬스터입니다.</param>
        /// <param name="showSuperArmor">슈퍼아머 UI 표시 여부입니다.</param>
        /// <remarks>
        /// 전역 전투 HUD는 한 번에 하나의 몬스터만 표시하므로 새 몬스터를 바인딩하기 전에 이전 구독을 정리합니다.
        /// </remarks>
        public void Bind(Monster monster, bool showSuperArmor)
        {
            Unbind();
            if (!monster)
            {
                return;
            }

            _boundMonster = monster;
            _boundShowSuperArmor = showSuperArmor;
            UpdateInfo(monster, showSuperArmor);

            _currentHpSubscription = monster.CurrentHp.Subscribe(OnBoundCurrentHpChanged);
            _maxHpSubscription = monster.MaxHp.Subscribe(OnBoundMaxHpChanged);
            _currentSuperArmorSubscription = monster.CurrentSuperArmor.Subscribe(OnBoundCurrentSuperArmorChanged);
        }

        /// <summary>
        /// 현재 전투 HUD에 바인딩된 몬스터와 리소스 변경 구독을 해제합니다.
        /// </summary>
        public void Unbind()
        {
            DisposeBindingSubscriptions();
            _boundMonster = null;
            _boundShowSuperArmor = false;
            _currentMonsterInstanceId = 0;
            ResetSuperArmorForHide();
        }

        /// <summary>
        /// 바인딩된 몬스터의 현재 HP 변경을 HUD에 반영합니다.
        /// </summary>
        /// <param name="value">변경된 현재 HP입니다.</param>
        private void OnBoundCurrentHpChanged(long value)
        {
            if (!_boundMonster) return;
            SetSliderHp(value, _boundMonster.MaxHp.Value);
        }

        /// <summary>
        /// 바인딩된 몬스터의 최대 HP 변경을 HUD에 반영합니다.
        /// </summary>
        /// <param name="value">변경된 최대 HP입니다.</param>
        private void OnBoundMaxHpChanged(long value)
        {
            if (!_boundMonster) return;
            SetSliderHp(_boundMonster.CurrentHp.Value, value);
        }

        /// <summary>
        /// 바인딩된 몬스터의 현재 슈퍼아머 변경을 HUD에 반영합니다.
        /// </summary>
        /// <param name="value">변경된 현재 슈퍼아머 값입니다.</param>
        private void OnBoundCurrentSuperArmorChanged(int value)
        {
            SetSuperArmor(_boundShowSuperArmor ? value : 0);
        }

        private void InitMonsterNameByImage(int monsterUid)
        {
            if (!imageCharacterName) return;

            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info == null) return;

            if (string.IsNullOrEmpty(info.ImageThumbnailFileName)) return;
            string key = $"{ConfigAddressableKey.CharacterImageNameMonster}_{info.ImageThumbnailFileName}";
            Sprite sprite = _addressableLoaderCharacterImageName.GetImageNameByKey(key);
            if (GcLogger.IsNull(sprite, $"[CharacterLoader] Failed to load character sprite. key={key}")) return;
            imageCharacterName.sprite = sprite;
            imageCharacterName.SetNativeSize();
        }

        private void InitMonsterNameText(int monsterUid)
        {
            var info = TableLoaderManager.Instance.GetMonsterData(monsterUid);
            if (info == null) return;
            if (!textCharacterName) return;
            textCharacterName.text = info.Name;
        }

        /// <summary>
        /// Battle HUD에 표시할 Super Armor 아이콘을 초기화합니다.
        /// </summary>
        /// <param name="monster">표시할 몬스터입니다.</param>
        /// <param name="showSuperArmor">Super Armor 표시 여부입니다.</param>
        private void InitSuperArmor(Monster monster, bool showSuperArmor)
        {
            if (containerSuperArmor != null)
            {
                containerSuperArmor.SetActive(showSuperArmor);
            }

            if (!showSuperArmor)
            {
                SetSuperArmorImmediate(0);
                return;
            }

            int maxSuperArmor = Mathf.Max(monster.TotalSuperArmor.Value, monster.CurrentSuperArmor.Value);
            if (maxSuperArmor <= 0)
            {
                SetSuperArmorImmediate(0);
                return;
            }

            if (GcLogger.IsNull(containerSuperArmor,
                    $"슈퍼아머 아이콘을 배치할 컨테이너가 없습니다. containerSuperArmor : {containerSuperArmor}"))
                return;
            if (GcLogger.IsNull(prefabShield, $"슈퍼아머에 사용할 아이콘 프리팹이 없습니다. prefabShield : {prefabShield}"))
                return;

            EnsureShieldIconCount(maxSuperArmor);
            SetSuperArmorImmediate(monster.CurrentSuperArmor.Value);
        }

        /// <summary>
        /// 필요한 Super Armor 아이콘 개수만큼 풀을 확장합니다.
        /// </summary>
        /// <param name="count">필요한 아이콘 개수입니다.</param>
        private void EnsureShieldIconCount(int count)
        {
            if (count <= _shieldIcons.Count) return;

            for (int i = _shieldIcons.Count; i < count; i++)
            {
                var shield = Instantiate(prefabShield, containerSuperArmor.transform);
                _shieldIcons.Add(shield);
            }
        }

        /// <summary>
        /// Battle HUD HP 슬라이더 값을 갱신합니다.
        /// </summary>
        /// <param name="currentValue">현재 HP 값입니다.</param>
        /// <param name="totalValue">최대 HP 값입니다.</param>
        public void SetSliderHp(long currentValue, long totalValue)
        {
            if (uiSliderFlip == null) return;
            uiSliderFlip.SetValue(currentValue, totalValue);
        }

        /// <summary>
        /// Battle HUD Super Armor 아이콘 활성 상태를 현재 값에 맞춰 갱신합니다.
        /// 감소한 아이콘은 VfxEffectUI가 있으면 1회 재생 후 비활성화합니다.
        /// </summary>
        /// <param name="value">현재 Super Armor 값입니다.</param>
        public void SetSuperArmor(int value)
        {
            int clampedValue = Mathf.Max(0, value);
            if (_shieldIcons == null || _shieldIcons.Count == 0)
            {
                _lastSuperArmorValue = clampedValue;
                return;
            }

            bool canPlayBreakAnimation = CanPlayShieldBreakAnimation();

            int index = 0;
            foreach (var shieldIcon in _shieldIcons)
            {
                if (shieldIcon == null)
                {
                    index++;
                    continue;
                }

                bool shouldBeVisible = index < clampedValue;
                if (shouldBeVisible)
                {
                    CancelPendingShieldHide(shieldIcon);
                    shieldIcon.SetActive(true);
                }
                else
                {
                    bool consumedThisTick = index < _lastSuperArmorValue;
                    if (consumedThisTick)
                    {
                        if (canPlayBreakAnimation)
                        {
                            PlayBreakAndHideShieldIcon(shieldIcon);
                        }
                        else
                        {
                            CancelPendingShieldHide(shieldIcon);
                            shieldIcon.SetActive(false);
                        }
                    }
                    else if (!_pendingShieldHideCoroutines.ContainsKey(shieldIcon))
                    {
                        shieldIcon.SetActive(false);
                    }
                }

                index++;
            }

            _lastSuperArmorValue = clampedValue;
        }

        /// <summary>
        /// Battle HUD를 숨기기 전에 Super Armor 아이콘을 연출 없이 즉시 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 윈도우 비활성화 이후에는 코루틴/VFX 연출 시작이 불가능하므로
        /// 숨김 분기에서는 즉시 동기화 경로로 상태를 정리합니다.
        /// </remarks>
        public void ResetSuperArmorForHide()
        {
            SetSuperArmorImmediate(0);
        }

        /// <summary>
        /// 슈퍼아머 아이콘 상태를 즉시 동기화합니다.
        /// 초기화/몬스터 교체/표시 해제 시에는 연출 없이 즉시 반영합니다.
        /// </summary>
        /// <param name="value">표시할 현재 Super Armor 값입니다.</param>
        private void SetSuperArmorImmediate(int value)
        {
            int clampedValue = Mathf.Max(0, value);
            CancelAllPendingShieldHide();

            if (_shieldIcons != null)
            {
                int index = 0;
                foreach (var shieldIcon in _shieldIcons)
                {
                    if (shieldIcon != null)
                    {
                        shieldIcon.SetActive(index < clampedValue);
                    }

                    index++;
                }
            }

            _lastSuperArmorValue = clampedValue;
        }

        /// <summary>
        /// 슈퍼아머 아이콘 파괴 연출을 재생한 뒤 아이콘을 비활성화합니다.
        /// prefabShield에 VfxEffectUI가 없으면 즉시 비활성화합니다.
        /// </summary>
        /// <param name="shieldIcon">연출 대상 아이콘입니다.</param>
        private void PlayBreakAndHideShieldIcon(GameObject shieldIcon)
        {
            if (shieldIcon == null)
            {
                return;
            }
            if (!CanPlayShieldBreakAnimation())
            {
                CancelPendingShieldHide(shieldIcon);
                shieldIcon.SetActive(false);
                return;
            }

            CancelPendingShieldHide(shieldIcon);
            shieldIcon.SetActive(true);
            if (!shieldIcon.activeInHierarchy)
            {
                shieldIcon.SetActive(false);
                return;
            }

            var vfxEffect = shieldIcon.GetComponent<VfxEffectUI>();
            if (vfxEffect == null)
            {
                shieldIcon.SetActive(false);
                return;
            }

            float duration = vfxEffect.PlayOneShotEffect(forceReset: true);
            if (duration <= 0f)
            {
                shieldIcon.SetActive(false);
                return;
            }
            if (!CanPlayShieldBreakAnimation())
            {
                shieldIcon.SetActive(false);
                return;
            }

            Coroutine coroutine = StartCoroutine(HideShieldIconAfterDelay(shieldIcon, duration));
            _pendingShieldHideCoroutines[shieldIcon] = coroutine;
        }

        /// <summary>
        /// 현재 상태에서 Super Armor 파괴 연출을 안전하게 시작할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>
        /// HUD 오브젝트와 컨테이너가 활성 상태이며 코루틴 실행이 가능하면 true를 반환합니다.
        /// </returns>
        private bool CanPlayShieldBreakAnimation()
        {
            if (!isActiveAndEnabled) return false;
            if (!gameObject.activeInHierarchy) return false;
            if (containerSuperArmor != null && !containerSuperArmor.activeInHierarchy) return false;
            return true;
        }

        /// <summary>
        /// 지정한 시간 대기 후 슈퍼아머 아이콘을 비활성화합니다.
        /// </summary>
        /// <param name="shieldIcon">비활성화할 아이콘입니다.</param>
        /// <param name="delay">대기 시간(초)입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator HideShieldIconAfterDelay(GameObject shieldIcon, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (shieldIcon != null)
            {
                shieldIcon.SetActive(false);
            }

            _pendingShieldHideCoroutines.Remove(shieldIcon);
        }

        /// <summary>
        /// 특정 아이콘에 예약된 숨김 코루틴을 취소합니다.
        /// </summary>
        /// <param name="shieldIcon">취소 대상 아이콘입니다.</param>
        private void CancelPendingShieldHide(GameObject shieldIcon)
        {
            if (shieldIcon == null) return;
            if (!_pendingShieldHideCoroutines.TryGetValue(shieldIcon, out var coroutine)) return;

            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            _pendingShieldHideCoroutines.Remove(shieldIcon);
        }

        /// <summary>
        /// 예약된 모든 숨김 코루틴을 취소합니다.
        /// </summary>
        private void CancelAllPendingShieldHide()
        {
            foreach (var pair in _pendingShieldHideCoroutines)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }

            _pendingShieldHideCoroutines.Clear();
        }

        /// <summary>
        /// 바인딩된 몬스터 리소스 구독을 모두 해제합니다.
        /// </summary>
        private void DisposeBindingSubscriptions()
        {
            _currentHpSubscription?.Dispose();
            _maxHpSubscription?.Dispose();
            _currentSuperArmorSubscription?.Dispose();
            _currentHpSubscription = null;
            _maxHpSubscription = null;
            _currentSuperArmorSubscription = null;
        }

        private void OnDisable()
        {
            CancelAllPendingShieldHide();
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
