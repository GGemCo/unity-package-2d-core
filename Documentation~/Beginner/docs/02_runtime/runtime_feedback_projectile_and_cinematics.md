# Runtime 기능 영역 문서 - 피드백, Projectile, 연출

## 1. 문서 목적

이 문서는 Core Runtime에서 **전투 체감과 연출 품질을 담당하는 시스템**을 설명합니다.
Projectile, VFX, 사운드, 애니메이션, 카메라, 컷신이 각각 어떤 책임을 가지는지 정리하는 것이 목적입니다.

---

## 2. 이 영역에 포함되는 주요 폴더

- `Projectile/`
- `Vfx/`
- `Sound/`
- `Animation/`
- `Camera/`
- `Cutscene/`
- `Characters/HitStop/`

이 영역은 다른 패키지에서 자주 호출되지만, 실행기 자체는 Core에 두는 편이 구조적으로 안정적입니다.

---

## 3. 대표 클래스

### Projectile
- `Projectile/ProjectileController.cs`
- `Projectile/ProjectileBase.cs`
- `Projectile/ProjectileManager.cs`
- `Projectile/ProjectileLinear.cs`
- `Projectile/ProjectileArc.cs`
- `Projectile/ProjectileLaser.cs`
- `Projectile/Visual/*`

### VFX
- `Vfx/VfxManager.cs`
- `Vfx/VfxBehaviourBase.cs`
- `Vfx/VfxPoolService.cs`
- `Vfx/VfxRuntimeData.cs`
- `Vfx/VfxSpawnRequest.cs`
- `Vfx/Effect/*`
- `Vfx/Particle/*`

### 사운드
- `Sound/SoundManager.cs`
- `Sound/SoundControllerBgm.cs`
- `Sound/SoundControllerSfx.cs`

### 애니메이션/카메라/컷신
- `Animation/Animation2dController.cs`
- `Animation/AnimationEventMediator.cs`
- `Camera/*`
- `Cutscene/*`

### 타격감 보조
- `Characters/HitStop/CharacterHitStopController.cs`
- `Characters/HitStop/HitStopRequest.cs`

---

## 4. 이 영역의 핵심 책임

## 4-1. 실행과 요청의 분리

이 영역은 대체로 “무엇을 실행할지 결정하는 계층”과 “실행을 실제로 수행하는 계층”을 나누는 것이 좋습니다.

예를 들어 상위 패키지는
- 어떤 Projectile을 쓸지
- 어떤 VFX를 재생할지
- 어떤 카메라 연출을 줄지
결정할 수 있지만,

실제 생성/재생/정리 책임은 Core의 실행기 계층이 맡는 구조가 더 안정적입니다.

## 4-2. Projectile을 공용 전투 리소스로 유지

Projectile은 스킬 시스템과 강하게 연결되더라도, 발사체의 생성/이동/충돌/시각 표현은 Core 공용 영역에 두는 편이 좋습니다.

이 구조를 유지하면 다음 장점이 있습니다.

- 스킬 외 다른 시스템도 발사체를 재사용할 수 있다.
- 충돌/이동/시각 표현 규칙을 중앙에서 관리할 수 있다.
- Projectile의 디버깅 포인트가 분산되지 않는다.

## 4-3. VFX와 사운드의 전역 관리

`VfxManager`와 `SoundManager`는 각각 시각/청각 피드백의 전역 허브입니다.
실제 게임에서 연출 품질을 좌우하지만, 동시에 가장 중복 호출과 누수 문제가 생기기 쉬운 영역이기도 합니다.

따라서 이 영역은 보통 다음을 문서화해 두는 것이 좋습니다.

- 누가 재생 요청을 보내는가
- 어떤 키나 프리팹으로 찾는가
- 재생 후 누가 정리하는가
- Follow형과 OneShot형을 어떻게 구분하는가

## 4-4. 애니메이션 이벤트와 전투 연결

`AnimationEventMediator`는 애니메이션 타임라인과 런타임 시스템을 잇는 중요한 브리지입니다.
공격 판정, VFX 재생, 사운드 재생, 컷신 연출이 애니메이션 이벤트와 맞물릴 수 있으므로, 이 계층이 지나치게 비대해지지 않게 주의해야 합니다.

