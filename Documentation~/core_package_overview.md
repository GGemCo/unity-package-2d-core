# Core 패키지 핵심 클래스 정리

## 1. 문서 목적

이 문서는 **Core 패키지에서 우선적으로 이해해야 하는 중요한 클래스**를 Runtime / Editor로 나누어 정리한 문서입니다.

정리 기준은 다음과 같습니다.

* 다른 시스템이 자주 참조하는가
* 시스템의 진입점 또는 조정자 역할을 하는가
* 확장 시 영향 범위가 큰가
* 디버깅 시 먼저 확인해야 하는가

---

# 2. Runtime 핵심 클래스

## 2-1. 최상위 기준 클래스

### `CharacterBase`

**위치**
`Characters/CharacterBase.cs`

**역할**
플레이어, 몬스터, NPC 계열의 공통 캐릭터 기반 클래스입니다.
`CharacterStat`을 상속하고, 캐릭터 공통 액션 인터페이스를 연결하는 중심축입니다.

**왜 중요한가**
전투, 이동, 상태, 스탯, 피격, 애니메이션, 타겟팅 확장 대부분이 결국 이 계층에 연결됩니다.
캐릭터 기능을 추가할 때 가장 먼저 영향 범위를 검토해야 하는 클래스입니다.

**같이 봐야 하는 클래스**

* `CharacterStat`
* `CharacterBaseController`
* `CharacterDamageController`
* `CharacterHitArea`

---

### `CharacterStat`

**위치**
`Characters/CharacterStat.cs`
`Characters/Stats/*`

**역할**
캐릭터의 스탯 계산과 리소스(HP/MP/Stamina/보너스 HP 등)를 관리하는 메인 클래스입니다.
현재 구조는 partial 및 모듈 분리 방식으로 확장되고 있습니다.

**왜 중요한가**
Core 전투 시스템의 실질적인 데이터 중심입니다.
스탯 계산, 보너스 HP, 패시브/장비/아이템 보정, 리소스 갱신이 이 클래스 축으로 모입니다.

**같이 봐야 하는 클래스**

* `CharacterStat.Resources`
* `CharacterStat.BonusHp`
* `CharacterStat.Modules`
* `StatCalculator`
* `IStatModifierProvider`
* `PassiveSkillModifierProvider`
* `ItemBonusModifierProvider`
* `PersistentModifierProvider`

**문서화 우선순위**
매우 높음.
향후 Core 문서에서 별도 장으로 분리해도 될 수준입니다.

---

### `CharacterMotionController2D`

**위치**
`Characters/Motion/CharacterMotionController2D.cs`

**역할**
캐릭터 이동 모션의 실제 실행기입니다.
선형 이동, 홀드, 아크 이동 등 Motion Solver를 통해 물리/이동 보간을 처리합니다.

**왜 중요한가**
스킬 돌진, 넉백, 넉업, 자동 이동, 연출 이동이 모두 이 계층과 연결됩니다.
최근 구조상 Crowd Control과의 결합도 매우 강합니다.

**같이 봐야 하는 클래스**

* `MotionRequest`
* `IMotionSolver`
* `MotionSolverLinearMove`
* `MotionSolverLinearMoveHold`
* `MotionSolverArcPhased`

---

### `CharacterCrowdControlController`

**위치**
`Characters/CrowdControl/CharacterCrowdControlController.cs`

**역할**
넉백/넉다운/넉업 등 Crowd Control의 런타임 실행을 담당하는 메인 컨트롤러입니다.

**왜 중요한가**
최근 Core 패키지의 전투 확장 포인트 중 하나입니다.
애니메이션 단계, 모션 요청, 상태 제어, BT 중단 여부, 플레이어 조작 중지 여부와 강하게 연결됩니다.

**같이 봐야 하는 클래스**

* `CrowdControlRuntimeData`
* `CrowdControlHandlers`
* `CrowdControlConstants`
* `CharacterMotionController2D`

---

## 2-2. 플레이어 / 몬스터 중심 클래스

### `Player`

