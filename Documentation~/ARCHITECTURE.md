# Core 문서

이 폴더는 **Core 패키지**의 구조/규칙/변경 절차를 표준화하기 위한 문서입니다.

- Runtime 네임스페이스: `GGemCo2DCore`
- Editor 네임스페이스: `GGemCo2DCoreEditor`

Unity 공식 문서 참고 링크:
- Assembly Definition(런타임/에디터 분리): https://docs.unity3d.com/6000.3/Documentation/Manual/cus-asmdef.html
- ScriptableObject(데이터 컨테이너/저장 특성): https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html
- EditorWindow(커스텀 툴): https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorWindow.html
- EditorWindow(UI Toolkit 가이드): https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-HowTo-CreateEditorWindow.html
- Addressables(패키지): https://docs.unity3d.com/Packages/com.unity.addressables%40latest/
- Addressables(개요): https://docs.unity3d.com/Packages/com.unity.addressables%401.24/manual/AddressableAssetsOverview.html
- Undo(에디터 Undo/Redo): https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Undo.html
- Serialization(직렬화 규칙): https://docs.unity3d.com/Manual/script-Serialization.html


## 1. 역할

Core는 프로젝트의 **공통 런타임 기반**입니다.

- 캐릭터(플레이어/몬스터) 기반 클래스, 스탯/피격/상태 관리
- UI 윈도우/요소(UGUI 기반)
- 데이터 테이블 로더(TableLoader)와 공통 테이블 구조
- Addressables 기반 로딩(AddressableLoader*)
- 이펙트/사운드/프로젝트일(Projectile) 등 전역 시스템

Core는 Quest/Control/Affect/Skill/BT 같은 상위 패키지에서 **의존하는 하위 계층**으로 동작합니다.

퀘스트 진행, 목표 처리, 보상 UI, NPC 퀘스트 표시와 같은 Quest 전용 로직은 `com.ggemco.2d.quest` 패키지로 분리합니다. Core는 Quest를 직접 참조하지 않고, 상위 패키지가 사용할 수 있는 공통 이벤트/레지스트리/저장 확장 포인트만 제공합니다.

## 2. 폴더(개략)와 책임

아래는 실제 코드 폴더 구성을 기준으로 한 책임 분류입니다.

- `Characters/` : CharacterBase, Player, Monster 및 전투/피격/상태/컨트롤러
- `UI/` : UIWindow, UIElement 등 UI 표시 및 바인딩
- `TableLoader/` : 테이블 파서/캐시/리로드 플로우, 게임 데이터 진입점
- `Projectile/` : 발사체 컨트롤러/충돌 처리/경계 처리 등
- `Effect/`, `Sound/` : 이펙트/사운드 재생 및 로더 연동
- `Configs/`, `ScriptableSettings/` : 프로젝트/게임플레이 설정(ScriptableObject 중심)
- `Maps/`, `Dialogue/`, `SaveData/`, `Interaction/` : 맵, 대화, 저장, 상호작용 공통 기능 영역

Editor(CoreEditor)는 아래를 담당합니다.
- `GGemCoTool/` : UseProjectile/UseCrowdControl/UseEffect/… 같은 테스트/편집 툴
- `GGemCoProjectSetup/` : 자동 세팅/프로젝트 초기화 Step
- `GGemCoCreator/` : 에셋 생성 보조

## 3. 런타임 데이터 흐름(표준)

1) **Table 로딩**
- TableLoader 계층이 txt(또는 변환된 데이터)를 로딩/파싱
- 런타임은 “읽기 전용 데이터”로 사용 (에디터 툴에서만 수정/내보내기)

2) **설정 로딩**
- ScriptableObject 기반 Settings(예: PlayerActionSettings, SortingLayer Keys 등)
- 런타임은 참조만(저장/수정은 에디터에서)

3) **Addressables 로딩**
- Prefab/Effect/Sound 등은 Addressables 키 기반으로 로딩
- 로딩/해제는 반드시 짝을 맞추고, 캐시/레퍼런스 카운팅 정책을 문서화

## 4. 의존성 규칙

- Core Runtime은 **UnityEditor 네임스페이스를 참조하지 않습니다.**
- Editor 코드는 반드시 CoreEditor 어셈블리(또는 Editor 폴더/asmdef)로 분리합니다.
- 상위 패키지(Quest/Control/Affect/Skill/BT)는 Core를 의존할 수 있으나, Core는 상위 패키지에 의존하지 않습니다.
- Quest는 Core의 SceneGame, SaveRegistry, InteractionChoiceContributorRegistry, MonsterRespawnSuppressionPolicyRegistry 같은 공통 포트에 연결하되, Core Runtime은 Quest Runtime을 직접 참조하지 않습니다.

## 5. 확장 포인트(권장)

- “기능 추가”는 기존 대형 클래스에 누적하기보다,
  - 컨트롤러/핸들러 인터페이스 추가
  - 테이블/설정(SO) 추가
  - 작은 컴포넌트로 조합
  방식으로 확장합니다.

예)
- Projectile: `BoundaryMode` 같은 정책은 enum + 핸들러로 분리
- UI: UIWindow는 View/Binding 책임을 분리(값 계산은 Controller/Presenter)

## 6. 패키지 분리 메모

- Quest 전용 런타임 로직은 `quest_package_overview.md`에서 관리합니다.
- Core 문서에는 Quest가 사용할 공통 기반과 포트만 문서화합니다.
- Quest 관련 테이블/JSON/보상/HUD/에디터 툴 변경은 Quest 패키지 문서와 의존성 계약을 함께 갱신합니다.
