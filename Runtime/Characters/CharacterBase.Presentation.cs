using System;
using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 방향, 렌더링, 페이드, 치수 표현을 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        /// <summary>
        /// 현재 스프라이트가 기본 방향 대비 뒤집힌 상태인지 확인합니다.
        /// </summary>
        /// <returns>기본 방향 기준으로 좌우가 반전되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsFlipped()
        {
            switch (defaultFacingDirection8)
            {
                case CharacterConstants.FacingDirection8.Left:
                    return _currentFacing == CharacterConstants.FacingDirection8.Right;
                case CharacterConstants.FacingDirection8.Right:
                    return _currentFacing == CharacterConstants.FacingDirection8.Left;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 좌우 방향 전환 가능 여부를 설정합니다.
        /// </summary>
        /// <param name="set">방향 전환 허용 여부입니다.</param>
        public void SetIsPossibleFlip(bool set) => _isPossibleFlip = set;

        /// <summary>
        /// 현재 인스턴스가 방향 전환을 허용하는지 확인합니다.
        /// </summary>
        /// <returns>좌우 전환이 가능하면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsPossibleFlip() => _isPossibleFlip;

        /// <summary>
        /// 기본 방향을 기준으로 좌우 반전 상태를 적용합니다.
        /// </summary>
        /// <param name="value">적용할 반전 여부입니다.</param>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 방향 값이 들어오면 발생할 수 있습니다.</exception>
        public void SetFlip(bool value)
        {
            if (IsPossibleFlip() != true) return;
            isFlip = value;
            switch (defaultFacingDirection8)
            {
                case CharacterConstants.FacingDirection8.Left:
                    SetFacing(value ? CharacterConstants.FacingDirection8.Right : CharacterConstants.FacingDirection8.Left);
                    break;
                case CharacterConstants.FacingDirection8.Right:
                    SetFacing(value ? CharacterConstants.FacingDirection8.Left : CharacterConstants.FacingDirection8.Right);
                    break;
                case CharacterConstants.FacingDirection8.None:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.Up:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
                case CharacterConstants.FacingDirection8.Down:
                case CharacterConstants.FacingDirection8.DownRight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 방향 벡터를 8방향 열거형으로 변환해 적용합니다.
        /// </summary>
        /// <param name="dir">적용할 방향 벡터입니다.</param>
        public void SetFacing(Vector2 dir)
        {
            SetFacing(CharacterConstants.ToFacingDirection8(dir));
        }

        /// <summary>
        /// 지정한 8방향 값을 현재 바라보는 방향으로 적용합니다.
        /// </summary>
        /// <param name="dir">적용할 8방향 값입니다.</param>
        public void SetFacing(CharacterConstants.FacingDirection8 dir)
        {
            if (IsPossibleFlip() != true || dir == CharacterConstants.FacingDirection8.None) return;
            _currentFacing = dir;

            float sign = 1;
            if ((defaultFacingDirection8 == CharacterConstants.FacingDirection8.Right &&
                 dir is CharacterConstants.FacingDirection8.Left or CharacterConstants.FacingDirection8.DownLeft
                     or CharacterConstants.FacingDirection8.UpLeft) ||
                (defaultFacingDirection8 == CharacterConstants.FacingDirection8.Left &&
                 dir is CharacterConstants.FacingDirection8.Right or CharacterConstants.FacingDirection8.DownRight
                     or CharacterConstants.FacingDirection8.UpRight))
            {
                sign = -1;
            }

            transform.localScale = new Vector3(originalScaleX * sign, transform.localScale.y, transform.localScale.z);
        }

        /// <summary>
        /// 대상 위치를 기준으로 캐릭터의 좌우 방향을 갱신합니다.
        /// </summary>
        /// <param name="targetTransform">바라볼 대상 Transform입니다.</param>
        protected void SetFlipToTarget(Transform targetTransform)
        {
            SetFlip(transform.position.x <= targetTransform.position.x);
        }

        /// <summary>
        /// 실제 렌더링 기준의 좌우 방향 부호를 계산합니다.
        /// </summary>
        /// <returns>오른쪽이면 양수, 왼쪽이면 음수를 반환합니다.</returns>
        private float GetFacingDirection()
        {
            float sign = Mathf.Sign(transform.localScale.x);
            return defaultFacingDirection8 == CharacterConstants.FacingDirection8.Right ? sign : -sign;
        }

        /// <summary>
        /// 두 캐릭터가 서로 마주보고 있는지 확인합니다.
        /// </summary>
        /// <param name="target">비교할 대상 캐릭터입니다.</param>
        /// <returns>서로를 향해 바라보고 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool AreFacingEachOther(CharacterBase target)
        {
            float attackerDir = GetFacingDirection();
            float targetDir = target.GetFacingDirection();
            float directionToMonster = Mathf.Sign(target.transform.position.x - transform.position.x);

            return Mathf.Approximately(attackerDir, directionToMonster) &&
                   Mathf.Approximately(targetDir, -directionToMonster);
        }

        /// <summary>
        /// 현재 월드 위치를 기준으로 렌더링 정렬 순서를 갱신합니다.
        /// </summary>
        private void UpdatePosition()
        {
            if (_sortingOrder == CharacterConstants.CharacterSortingOrder.Fixed) return;

            int baseSortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            baseSortingOrder = _sortingOrder switch
            {
                CharacterConstants.CharacterSortingOrder.AlwaysOnTop => CharacterConstants.SortingOrderTop,
                CharacterConstants.CharacterSortingOrder.AlwaysOnBottom => CharacterConstants.SortingOrderBottom,
                _ => baseSortingOrder
            };

            _characterRenderer.sortingOrder = baseSortingOrder;
        }

        /// <summary>
        /// 표현 갱신과 하단 경계 체크를 처리합니다.
        /// </summary>
        protected virtual void Update()
        {
            if (IsStatusDead()) return;
            if (CheckEndGround()) return;

            UpdatePosition();
        }

        /// <summary>
        /// 하단 경계 이탈 여부를 확인하고 필요 시 즉시 사망 처리합니다.
        /// 맵 이동 중에는 이전 맵 제거와 새 스폰 좌표 적용 사이에 캐릭터가 임시로 경계 밖에 있을 수 있으므로,
        /// <see cref="SetEndTilemapYDeathSuppressed"/>로 보호 중이면 사망 처리를 건너뜁니다.
        /// </summary>
        /// <returns>경계 이탈로 사망 처리를 수행했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool CheckEndGround()
        {
            if (_suppressEndTilemapYDeath) return false;
            if (transform.position.y > 0) return false;
            if (_limitBoundaryBottom) return false;

            GcLogger.LogWarning($"Dead by EndTilemapY. {name}");
            Dead(CharacterConstants.DieReasonType.EndTilemapY);
            return true;
        }

        /// <summary>
        /// 캐릭터의 로컬 스케일을 일괄 설정합니다.
        /// </summary>
        /// <param name="scale">적용할 균일 스케일 값입니다.</param>
        public void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 0);
            originalScaleX = scale;
        }

        /// <summary>
        /// 페이드 인/아웃 연출이 현재 진행 중인지 반환합니다.
        /// </summary>
        /// <remarks>
        /// 컬링 영역 재진입 시 BT 시작 타이밍을 제어하기 위한 읽기 전용 상태 값입니다.
        /// </remarks>
        public bool IsFading => _isStartFade;

        /// <summary>
        /// 페이드 인을 시작하고 시작 훅을 호출합니다.
        /// </summary>
        public void StartFadeIn()
        {
            if (_isStartFade || gameObject.activeSelf) return;
            _isStartFade = true;
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
            OnStartFadeIn();
        }

        /// <summary>
        /// 페이드 인 시작 시 파생 클래스가 추가 연출을 구현할 수 있는 훅입니다.
        /// </summary>
        protected virtual void OnStartFadeIn()
        {
        }

        /// <summary>
        /// 페이드 아웃을 시작하고 시작 훅을 호출합니다.
        /// </summary>
        public void StartFadeOut()
        {
            if (_isStartFade || !gameObject.activeSelf) return;
            _isStartFade = true;
            StartCoroutine(FadeOut(ConfigCommon.CharacterFadeSec));
            OnStartFadeOut();
        }

        /// <summary>
        /// 페이드 아웃 시작 시 파생 클래스가 추가 연출을 구현할 수 있는 훅입니다.
        /// </summary>
        protected virtual void OnStartFadeOut()
        {
        }

        /// <summary>
        /// 캐릭터 표현을 점진적으로 표시합니다.
        /// </summary>
        /// <param name="duration">페이드에 사용할 지속 시간입니다.</param>
        /// <returns>페이드 인이 완료될 때까지 진행되는 코루틴입니다.</returns>
        private IEnumerator FadeIn(float duration)
        {
            yield return CharacterAnimationController.FadeEffect(duration, true);
        }

        /// <summary>
        /// 캐릭터 표현을 점진적으로 숨기고 완료 후 비활성화합니다.
        /// </summary>
        /// <param name="duration">페이드에 사용할 지속 시간입니다.</param>
        /// <returns>페이드 아웃이 완료될 때까지 진행되는 코루틴입니다.</returns>
        private IEnumerator FadeOut(float duration)
        {
            yield return CharacterAnimationController.FadeEffect(duration, false);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 페이드 진행 상태 플래그를 직접 설정합니다.
        /// </summary>
        /// <param name="value">적용할 페이드 진행 여부입니다.</param>
        public void SetIsStartFade(bool value)
        {
            _isStartFade = value;
        }

        /// <summary>
        /// 캐릭터 기본 높이를 반환합니다.
        /// </summary>
        /// <returns>캐릭터 기준 높이 값입니다.</returns>
        public float GetHeight()
        {
            return _characterHeight;
        }

        /// <summary>
        /// 캐릭터 기본 높이를 설정합니다.
        /// </summary>
        /// <param name="value">적용할 높이 값입니다.</param>
        protected void SetHeight(float value)
        {
            _characterHeight = value;
        }

        /// <summary>
        /// 현재 스케일까지 반영된 높이를 계산해 반환합니다.
        /// </summary>
        /// <returns>스케일이 반영된 실제 높이 값입니다.</returns>
        public float GetHeightByScale()
        {
            return GetHeight() * Math.Abs(transform.localScale.x);
        }

        /// <summary>
        /// 캐릭터 기본 너비를 반환합니다.
        /// </summary>
        /// <returns>캐릭터 기준 너비 값입니다.</returns>
        public float GetWidth()
        {
            return _characterWidth;
        }

        /// <summary>
        /// 캐릭터 기본 너비를 설정합니다.
        /// </summary>
        /// <param name="value">적용할 너비 값입니다.</param>
        protected void SetWidth(float value)
        {
            _characterWidth = value;
        }

        /// <summary>
        /// 서브 상태를 지정한 값으로 교체합니다.
        /// </summary>
        /// <param name="value">적용할 서브 상태 플래그입니다.</param>
        public void SetSubStatus(CharacterConstants.CharacterSubStatus value)
        {
            _currentSubStatus = _currentSubStatus.ClearFlags();
            _currentSubStatus = value;
        }

        /// <summary>
        /// 현재 서브 상태에 플래그를 추가합니다.
        /// </summary>
        /// <param name="value">추가할 서브 상태 플래그입니다.</param>
        public void AddSubStatus(CharacterConstants.CharacterSubStatus value)
        {
            _currentSubStatus = _currentSubStatus.AddFlag(value);
        }

        /// <summary>
        /// 현재 서브 상태에서 지정한 플래그를 제거합니다.
        /// </summary>
        /// <param name="value">제거할 서브 상태 플래그입니다.</param>
        public void RemoveSubStatus(CharacterConstants.CharacterSubStatus value)
        {
            _currentSubStatus = _currentSubStatus.RemoveFlag(value);
        }

        /// <summary>
        /// 현재 서브 상태 플래그를 모두 제거합니다.
        /// </summary>
        public void ClearSubStatus()
        {
            _currentSubStatus = _currentSubStatus.ClearFlags();
        }

        /// <summary>
        /// 줍기(Pick Up) 표현 변경이 필요할 때 파생 클래스가 구현하는 확장 지점입니다.
        /// </summary>
        public virtual void ChangePickUpSprite()
        {
        }
    }
}