**위치**
`Characters/Player/Player.cs`

**역할**
플레이어 전용 캐릭터 구현입니다.

**왜 중요한가**
Control 패키지, UI, 저장 데이터, 장비, 퀵슬롯, 자동 이동 연동의 중심입니다.

---

### `ControllerPlayer`

**위치**
`Characters/Player/ControllerPlayer.cs`

**역할**
플레이어 쪽 캐릭터 제어 컴포넌트입니다.

**왜 중요한가**
입력 시스템과 Runtime 캐릭터 데이터의 접점입니다.
실제 조작과 캐릭터 동기화 문제를 볼 때 중요합니다.

---

### `PlayerUIController`

**위치**
`Characters/Player/PlayerUIController.cs`

**역할**
플레이어 상태를 HUD/UI와 연결하는 브리지 역할입니다.

**왜 중요한가**
스탯 변화, 보너스 HP, UI 갱신, 상태 아이콘 반영 등에서 중심이 됩니다.
최근 작업 흐름상 HP/MP/Stamina UI 연결 시 자주 수정 대상이 됩니다.

---

### `Monster`

**위치**
`Characters/Monster/Monster.cs`

**역할**
몬스터 전용 캐릭터 구현입니다.

---

### `ControllerMonster`

**위치**
`Characters/Monster/ControllerMonster.cs`

**역할**
몬스터 런타임 제어를 담당합니다.

---

### `MonsterBrainTicker`

**위치**
`Characters/Monster/MonsterBrainTicker.cs`

**역할**
몬스터 Brain/BT 계열의 Tick 실행 허브입니다.

**왜 중요한가**
AI_BT 패키지가 붙는 실제 런타임 접점에 가깝습니다.
CC 중 Tick 정지 여부, 우선순위 Brain 변경, 스킬 실행 충돌 분석 시 중요합니다.

---

### `MonsterBrainSelector`

**위치**
`Characters/Monster/MonsterBrainSelector.cs`

**역할**
현재 활성화할 Brain / Tickable Brain을 선택하는 정적 유틸리티입니다.

**왜 중요한가**
다중 Brain 구조에서 누가 우선권을 갖는지 판단하는 핵심입니다.

---

### `ControllerMonsterSuperArmor`

**위치**
`Characters/Monster/ControllerMonsterSuperArmor.cs`

**역할**
몬스터 슈퍼아머 상태와 Groggy 전환 같은 처리를 담당합니다.

**왜 중요한가**
피격 누적, 슈퍼아머 붕괴, 상태 전환, UI 연동이 연결되는 실전 전투 포인트입니다.

---

## 2-3. 로딩 / 씬 / 데이터 진입점

### `GameLoaderManager`

**위치**
`Core/GameLoaderManager.cs`

**역할**
게임 로딩 시퀀스를 관리하는 메인 클래스입니다.
Load Step 등록, 진행률 갱신, 완료 처리 등을 담당합니다.

**왜 중요한가**
프로젝트 시작 흐름에서 가장 먼저 보는 클래스 중 하나입니다.
테이블, 로컬라이제이션, 저장 데이터 초기화 순서를 추적할 때 핵심입니다.

**같이 봐야 하는 클래스**

* `IGameLoadStep`
* `GameLoadStepBase`
* `TableLoadStep`
* `LocalizationLoadStep`
* `SaveDataLoadStep`

---

### `SceneGame`

**위치**
`Scenes/SceneGame.cs`

**역할**
Game 씬의 메인 진입점 역할을 하는 클래스입니다.

**왜 중요한가**
캔버스, VFX, UI, 씬 공용 오브젝트 참조가 여기에 집중되어 있습니다.
실제 플레이 씬 초기화 흐름을 볼 때 우선 확인 대상입니다.

---

### `SceneManager`

**위치**
`Core/SceneManager.cs`

**역할**
씬 전환 유틸리티입니다.

**왜 중요한가**
구조는 단순하지만 전역 흐름의 출발점이라 영향 범위가 큽니다.

---

