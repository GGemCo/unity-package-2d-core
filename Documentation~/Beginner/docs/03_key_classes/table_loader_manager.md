# TableLoaderManager

## 1. 문서 목적

이 문서는 `TableLoaderManager`를 설명합니다.
Core와 상위 패키지들이 게임 데이터를 어디서 읽어야 하는지, 그리고 테이블 접근의 표준 진입점이 무엇인지 이해하는 것이 목적입니다.

---

## 2. 역할

`TableLoaderManager`는 **Core 데이터 테이블 허브**입니다.
수많은 `Table*` 클래스를 소유하고, 런타임에서 필요한 데이터를 UID 기반으로 찾아주는 진입점 역할을 합니다.

코드 기준으로 다루는 대표 테이블은 다음과 같습니다.

- NPC, Map, Monster, Animation
- Item, ItemVisual, ItemAffix 계열
- Window
- Stat, DamageType, State
- CrowdControl
- Vfx
- Interaction, Shop
- Quest, Dialogue, Cutscene
- Projectile
- Sound
- Simulation, ItemUse

즉, 게임 전반의 정적 정의 데이터가 거의 모두 이 매니저를 통해 노출됩니다.

---

## 3. 왜 중요한가

런타임에서 “데이터가 왜 적용되지 않는가”를 볼 때 가장 먼저 의심해야 하는 클래스입니다.

예를 들어 아래 문제는 대부분 여기까지 추적됩니다.

- 특정 UID 데이터가 null이다
- 테이블은 있는데 런타임 변환이 안 된다
- Window/Vfx/CrowdControl이 UID로 조회되지 않는다
- 잘못된 move step, 잘못된 상태 이름이 나온다

Core를 기준으로 런타임 데이터의 공식 입구가 하나 필요하고, 그 역할을 `TableLoaderManager`가 맡고 있습니다.

---

## 4. 핵심 구조

### 테이블 소유 프로퍼티
코드상 `TableLoaderManager`는 각 테이블 로더를 프로퍼티로 들고 있습니다.
예:

- `TableNpc`
- `TableMap`
- `TableMonster`
- `TableAnimation`
- `TableItem`
- `TableWindow`
- `TableStat`
- `TableCrowdControl`
- `TableVfxEffect`
- `TableVfxParticle`
- `TableProjectile`
- `TableSound`

이 구조의 장점은, 각 테이블 파싱 책임은 개별 클래스로 분리하고, 런타임 진입점만 중앙화할 수 있다는 점입니다.

### 싱글톤 접근
- `Instance`

대부분의 런타임 시스템은 이 매니저를 통해 데이터를 조회합니다.
따라서 초기화 시점과 라이프사이클을 항상 같이 봐야 합니다.

---

## 5. 주요 메서드

### `GetCharacterMoveStep(CharacterConstants.Type type, int characterUid)`
NPC/Monster 등 캐릭터 타입에 따라 이동 스텝 값을 조회합니다.
실제 캐릭터 초기화나 이동 감각에 영향을 주는 유틸리티성 메서드입니다.

### `RefreshStatusNames()`
상태 관련 이름을 다시 반영하는 보정 메서드입니다.
Localization이나 상태 데이터 연결이 바뀐 뒤 유용합니다.

### `GetNpcData / TryGetNpcData`
### `GetMapData / TryGetMapData`
### `GetMonsterData / TryGetMonsterData`
### `GetAnimationData / TryGetAnimationData`
### `GetItemData / TryGetItemData`
### `GetWindowData / TryGetWindowData`

대표적인 일반 테이블 조회 메서드입니다.

### `GetCrowdControlData / TryGetCrowdControlData`
Crowd Control 기본 테이블 행을 가져옵니다.

### `GetCrowdControlRuntimeData / TryGetCrowdControlRuntimeData`
Crowd Control 테이블을 실제 런타임 사용 형태인 `CrowdControlRuntimeData`로 해석합니다.
즉, 단순 조회를 넘어 해석 계층 진입점까지 제공합니다.

