# VfxManager

## 1. 문서 목적

이 문서는 `VfxManager`를 설명합니다.
VFX UID 기반 생성, 풀링, 스폰 정책 적용, 애니메이션 컨트롤러 연결이 어떤 흐름으로 이루어지는지 이해하는 것이 목적입니다.

---

## 2. 역할

`VfxManager`는 **VFX 생성과 풀링의 전역 실행기**입니다.
단순히 프리팹을 Instantiate하는 것이 아니라, 테이블 데이터와 Addressables, 풀링, 재생 정책을 묶어 실제 VFX를 만들어냅니다.

코드상 핵심 책임은 다음과 같습니다.

- `SceneGame`과 연결
- `VfxPoolService` 관리
- VFX 테이블 조회
- 프리팹 키 해석
- 풀 프리웜
- 요청 기반 스폰 정책 적용
- 애니메이션 컨트롤러 보장
- 종료 후 풀 반환

즉, 이 클래스는 “VFX를 재생 가능한 상태로 만들어주는 중앙 실행기”입니다.

---

## 3. 왜 중요한가

Skill, Affect, Projectile, Animation Event, UI 연출이 모두 결국 VFX 생성과 연결됩니다.
따라서 VFX가 안 보이거나, Follow가 이상하거나, 풀링이 꼬이는 문제는 대부분 이 클래스까지 내려와서 봐야 합니다.

특히 다음 상황에서 중요합니다.

- VFX UID는 맞는데 실제 이펙트가 안 나온다
- Effect/Particle 타입이 잘못 붙는다
- Follow 대상이 안 붙는다
- UI용 VFX가 월드에 뜬다
- 풀 프리웜/반환이 맞지 않는다

---

## 4. 핵심 상태

### 씬/애니메이션 연결
- `_sceneGame`
- `_animationEventMediator`

씬의 `canvasUI` 같은 참조와 애니메이션 이벤트 브리지를 연결합니다.

### 풀링
- `_poolService`
- `_didInitialPrewarm`

전체 VFX 프리웜을 한 번만 수행하도록 제어합니다.

---

## 5. 주요 진입 메서드

### `Initialize(SceneGame sceneGame)`
매니저를 씬과 연결하고 풀링 서비스를 초기화합니다.
필요하면 초기 프리웜도 수행합니다.

### `OnStartBySceneGame()`
SceneGame 시작 시점에 프리웜을 다시 보장하는 훅입니다.
씬 라이프사이클과 분리된 초기화 문제를 완화합니다.

### `CreateVfx(int vfxUid, float duration = 0f)`
가장 단순한 진입점입니다.
UID와 선택적 duration만으로 VFX를 생성합니다.

### `CreateVfx(StruckAnimationEventVfx struckAnimationEventVfx)`
애니메이션 이벤트 데이터를 VFX 스폰 요청으로 바꿔 실행합니다.
애니메이션 기반 VFX 연동의 핵심 경로입니다.

### `CreateVfx(VfxSpawnRequest request)`
실제 중심 메서드입니다.

이 메서드는 대략 다음 일을 수행합니다.

1. UID 유효성 검사
2. `TableLoaderManager.Instance.GetVfxData()` 호출
3. 프리팹 해석
4. 풀 구성 및 인스턴스 획득
5. Behaviour 보장
6. SpawnPolicy 해석
7. AnimationController 보장
8. 요청 적용
9. 활성화

### `SetAnimationEventMediator(AnimationEventMediator mediator)`
VFX의 애니메이션 이벤트를 전역 중계기와 연결합니다.

---

## 6. 중요한 내부 메서드

### `TryPrewarmAllConfiguredVfx()`
전체 VFX 데이터를 순회하면서 `PoolPrewarmCount`가 설정된 항목을 미리 구성합니다.
로딩 직후 VFX 지연을 줄이는 데 중요합니다.

### `ResolvePrefab(VfxRuntimeData info)`
`ConfigAddressableGroupName.Vfx_{PrefabPath}` 형태의 키를 만들어 Addressables 프리팹을 찾습니다.
VFX가 안 뜰 때 가장 먼저 확인할 해석 지점입니다.

### `ApplyRequest(...)`
요청 정보에 따라
- Parent
- UI Canvas 부모
- WorldPosition
- Owner/Target Follow
- Duration/Scale/Color
- SortingLayer/SortingOrder
- PositionY
를 실제 인스턴스에 적용합니다.