### `TableLoaderManager`

**위치**
`TableLoader/TableLoaderManager.cs`

**역할**
Core 데이터 테이블 접근의 중심 허브입니다.
Npc, Map, Monster, Animation, Item, Window 등 테이블 조회 API를 제공합니다.

**왜 중요한가**
런타임의 거의 모든 데이터 진입점입니다.
“데이터가 왜 적용되지 않는가”를 볼 때 가장 먼저 확인해야 하는 클래스입니다.

**같이 봐야 하는 클래스**

* `TableLoaderBase`
* `TableRegistry`
* `TableLoader/Table/*`

---

### `AddressableLoaderController`

**위치**
`AddressableLoader/AddressableLoaderController.cs`

**역할**
Addressables 기반 로딩 진입 유틸리티입니다.

**왜 중요한가**
캐릭터, 공용 프리팹, 사운드, VFX, 설정 등의 로딩 흐름과 이어집니다.

**같이 봐야 하는 클래스**

* `AddressableLoaderPrefabCharacter`
* `AddressableLoaderPrefabCommon`
* `AddressableLoaderPrefabVfx`
* `AddressableLoaderSound`
* `AddressableLoaderSettings`

---

## 2-4. UI 중심 클래스

### `UIWindowManager`

**위치**
`UI/Core/Base/UIWindowManager.cs`

**역할**
윈도우 프리팹 초기화, 창 표시/숨김, 아이콘 이동/등록/해제 등을 담당하는 UI 전역 관리자입니다.

**왜 중요한가**
인벤토리, 퀵슬롯, 장비창 등 윈도우 기반 UI 공통 동작의 중심입니다.
UI 버그가 날 때 가장 자주 확인하게 되는 매니저입니다.

**같이 봐야 하는 클래스**

* `UIWindow`
* `UIWindowBase`
* `IconPoolManager`

---

### `UIWindow`

**위치**
`UI/Core/Base/UIWindow.cs`

**역할**
개별 UI 창의 공통 베이스 구현입니다.
드래그 앤 드롭, 슬롯/아이콘 관리, 풀 초기화 등을 담당합니다.

**왜 중요한가**
대부분의 UGUI 기반 윈도우가 이 공통 구조 위에 놓입니다.

---

### `PopupManager`

**위치**
`Popup/PopupManager.cs`

**역할**
경고/에러/확인 팝업 큐와 표시를 관리합니다.

**왜 중요한가**
즉시 사용자 피드백, 확인 창, 강제 팝업 흐름의 중심입니다.

---

### `SystemMessageManager`

**위치**
`SystemMessage/SystemMessageManager.cs`

**역할**
시스템 메시지, 경고 메시지, 정보 메시지를 화면에 출력하는 매니저입니다.

**왜 중요한가**
스킬 실패, 자원 부족, 잘못된 입력 등 **플레이어 피드백 계층**의 핵심입니다.

**같이 봐야 하는 클래스**

* `ResultCommon`

---

### `UIFloatingTextManager`

**위치**
`UI/Core/FloatingText/UIFloatingTextManager.cs`

**역할**
데미지 텍스트나 플로팅 텍스트 표시를 담당합니다.

**왜 중요한가**
전투 피드백 품질과 직결됩니다.

---

## 2-5. 전투 피드백 / VFX / Projectile

### `ProjectileController`

**위치**
`Projectile/ProjectileController.cs`

**역할**
프로젝트일 생성, 발사, 버스트 처리 등의 런타임 허브입니다.

**왜 중요한가**
스킬 시스템과 매우 밀접합니다.
타겟 미지정 발사, 충돌, 종점 처리, 바운더리 처리 문제의 중심입니다.

**같이 봐야 하는 클래스**

* `ProjectileBase`
* `ProjectileManager`
* `Projectile/Visual/*`

---

### `VfxManager`

**위치**
`Vfx/VfxManager.cs`

**역할**
VFX 생성과 요청 적용, 프리웜, SpawnPolicy 적용을 담당합니다.