## 4-5. 컷신과 카메라의 공용 연출 계층

`Cutscene/`과 `Camera/`는 전투 외 연출에서도 재사용될 수 있는 공용 시스템입니다.
즉흥적으로 개별 스킬 안에 카메라 연출 로직을 넣기보다, Core 쪽 공용 컷신/카메라 계층을 통해 요청하는 구조가 더 좋습니다.

---

## 5. 대표 런타임 흐름

### 흐름 A: Projectile 발사

1. 호출부가 발사 요청을 보냅니다.
2. `ProjectileController`가 정의와 스폰 정보를 해석합니다.
3. 적절한 `ProjectileBase` 파생 구현이 생성/초기화됩니다.
4. 이동, 충돌, 종료 시점이 처리됩니다.
5. 필요 시 `ProjectileVisual*`이 시각 표현을 담당합니다.

### 흐름 B: 전투 VFX/사운드 재생

1. 공격, 피격, 상태 변화, UI 이벤트가 발생합니다.
2. `VfxManager` 또는 `SoundManager`가 재생 요청을 받습니다.
3. 해당 키나 프리팹을 기준으로 리소스를 생성/재생합니다.
4. 재생 정책에 따라 자동 정리 또는 지속 추적을 수행합니다.

### 흐름 C: 컷신/카메라 연출

1. 특정 이벤트가 컷신 연출을 요청합니다.
2. 카메라 이동, 줌, 쉐이크, 페이드, 캐릭터 이동, UI 표시 제어가 단계별로 실행됩니다.
3. 연출 종료 후 원래 상태를 복구합니다.

---

## 6. 추천 읽기 순서

1. `ProjectileController`, `ProjectileBase`
2. `ProjectileVisual*`
3. `VfxManager`, `VfxBehaviourBase`
4. `SoundManager`
5. `Animation2dController`, `AnimationEventMediator`
6. `CharacterHitStopController`
7. `Camera/*`
8. `Cutscene/*`

---

## 7. 기능 추가 시 배치 기준

## 이 영역에 넣는 것이 맞는 경우
- 새 Projectile 실행기나 시각 표현
- 공용 VFX/사운드 재생 로직
- 카메라 연출, 컷신 연출
- HitStop 같은 전역 타격감 보조

## 다른 영역에 두는 것이 좋은 경우
- 특정 스킬의 발동 조건
- 특정 상태 이상 규칙
- UI 값 계산
- 전투 데미지 계산 자체

즉, 이 영역은 **결과를 체감하게 만드는 실행 계층**으로 보는 것이 좋습니다.

---

## 8. 디버깅 포인트

### Projectile이 안 보이거나 안 맞는 경우
- `ProjectileController`까지 요청이 도달했는지 확인합니다.
- 시각 표현 문제인지, 충돌 문제인지, 이동 문제인지 분리합니다.
- 정의 데이터와 실제 스폰 파라미터가 일치하는지 확인합니다.

### VFX가 중복되거나 정리되지 않는 경우
- OneShot과 Follow 재생 정책을 구분합니다.
- Pool 재사용 시 이전 상태가 남아 있지 않은지 확인합니다.
- 종료 조건이 프리팹마다 다른지 점검합니다.

### 사운드가 재생되지 않는 경우
- 사운드 키 또는 Addressables 등록 문제인지 확인합니다.
- SFX/BGM 채널이 다른 설정을 쓰는지 확인합니다.

### 애니메이션 타이밍이 어긋나는 경우
- `AnimationEventMediator` 이벤트 시점과 실제 런타임 처리 시점을 함께 확인합니다.
- 애니메이션 이벤트 누락인지, 이벤트는 왔지만 후속 시스템이 실패했는지 구분합니다.

---

## 9. 새로 합류한 개발자를 위한 한 줄 정리

이 영역은 **전투와 연출의 결과를 플레이어가 실제로 느끼게 만드는 실행 계층**이며,
발사체, VFX, 사운드, 카메라, 컷신이 공용 규칙 아래에서 동작하도록 만드는 구조입니다.