### `EnsureBehaviour(...)`
VFX 종류에 따라 적절한 `VfxBehaviourBase` 파생 컴포넌트를 보장합니다.
- Particle → `VfxBehaviourParticle`
- Laser → `VfxEffectLaser`
- 일반 Effect → `VfxBehaviourEffect`

### `EnsureAnimationController(...)`
필요한 경우 Spine 또는 Sprite 애니메이션 컨트롤러를 붙입니다.

---

## 7. 연결해서 봐야 하는 클래스

### 데이터와 로더
- `TableLoaderManager`
- `VfxRuntimeData`
- `VfxRuntimeDataFactory`
- `AddressableLoaderPrefabVfx`

### 풀과 실행
- `VfxPoolService`
- `VfxSpawnRequest`
- `VfxSpawnPolicy`
- `VfxBehaviourBase`

### 구체 Behaviour
- `VfxBehaviourEffect`
- `VfxBehaviourParticle`
- `VfxEffectLaser`

### 애니메이션 계층
- `AnimationEventMediator`
- `VfxAnimationControllerSprite`
- `VfxAnimationControllerSpine`
- `Animation2dController`

---

## 8. 대표 런타임 흐름

### 흐름 A: UID로 일반 VFX 생성
1. 외부 시스템이 VFX UID를 요청합니다.
2. `GetVfxData()`로 런타임 데이터를 가져옵니다.
3. Addressables에서 프리팹을 찾습니다.
4. 풀에서 인스턴스를 가져옵니다.
5. Behaviour와 SpawnPolicy를 붙입니다.
6. 활성화합니다.

### 흐름 B: 애니메이션 이벤트 기반 생성
1. 애니메이션 이벤트가 `StruckAnimationEventVfx`를 전달합니다.
2. `VfxSpawnRequest.FromAnimationEvent()`로 요청을 만듭니다.
3. 나머지는 일반 생성 흐름과 동일하게 처리됩니다.

### 흐름 C: UI용 또는 Follow형 생성
1. 요청에 Parent, Owner, Target, AttachType이 포함됩니다.
2. `ApplyRequest()`가 UI Canvas 부모 혹은 캐릭터 Follow를 설정합니다.
3. Lifecycle 정책에 따라 재생 후 풀로 반환됩니다.

---

## 9. 확장 포인트

### 새 VFX 타입을 추가할 때
`EnsureBehaviour()`와 필요한 Animation Controller 보장 경로를 같이 확장해야 합니다.

### 새 스폰 정책을 추가할 때
`VfxSpawnPolicy`와 `ApplyRequest()`의 역할을 먼저 확장하는 편이 좋습니다.
`CreateVfx()`에 분기를 늘리는 방식은 빠르게 복잡해집니다.

### Addressables 키 체계를 바꿀 때
`ResolvePrefab()`이 핵심입니다.
런타임/에디터 규약이 모두 이 키 체계를 따라가야 합니다.

---

## 10. 디버깅 체크리스트

### VFX가 아예 안 나오는 경우
- `request.VfxUid`가 0 이하가 아닌지 확인합니다.
- `GetVfxData()`가 null을 반환하는지 확인합니다.
- `ResolvePrefab()` 키가 실제 Addressables와 맞는지 확인합니다.

### 타입이 잘못 붙는 경우
- `VfxRuntimeData`의 `PlaybackType`, `EffectType`, 런타임 데이터 타입을 확인합니다.
- `EnsureBehaviour()` 분기가 기대대로 들어가는지 확인합니다.

### Follow가 안 되는 경우
- `Owner`, `Target`, `FollowTarget`, `AttachType`, `FollowMode`가 어떤 조합으로 전달되는지 확인합니다.
- `ApplyRequest()`가 최종 부모와 follow 대상을 어떻게 설정하는지 확인합니다.

### 풀링이 꼬이는 경우
- `ReleaseToPool()`이 정상적으로 호출되는지 확인합니다.
- 같은 UID인데 프리팹이 바뀌는 케이스가 없는지 확인합니다.

---

## 11. 한 줄 정리

`VfxManager`는 **VFX UID와 요청 정보를 받아 Addressables, 풀링, SpawnPolicy, AnimationController를 연결해 실제 연출을 생성하는 전역 실행기**입니다.