**왜 중요한가**
기존 Effect 시스템에서 VFX 시스템으로 전환된 이후 가장 중요한 전역 매니저 중 하나입니다.
애니메이션 이벤트, Affect, Projectile, Skill 모두와 연결됩니다.

**같이 봐야 하는 클래스**

* `VfxBehaviourBase`
* `VfxFadeController`
* `Vfx/Effect/*`
* `Vfx/Particle/*`

---

### `SoundManager`

**위치**
`Sound/SoundManager.cs`

**역할**
전역 사운드 재생을 담당합니다.

**왜 중요한가**
구조는 전형적이지만, Core 공용 피드백 계층이라 의존도가 높습니다.

---

### `Animation2dController`

**위치**
`Animation/Animation2dController.cs`

**역할**
2D 애니메이션 클립 재생 관리 클래스입니다.

**왜 중요한가**
캐릭터, VFX, 클립 길이 계산, 상태 전환 문제에서 자주 확인됩니다.

---

### `AnimationEventMediator`

**위치**
`Animation/AnimationEventMediator.cs`

**역할**
애니메이션 이벤트를 런타임 시스템으로 중계합니다.

**왜 중요한가**
스킬 이벤트, VFX 이벤트, 사운드 이벤트 연결의 허브 역할을 합니다.

---

## 2-6. 저장 / 복원 / 게임 상태

### `SaveRegistry`

**위치**
`SaveData/Support/SaveRegistry.cs`

**역할**
저장 기여자 등록과 복원 시점 연결을 담당하는 정적 레지스트리입니다.
지연 등록된 객체에도 Restore를 적용하는 구조를 제공합니다.

**왜 중요한가**
초기화 순서 문제를 줄이는 핵심 장치입니다.
최근 프로젝트 방향에서도 매우 중요한 기반으로 보입니다.

---

### `SaveDataManagerBase`

**위치**
`SaveData/Base/SaveDataManagerBase.cs`

**역할**
세이브 슬롯, 저장 주기, 저장 파일 컨트롤러, Envelope 빌드 등을 관리하는 저장 베이스 클래스입니다.

**왜 중요한가**
저장 시스템의 공통 뼈대입니다.
자동 저장, 강제 저장, 슬롯 관리, 썸네일 관리와 이어집니다.

**같이 봐야 하는 클래스**

* `SaveDataLoader`
* `SaveFileController`
* `SaveEnvelope`
* `ISaveContributor`

---

### `GameTimeManager`

**위치**
`Core/GameTimeManager.cs`

**역할**
인게임 시간 흐름을 관리합니다.

**왜 중요한가**
Simulation/Quest/SaveData와 연결될 여지가 크며, 시스템 공통 시간 기준이 됩니다.

---

## 2-7. 기타 중요 매니저

### `MapManager`

**위치**
`Maps/MapManager.cs`

**역할**
맵 관련 런타임 제어의 중심입니다.

---

### `ItemManager`

**위치**
`Items/ItemManager.cs`

**역할**
아이템 런타임 처리의 허브입니다.

---

### `InteractionManager`

**위치**
`Interaction/InteractionManager.cs`

**역할**
오브젝트 상호작용 처리의 중심입니다.

---

### `LocalizationManager`

**위치**
`Localization/LocalizationManager.cs`

**역할**
로컬라이제이션 런타임 진입점입니다.

---

# 3. Editor 핵심 클래스

Core 문서상 Editor는 `GGemCoTool`, `GGemCoProjectSetup`, `GGemCoCreator`를 중심으로 구성되며, 테스트/편집 툴과 프로젝트 초기 세팅을 담당합니다.

## 3-1. 데이터 편집 계열

### `TableEditorWindow`

**위치**
`GGemCoTool/TableEditor/TableEditorWindow.cs`

**역할**
테이블 데이터를 직접 확인/수정하는 메인 EditorWindow입니다.

**왜 중요한가**
Core Editor에서 가장 중요한 툴 중 하나입니다.
실무상 테이블 편집, 참조 점프, 키 기반 조회, 검증 흐름이 모두 이 창에 모입니다.

