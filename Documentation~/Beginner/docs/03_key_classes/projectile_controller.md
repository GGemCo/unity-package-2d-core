# ProjectileController

## 1. 문서 목적

이 문서는 `ProjectileController`를 설명합니다.
캐릭터 기준으로 발사체를 생성하고, 타겟/좌표/버스트 발사를 어떻게 제어하는지 이해하는 것이 목적입니다.

---

## 2. 역할

`ProjectileController`는 **캐릭터 단위 발사체 생성 허브**입니다.
캐릭터가 발사체를 사용할 때 필요한 최소 문맥을 들고 있으며, 실제 발사체 생성은 `ProjectileManager`에 위임합니다.

핵심 책임은 다음과 같습니다.

- 캐릭터 기준 초기화
- 메타데이터 기반 Projectile 정보 조회
- 타겟 설정 반영
- 단발/다발 발사 처리
- 좌표 기반 launch와 타겟 기반 launch 분기
- 발사 간 딜레이 처리

즉, 이 클래스는 “캐릭터가 Projectile 시스템을 호출하는 입구”라고 보면 됩니다.

---

## 3. 왜 중요한가

스킬 시스템이나 테스트 툴 입장에서 Projectile은 캐릭터 문맥이 필요합니다.

- 누가 쐈는가
- 누구를 노리는가
- 좌표 직접 지정인가
- 다발 발사인가
- 발사 간격은 얼마인가

이 정보를 상위 시스템마다 중복 구현하지 않기 위해 `ProjectileController`가 필요합니다.

---

## 4. 핵심 상태

### 캐릭터 문맥
- `_character`

발사체 생성자의 소유자입니다.
Projectile 메타데이터가 캐릭터 기반 정보를 필요로 할 때 중요합니다.

### 매니저
- `_projectileManager`

실제 발사체 생성과 런타임 관리 책임은 여기에 있습니다.

### 타겟
- `_target`

Fixed 타겟형 발사체나 좌표 보정 시 사용됩니다.

---

## 5. 주요 진입 메서드

### `Initialize(CharacterBase characterBase)`
발사체 컨트롤러를 캐릭터에 연결합니다.

### `Launch(MetadataProjectile metadataProjectile)`
대표 진입점입니다.

이 메서드는 대략 다음을 수행합니다.

1. 메타데이터에서 Projectile UID 확인
2. `TableLoaderManager.Instance.TableProjectile`로 Projectile 정보 조회
3. 타겟/공격 ID 같은 메타를 보정
4. 실제 버스트 생성 코루틴 시작

### `CreateProjectileBurst(StruckTableProjectile info, MetadataProjectile meta)`
실제 발사 루프를 처리합니다.

중요한 분기:
- `TargetType.Fixed`이고 타겟이 없으면 중단
- Count만큼 반복 생성
- Fixed 타겟이면 `proj.Launch(_target)`
- Area/None이면 좌표 기반 `proj.Launch(Vector2)`
- `UseTargetPositionOverride`가 있으면 그 좌표를 우선 사용
- `SecDelayByOne`이 있으면 발사 간 지연

---

## 6. 연결해서 봐야 하는 클래스

### 데이터
- `TableLoaderManager`
- `TableProjectile`
- `StruckTableProjectile`
- `MetadataProjectile`

### 런타임 발사체
- `ProjectileManager`
- `ProjectileBase`
- `ProjectileConstants`

### 캐릭터 기반
- `CharacterBase`
- 타겟 캐릭터의 `GetRandomPositionYInHitArea()`

---

## 7. 대표 런타임 흐름

### 흐름 A: 타겟 고정형 발사
1. 캐릭터가 Projectile 실행을 요청합니다.
2. `ProjectileController`가 메타데이터를 받습니다.
3. 타겟이 존재하는지 확인합니다.
4. `ProjectileManager`가 발사체를 생성합니다.
5. `proj.Launch(_target)`으로 발사합니다.

### 흐름 B: 좌표 기반 발사
1. 메타데이터에 좌표 오버라이드가 있으면 해당 좌표를 씁니다.
2. 없으면 캐릭터/타겟 기준 좌표를 계산합니다.
3. 발사체를 좌표로 launch 합니다.

### 흐름 C: 다발 발사
1. `info.Count`만큼 반복합니다.
2. 각 발사마다 새 Projectile을 생성합니다.
3. `SecDelayByOne`이 있으면 코루틴으로 간격을 둡니다.

---

## 8. 확장 포인트

### 발사 패턴을 늘릴 때
`ProjectileController`에 복잡한 패턴 분기를 모두 넣기보다, `MetadataProjectile`나 `ProjectileManager` 쪽 패턴 객체로 분리하는 편이 장기적으로 낫습니다.

### 타겟 좌표 샘플링 규칙을 바꿀 때
현재는 arc 여부, target position range, hit area 랜덤 Y 같은 규칙이 섞여 있으므로, 좌표 해석 유틸리티로 추출할 여지가 큽니다.

### 스킬 연동 확장 시
이 클래스는 “요청 입구”에 집중하고, 실제 데미지/판정/충돌은 Projectile 런타임 클래스에 맡기는 경계를 유지하는 편이 좋습니다.

---

## 9. 디버깅 체크리스트

### 발사체가 안 나가는 경우
- `ProjectileUid`가 0인지 확인합니다.
- 테이블에서 해당 UID를 찾을 수 있는지 확인합니다.
- `TargetType.Fixed`인데 `_target`이 null인지 확인합니다.

### 좌표가 이상한 경우
- `UseTargetPositionOverride`가 켜져 있는지 확인합니다.
- Arc 타입인지, `TargetPositionRangeX`가 적용되는지 확인합니다.
- 타겟의 `GetRandomPositionYInHitArea()`가 기대 범위를 반환하는지 확인합니다.

### 다발 발사 간격이 이상한 경우
- `Count`와 `SecDelayByOne` 값을 확인합니다.
- 코루틴이 중간에 취소되지 않는지 확인합니다.

---

## 10. 한 줄 정리

`ProjectileController`는 **캐릭터 문맥을 가진 발사체 실행 입구이며, 메타데이터와 테이블 정보를 바탕으로 단발·다발 발사와 타겟/좌표 기반 launch를 조정하는 컨트롤러**입니다.
