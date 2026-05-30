using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대표 VFX UID를 실제 VFX 리소스 행으로 해석하는 런타임 서비스입니다.
    /// Direct/Variant 및 실제 리소스 테이블(vfx_effect/vfx_particle) 해석을 한곳에서 처리합니다.
    /// </summary>
    public sealed class VfxResolver
    {
        private readonly TableLoaderManager _tableLoaderManager;
        private readonly VfxVariantSelector _variantSelector = new VfxVariantSelector();

        /// <summary>
        /// 대표 VFX 해석기를 생성합니다.
        /// </summary>
        /// <param name="tableLoaderManager">VFX 관련 테이블을 보관하는 테이블 로더입니다.</param>
        public VfxResolver(TableLoaderManager tableLoaderManager)
        {
            _tableLoaderManager = tableLoaderManager;
        }

        /// <summary>
        /// 외부에서 사용하는 대표 VFX UID를 실제 생성 가능한 VFX 리소스 정보로 해석합니다.
        /// </summary>
        /// <param name="vfxUid">대표 VFX UID입니다.</param>
        /// <param name="resolved">최종 생성 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다. 무출력 후보도 성공 결과로 처리됩니다.</returns>
        public bool TryResolve(int vfxUid, out ResolvedVfx resolved)
        {
            resolved = default;
            if (_tableLoaderManager == null || vfxUid <= 0)
                return false;

            StruckTableVfx vfx = null;
            if (_tableLoaderManager.TableVfx != null)
                _tableLoaderManager.TableVfx.TryGetDataByUid(vfxUid, out vfx);

            // vfx 대표 테이블을 아직 만들지 않은 기존 프로젝트는 기존 방식으로 직접 리소스를 조회합니다.
            if (vfx == null)
                return TryResolveLegacy(vfxUid, out resolved);

            if (!vfx.Enabled)
            {
                resolved = ResolvedVfx.Silent(vfxUid, vfx, null);
                return true;
            }

            if (vfx.ResolveMode == VfxConstants.ResolveMode.Variant)
                return TryResolveVariant(vfx, out resolved);

            return TryResolveDirect(vfx, out resolved);
        }

        /// <summary>
        /// 대표 vfx 테이블이 없는 기존 데이터 구조를 기존처럼 직접 해석합니다.
        /// </summary>
        /// <param name="vfxUid">요청된 VFX UID입니다.</param>
        /// <param name="resolved">최종 생성 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다.</returns>
        private bool TryResolveLegacy(int vfxUid, out ResolvedVfx resolved)
        {
            resolved = default;
            if (!TryGetResourceByUid(vfxUid, VfxConstants.AssetKind.None, out VfxRuntimeData runtimeData))
                return false;

            resolved = new ResolvedVfx(vfxUid, runtimeData.Uid, runtimeData.AssetKind, runtimeData, null, null);
            return true;
        }

        /// <summary>
        /// 대표 VFX UID에 직접 연결된 실제 리소스 1개를 해석합니다.
        /// </summary>
        /// <param name="vfx">대표 VFX 행입니다.</param>
        /// <param name="resolved">최종 생성 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다.</returns>
        private bool TryResolveDirect(StruckTableVfx vfx, out ResolvedVfx resolved)
        {
            resolved = default;

            if (vfx.FallbackResourceUid > 0 && TryGetResourceByUid(vfx.FallbackResourceUid, vfx.AssetKind, out VfxRuntimeData fallback))
                return BuildResolved(vfx, fallback, null, out resolved);

            if (TryGetFirstResource(vfx, out VfxRuntimeData resource))
                return BuildResolved(vfx, resource, null, out resolved);

            if (TryGetResourceByUid(vfx.Uid, vfx.AssetKind, out VfxRuntimeData sameUidResource))
                return BuildResolved(vfx, sameUidResource, null, out resolved);

            GcLogger.LogWarning($"[VfxResolver] 직접 연결된 실제 VFX 리소스가 없습니다. vfxUid={vfx.Uid}, assetKind={vfx.AssetKind}");
            return false;
        }

        /// <summary>
        /// vfx_variant 후보 목록을 기준으로 실제 리소스를 선택합니다.
        /// </summary>
        /// <param name="vfx">대표 VFX 행입니다.</param>
        /// <param name="resolved">최종 생성 정보입니다.</param>
        /// <returns>해석에 성공하면 true를 반환합니다.</returns>
        private bool TryResolveVariant(StruckTableVfx vfx, out ResolvedVfx resolved)
        {
            resolved = default;
            IReadOnlyList<StruckTableVfxVariant> variants = _tableLoaderManager.TableVfxVariant != null
                ? _tableLoaderManager.TableVfxVariant.GetVariants(vfx.Uid)
                : System.Array.Empty<StruckTableVfxVariant>();

            if (!_variantSelector.TrySelect(vfx, variants, out StruckTableVfxVariant selected))
            {
                if (vfx.FallbackResourceUid > 0 && TryGetResourceByUid(vfx.FallbackResourceUid, vfx.AssetKind, out VfxRuntimeData fallback))
                    return BuildResolved(vfx, fallback, null, out resolved);

                // Variant 테이블이 아직 준비되지 않았더라도 직접 연결된 리소스가 있으면 기존 방식처럼 생성합니다.
                if (TryGetFirstResource(vfx, out VfxRuntimeData directResource))
                    return BuildResolved(vfx, directResource, null, out resolved);

                if (TryGetResourceByUid(vfx.Uid, vfx.AssetKind, out VfxRuntimeData sameUidResource))
                    return BuildResolved(vfx, sameUidResource, null, out resolved);

                GcLogger.LogWarning($"[VfxResolver] 선택 가능한 variant 후보가 없습니다. vfxUid={vfx.Uid}");
                return false;
            }

            if (selected.CandidateVfxResourceUid <= 0)
            {
                resolved = ResolvedVfx.Silent(vfx.Uid, vfx, selected);
                return true;
            }

            if (!TryGetResourceByUid(selected.CandidateVfxResourceUid, selected.CandidateAssetKind, out VfxRuntimeData resource))
            {
                GcLogger.LogWarning($"[VfxResolver] variant 후보 리소스를 찾지 못했습니다. vfxUid={vfx.Uid}, resourceUid={selected.CandidateVfxResourceUid}, assetKind={selected.CandidateAssetKind}");
                return false;
            }

            return BuildResolved(vfx, resource, selected, out resolved);
        }

        /// <summary>
        /// 대표 VFX UID에 연결된 첫 번째 실제 리소스 행을 찾습니다.
        /// </summary>
        /// <param name="vfx">대표 VFX 행입니다.</param>
        /// <param name="resource">찾은 실제 리소스 데이터입니다.</param>
        /// <returns>찾으면 true를 반환합니다.</returns>
        private bool TryGetFirstResource(StruckTableVfx vfx, out VfxRuntimeData resource)
        {
            resource = null;
            if (vfx == null)
                return false;

            if (vfx.AssetKind == VfxConstants.AssetKind.Effect || vfx.AssetKind == VfxConstants.AssetKind.None)
            {
                StruckTableVfxEffect effect = _tableLoaderManager.TableVfxEffect?.GetFirstByVfxUid(vfx.Uid);
                if (effect != null)
                {
                    resource = VfxRuntimeDataFactory.Create(effect);
                    return true;
                }
            }

            if (vfx.AssetKind == VfxConstants.AssetKind.Particle || vfx.AssetKind == VfxConstants.AssetKind.None)
            {
                StruckTableVfxParticle particle = _tableLoaderManager.TableVfxParticle?.GetFirstByVfxUid(vfx.Uid);
                if (particle != null)
                {
                    resource = VfxRuntimeDataFactory.Create(particle);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 실제 리소스 테이블에서 UID로 VFX 런타임 데이터를 찾습니다.
        /// </summary>
        /// <param name="resourceUid">실제 리소스 UID입니다.</param>
        /// <param name="assetKind">검색할 실제 리소스 타입입니다. None이면 Effect, Particle 순서로 검색합니다.</param>
        /// <param name="resource">찾은 VFX 런타임 데이터입니다.</param>
        /// <returns>찾으면 true를 반환합니다.</returns>
        private bool TryGetResourceByUid(int resourceUid, VfxConstants.AssetKind assetKind, out VfxRuntimeData resource)
        {
            resource = null;
            if (resourceUid <= 0)
                return false;

            if (assetKind == VfxConstants.AssetKind.Effect || assetKind == VfxConstants.AssetKind.None)
            {
                if (_tableLoaderManager.TableVfxEffect != null
                    && _tableLoaderManager.TableVfxEffect.TryGetDataByUid(resourceUid, out StruckTableVfxEffect effect))
                {
                    resource = VfxRuntimeDataFactory.Create(effect);
                    return true;
                }
            }

            if (assetKind == VfxConstants.AssetKind.Particle || assetKind == VfxConstants.AssetKind.None)
            {
                if (_tableLoaderManager.TableVfxParticle != null
                    && _tableLoaderManager.TableVfxParticle.TryGetDataByUid(resourceUid, out StruckTableVfxParticle particle))
                {
                    resource = VfxRuntimeDataFactory.Create(particle);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 실제 리소스 데이터와 variant 보정값을 결합해 최종 생성 정보를 생성합니다.
        /// </summary>
        /// <param name="vfx">대표 VFX 행입니다.</param>
        /// <param name="resource">실제 VFX 런타임 데이터입니다.</param>
        /// <param name="variant">선택된 variant 행입니다. Direct 재생이면 null입니다.</param>
        /// <param name="resolved">최종 생성 정보입니다.</param>
        /// <returns>생성에 성공하면 true를 반환합니다.</returns>
        private static bool BuildResolved(StruckTableVfx vfx, VfxRuntimeData resource, StruckTableVfxVariant variant, out ResolvedVfx resolved)
        {
            resolved = default;
            if (vfx == null || resource == null || string.IsNullOrWhiteSpace(resource.PrefabPath))
                return false;

            resolved = new ResolvedVfx(
                vfx.Uid,
                resource.Uid,
                resource.AssetKind,
                resource,
                vfx,
                variant,
                variant != null ? variant.ScaleOverride : 0f,
                variant != null ? variant.DurationOverride : 0f,
                variant != null ? variant.ColorOverride : string.Empty);
            return true;
        }
    }
}