**같이 봐야 하는 클래스**

* `TableEditorDocument`
* `TableEditorGui`
* `TableEditorSchema`
* `TableEditorRuleProvider`
* `TableEditorUndoController`
* `TableEditorReflectionUtility`
* `TableEditorValueUtility`
* `TableEditorValidator`
* `TableEditorReferenceCache`

---

### `TableRowEditorUtility`

**위치**
`GGemCoTool/Utils/TableRowEditorUtility.cs`

**역할**
테이블 행 객체를 IMGUI 기반으로 공통 편집하는 유틸리티입니다.

**왜 중요한가**
UseCrowdControl, UseVfx, 스킬 패널 편집 등 여러 툴의 공통 기반으로 사용하기 좋습니다.
향후 Core Editor의 표준 Row 편집 계층으로 볼 수 있습니다.

---

### `TableEditorReflectionUtility`

**위치**
`GGemCoTool/TableEditor/TableEditorReflectionUtility.cs`

**역할**
리플렉션 기반 표시명/필드 해석을 지원합니다.

**왜 중요한가**
테이블 표시 품질과 편집 UX에 직접 영향을 줍니다.

---

## 3-2. 검색 / 선택 UI 계열

### `SearchableDropdownUtility`

**위치**
`GGemCoTool/Utils/SearchableDropdown/SearchableDropdownUtility.cs`

**역할**
검색 가능한 드롭다운 UI를 제공하는 공용 유틸리티입니다.

**왜 중요한가**
최근 MapEditor, MoveMap, TestDropItemRate, Affect/Vfx 선택 등 여러 툴에 확산되고 있습니다.
Core Editor UX 개선의 핵심 공통 부품입니다.

**같이 봐야 하는 클래스**

* `SearchableDropdownUtilityButton`
* `SearchableDropdownUtilityLabelField`
* `SearchableDropdownUtilityUIToolkit`

---

## 3-3. 테스트 / 디버그 툴 계열

### `UseCrowdControl`

**위치**
`GGemCoTool/Test/UseCrowdControl.cs`

**역할**
Crowd Control 데이터 선택, 대상 선택, 상세 Row 편집, 플레이 모드 테스트를 담당하는 툴입니다.

**왜 중요한가**
최근 CC 시스템 확장과 함께 디버깅 핵심 툴이 되었습니다.

**같이 봐야 하는 클래스**

* `UseCrowdControlKnockBack`
* `UseCrowdControlKnockDown`
* `UseCrowdControlKnockUp`
* `UseCrowdControlDetailWindowBase`

---

### `UseProjectile`

**위치**
`GGemCoTool/Test/UseProjectile.cs`

**역할**
프로젝트일 데이터를 선택하고 런타임 발사를 테스트하는 툴입니다.

**왜 중요한가**
Projectile 런타임 검증의 핵심 툴입니다.

---

### `UseVfx`

**위치**
`GGemCoTool/Test/Vfx/UseVfx.cs`

**역할**
VFX 공통 테스트 창의 베이스입니다.

**왜 중요한가**
Effect/Particle 분리 이후 VFX 검증 툴 구조의 공통 베이스 역할을 합니다.

**같이 봐야 하는 클래스**

* `UseVfxEffect`
* `UseVfxParticle`

---

### `TestDropItemRate`

**위치**
`GGemCoTool/Test/TestDropItemRate.cs`

**역할**
몬스터 드롭률을 검사하는 툴입니다.

**왜 중요한가**
게임 밸런스 검증용 툴로 실무 활용도가 높습니다.

---

### `MapEditor`

**위치**
`GGemCoTool/MapEditor/MapEditor.cs`

**역할**
맵에 NPC/몬스터/워프 등을 배치하는 툴입니다.

**왜 중요한가**
맵 제작 파이프라인에서 핵심 툴입니다.
테이블 선택, 에디터 배치, 내보내기 흐름이 결합됩니다.

**같이 봐야 하는 클래스**