### `GetVfxData / TryGetVfxData`
VFX UID를 받아 `VfxRuntimeData`를 반환합니다.
`TableVfxEffect`와 `TableVfxParticle`를 모두 고려하여 조회하는 점이 중요합니다.

### `GetAllVfxData()`
전체 VFX 런타임 데이터 조회입니다.
VfxManager의 프리웜 준비처럼 전체 순회가 필요한 경우에 사용됩니다.

---

## 6. 연결해서 봐야 하는 클래스

### 공통 기반
- `TableLoaderBase`
- 각 `Table*` 클래스
- `StruckTable*` row 구조체/클래스

### 런타임 변환기
- `CrowdControlRuntimeDataResolver`
- `VfxRuntimeDataFactory`

### 소비자 클래스
- `CharacterBase`, `CharacterCrowdControlController`
- `UIWindowManager`
- `VfxManager`
- `ProjectileController`
- Save/Quest/Dialogue 계층

---

## 7. 대표 런타임 흐름

### 흐름 A: 게임 시작 시 테이블 로딩
1. 로딩 단계에서 각 테이블이 파싱됩니다.
2. `TableLoaderManager`가 각 `Table*` 인스턴스를 초기화합니다.
3. 런타임 시스템은 이후 `Instance`를 통해 접근합니다.

### 흐름 B: UID 기반 데이터 조회
1. 다른 시스템이 UID를 알고 있습니다.
2. `TableLoaderManager`에 조회를 요청합니다.
3. `GetDataByUid` 또는 런타임 데이터 해석 계층으로 이어집니다.
4. 결과를 기반으로 실제 실행이 일어납니다.

### 흐름 C: 해석형 데이터 사용
1. CC나 VFX처럼 단순 행 데이터로 부족한 시스템이 있습니다.
2. `TableLoaderManager`가 런타임 데이터 변환 메서드를 제공합니다.
3. 소비자 시스템은 변환 결과를 바로 사용할 수 있습니다.

---

## 8. 확장 포인트

### 새 테이블을 추가할 때
1. 개별 `Table*` 클래스를 만듭니다.
2. `TableLoaderManager` 프로퍼티에 추가합니다.
3. 필요한 `GetXData` / `TryGetXData` 진입점을 만듭니다.
4. 런타임 변환이 필요하면 별도 Resolver/Factory를 둡니다.

### 런타임 해석 계층이 필요한 경우
Raw row를 소비자 시스템이 직접 해석하게 두기보다, `TableLoaderManager` 또는 별도 Resolver에서 표준화하는 편이 좋습니다.

---

## 9. 디버깅 체크리스트

### 데이터가 null인 경우
- 테이블 로딩 자체가 완료되었는지 확인합니다.
- UID가 잘못되었는지 확인합니다.
- `TryGet`과 `Get` 호출 위치를 구분해 로그 누락이 없는지 확인합니다.

### VFX가 안 나오는 경우
- `GetVfxData()`에서 null인지 확인합니다.
- `TableVfxEffect`와 `TableVfxParticle` 어느 쪽에 있어야 하는지 확인합니다.

### CrowdControl이 이상한 경우
- 원본 `StruckTableCrowdControl`이 맞는지 먼저 확인합니다.
- 그 다음 `CrowdControlRuntimeDataResolver` 결과가 기대대로 해석되는지 확인합니다.

### 이동 스텝이 이상한 경우
- `GetCharacterMoveStep()`이 어떤 타입 분기로 들어가는지 확인합니다.
- 실제 캐릭터 UID와 테이블 UID가 일치하는지 확인합니다.

---

## 10. 한 줄 정리

`TableLoaderManager`는 **Core 런타임의 정적 데이터 진입 허브이며, UID 기반 조회와 일부 런타임 데이터 해석을 한곳에서 제공하는 중앙 매니저**입니다.
