using System.Collections;
using GGemCo2DAffect;

namespace GGemCo2DCore
{
    /// <summary>
    /// 테이블 로드 이후, com.ggemco.2d.affect 런타임 저장소를 Core 테이블로부터 초기화한다.
    /// - 스킬 시스템(별도 패키지)에서 AffectComponent를 사용하려면, 반드시 한 번은 실행되어야 한다.
    /// </summary>
    public sealed class AffectRuntimeBootstrapStep : GameLoadStepBase
    {
        public AffectRuntimeBootstrapStep(string id, int order, string localizedKey)
            : base(id, order, localizedKey)
        {
        }

        public override IEnumerator Run()
        {
            progress = 0f;

            var mgr = TableLoaderManager.Instance;
            if (mgr == null)
            {
                progress = 1f;
                GcLogger.LogError($"{nameof(TableLoaderManager)} 싱글톤이 없습니다.");
                yield break;
            }

            var affectRepo = new InMemoryAffectRepository();
            var statusRepo = new InMemoryStatusRepository();

            // Status: Stat/DamageType/State
            foreach (var kv in mgr.TableStat.GetAll())
                statusRepo.RegisterStat(kv.Value.ID);
            foreach (var kv in mgr.TableDamageType.GetAll())
                statusRepo.RegisterDamageType(kv.Value.ID);
            foreach (var kv in mgr.TableState.GetAll())
                statusRepo.RegisterState(kv.Value.ID);

            // Affect + Modifiers
            foreach (var kv in mgr.TableAffect.GetAll())
            {
                var row = kv.Value;

                // Core 테이블 Row → AffectDefinition
                var def = new AffectDefinition
                {
                    Uid = row.Uid,
                    IconKey = row.IconKey,
                    GroupId = row.GroupId,
                    BaseDuration = row.BaseDuration,
                    TickInterval = row.TickInterval,
                    StackPolicy = row.StackPolicy,
                    MaxStacks = row.MaxStacks,
                    RefreshPolicy = row.RefreshPolicy,
                    DispelType = row.DispelType,
                    VfxUid = row.VfxUid,
                    VfxScale = row.VfxScale,
                    VfxOffsetY = row.VfxOffsetY,
                    ApplyChance = row.ApplyChance
                };

                // Tags
                def.Tags.Clear();
                if (!string.IsNullOrWhiteSpace(row.Tags))
                {
                    var parts = row.Tags.Split(',', ' ');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var t = parts[i]?.Trim();
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        def.Tags.Add(t);
                    }
                }

                var mods = mgr.TableAffectModifier.GetModifiers(row.Uid);
                affectRepo.Register(def, new System.Collections.Generic.List<AffectModifierDefinition>(mods));
            }

            AffectRuntime.AffectRepository = affectRepo;
            AffectRuntime.StatusRepository = statusRepo;

            progress = 1f;
            yield break;
        }
    }
}
