using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 캐릭터에서 사용하는 어펙트 컨트롤러
    /// CharacterBase.cs 에서 생성된다.
    /// </summary>
    public class AffectController
    {
        private readonly CharacterBase character;
        private readonly Dictionary<int, List<ConfigCommon.StruckStatus>> activeBuffs = new();
        private readonly TableAffect tableAffect;
        private readonly TableEffect tableEffect;
        private readonly Dictionary<int, DefaultEffect> defaultEffects = new();
        private Coroutine removeCoroutine;
            
        public AffectController(CharacterBase characterBase)
        {
            if (TableLoaderManager.Instance == null) return;
            character = characterBase;
            tableAffect = TableLoaderManager.Instance.TableAffect;
            tableEffect = TableLoaderManager.Instance.TableEffect;
        }
        /// <summary>
        /// 어펙트 적용하기
        /// </summary>
        /// <param name="affectUid"></param>
        public void ApplyAffect(int affectUid)
        {
            var info = tableAffect.GetDataByUid(affectUid);
            if (info == null)
            {
                GcLogger.LogError("affect 테이블에 없는 어펙트 입니다. affect Uid: "+affectUid);
                return;
            }
            float duration = info.Duration;
            string statusId = info.StatusID;
            ConfigCommon.SuffixType suffixType = info.StatusSuffix;
            int value = info.Value;
            List<ConfigCommon.StruckStatus> buffs = new List<ConfigCommon.StruckStatus>
                { new (statusId, suffixType, value) };
            
            // 적용되어 있는 버프면, duration 초기화 하기
            if (activeBuffs.ContainsKey(info.Uid))
            {
                RemoveAffect(affectUid);
                if (removeCoroutine != null)
                {
                    character.StopCoroutine(removeCoroutine);
                }
            }
            
            // GcLogger.Log($"ApplyBuff {buff.Uid}/{buff.Name}/{buff.Duration}");
            activeBuffs.TryAdd(info.Uid, buffs);
            character.ApplyStatModifiers(buffs);
            character.RecalculateStats();
            removeCoroutine = character.StartCoroutine(RemoveBuffAfterDuration(info.Uid, duration));
            if (info.EffectUid > 0)
            {
                DefaultEffect defaultEffect = EffectManager.CreateEffect(info.EffectUid);
                if (defaultEffect == null) return;
                // SetParent 보다 먼저 scale 을 바꿔야 한다.
                if (info.EffectScale > 0)
                {
                    defaultEffect.SetScale(info.EffectScale);
                }
                // 캐릭터 하위에 붙이기
                defaultEffect.transform.SetParent(character.transform);
                // 캐릭터 height 만큼 위치 조정
                defaultEffect.transform.localPosition = new Vector3(0, character.height, 0);

                defaultEffects.TryAdd(info.Uid, defaultEffect);
            }
        }

        private IEnumerator RemoveBuffAfterDuration(int affectUid, float duration)
        {
            yield return new WaitForSeconds(duration);
            // GcLogger.Log($"RemoveBuffAfterDuration {buff.Uid}/{buff.Name}/{buff.Duration}");
            RemoveAffect(affectUid);
        }

        private void RemoveAffect(int affectUid)
        {
            if (activeBuffs.Count > 0)
            {
                // 캐릭터에 적용되어 있던 어펙트를 먼저 지워준다.
                character.RemoveStatModifiers(activeBuffs[affectUid]);
                character.RecalculateStats();
                activeBuffs[affectUid].Clear();
                activeBuffs.Remove(affectUid);
            }

            if (defaultEffects.Count > 0)
            {
                if (defaultEffects.Remove(affectUid, out var effect))
                {
                    if (effect == null) return;
                    Object.Destroy(effect.gameObject);
                }
            }
        }
        /// <summary>
        /// 캐릭터가 죽으면 모든 어펙트 지워주기
        /// </summary>
        public void RemoveAllAffects()
        {
            if (removeCoroutine != null)
            {
                character?.StopCoroutine(removeCoroutine);
            }
            // 모든 버프, 디버프 지우고 stat 갱신
            foreach (var info in activeBuffs)
            {
                character?.RemoveStatModifiers(activeBuffs[info.Key]);
            }
            character?.RecalculateStats();

            // 생성된 이펙트 지우기
            foreach (var info in defaultEffects)
            {
                info.Value.DestroyForce();
            }
            activeBuffs.Clear();
            defaultEffects.Clear();
        }
    }
}