* `MonsterExporter`
* `NpcExporter`
* `WarpExporter`
* `CharacterInfoWatcher`

---

## 3-4. 프로젝트 세팅 / 생성 툴 계열

### `AutoProjectSetupWindow`

**위치**
`GGemCoProjectSetup/AutoProjectSetupWindow.cs`

**역할**
프로젝트 초기 세팅을 일괄 수행하는 EditorWindow입니다.

**왜 중요한가**
신규 프로젝트 셋업 자동화의 중심입니다.
레이어, 태그, 소팅레이어, Addressables, 기본 씬, ScriptableObject 세팅 등이 이 흐름에 들어갑니다.

---

### `SetupRunner`

**위치**
`GGemCoProjectSetup/Support/SetupRunner.cs`

**역할**
프로젝트 셋업 Step을 순차 실행하는 실행기입니다.

**왜 중요한가**
셋업 자동화 구조의 핵심입니다.
Step 기반으로 확장 가능한 점이 좋습니다.

**같이 봐야 하는 클래스**

* `SetupStepBase`
* `StepAddLayers`
* `StepAddSortingLayers`
* `StepAddTags`
* `StepCreateDefaultScenes`
* `StepCreateSettingScriptableObject`

---

### `CreatorHubWindow`

**위치**
`GGemCoCreator/Windows/CreatorHubWindow.cs`

**역할**
에셋 생성 도구의 허브 윈도우입니다.

**왜 중요한가**
Projectile, Trap, Vfx 등 새 리소스 생성의 진입점 역할을 합니다.

**같이 봐야 하는 클래스**

* `ProjectileFactory`
* `TrapFactory`
* `VfxFactory`

---

## 3-5. 보조이지만 유지 가치가 높은 툴

아래 클래스들은 “핵심 인프라”보다는 “생산성 강화” 쪽에 가깝지만, 실제 프로젝트 운영에서 매우 유용합니다.

* `AnimationEventNameChangerWindow`
  애니메이션 이벤트 이름 일괄 변경 툴
* `LocalizationUsageFinder`
  로컬라이즈 키 사용처 탐색
* `SpineJsonValidatorWindow`
  Spine JSON 검사
* `DebugOptionAssetScanner`
  디버그 옵션 자산 스캔
* `AddressableLoaderTool` / `SettingVfx` / `SettingTable`
  Addressables 세팅 보조

---

# 4. 문서화 우선순위 제안

소스 문서로 사용할 목적이라면, 아래 순서로 별도 상세 문서를 만드는 것이 좋습니다.

## Runtime 1순위

* `CharacterStat`
* `CharacterBase`
* `CharacterMotionController2D`
* `CharacterCrowdControlController`
* `TableLoaderManager`
* `GameLoaderManager`
* `UIWindowManager`
* `VfxManager`
* `ProjectileController`
* `SaveRegistry`
* `SaveDataManagerBase`

## Runtime 2순위

* `PlayerUIController`
* `MonsterBrainTicker`
* `MonsterBrainSelector`
* `SystemMessageManager`
* `PopupManager`
* `Animation2dController`
* `MapManager`
* `ItemManager`

## Editor 1순위

* `TableEditorWindow`
* `TableRowEditorUtility`
* `SearchableDropdownUtility`
* `UseCrowdControl`
* `UseProjectile`
* `UseVfx`
* `MapEditor`
* `AutoProjectSetupWindow`
* `SetupRunner`

## Editor 2순위

* `CreatorHubWindow`
* `AnimationEventNameChangerWindow`
* `LocalizationUsageFinder`
* `SpineJsonValidatorWindow`

---

# 5. Core 패키지 구조를 한 문장으로 요약하면

Core는 **캐릭터/전투/스탯/UI/테이블/저장/로딩/VFX 같은 공통 기반을 제공하는 하위 런타임 계층**이고, Editor는 그 위에서 **데이터 편집, 테스트, 프로젝트 셋업, 생성 툴**을 제공하는 구조로 보는 것이 가장 적절합니다.
