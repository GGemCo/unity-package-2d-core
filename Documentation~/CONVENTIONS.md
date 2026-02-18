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


## 1. 네이밍/네임스페이스

- Runtime: `GGemCo2DCore`
- Editor: `GGemCo2DCoreEditor`
- Editor 전용 타입은 `...Editor` 네임스페이스 하위로만 둡니다.

## 2. Runtime/Editor 분리

- Runtime 폴더에는 `UnityEditor` 의존 금지
- EditorWindow/CustomTool은 Editor 폴더 및 Editor asmdef에 위치
- 패키지 단위로 asmdef를 사용하는 것을 권장합니다(런타임/에디터 별도 asmdef).  
  참고: {unity_links['asmdef']}

## 3. 데이터(테이블) 규칙

- 테이블 구조 변경은 “파서/구조체/캐시/툴/샘플 데이터”까지 한 번에 갱신합니다.
- 테이블 키/UID는 “런타임 안정성”을 위해 불변을 전제로 합니다.
- 파싱 실패는 조용히 무시하지 말고, **에러 로그 + 안전한 기본값**을 적용합니다.

## 4. ScriptableObject 규칙

- ScriptableObject는 “공유/불변 데이터” 또는 “설정”으로 사용합니다.
- 런타임 빌드에서 ScriptableObject에 저장(쓰기)은 불가/비권장입니다.  
  참고: {unity_links['scriptableobject']}

## 5. Addressables 규칙

- 로딩한 자산은 반드시 Release/Dispose 경로를 제공해야 합니다.
- “키 문자열”은 Config 또는 Keys 클래스로 중앙집중 관리합니다.
- 대형 프로젝트일수록 로딩 정책(캐시/프리로드/해제 시점)을 문서화합니다.  
  참고: {unity_links['addressables_overview']}

## 6. 이벤트/구독 해제

- 구독/해제는 항상 짝을 맞춥니다.
- MonoBehaviour 생명주기: `OnEnable` 구독 / `OnDisable` 해제 또는 `OnDestroy` 해제
- 순수 C# 컨트롤러는 `IDisposable` 또는 명시적 `Dispose()`를 권장합니다.

## 7. 성능/GC

- Update/FixedUpdate에서 할당이 발생하는 LINQ, 문자열 결합을 최소화합니다.
- Physics2D 쿼리는 `RaycastNonAlloc` 등 NonAlloc을 우선합니다.
- UI 갱신은 “값이 바뀐 경우에만” 수행(Dirty 플래그/이벤트 기반).

## 8. 에디터 툴(Use*) 규칙

- EditorWindow는 Undo/Redo를 지원해야 합니다.  
  참고: {unity_links['undo']}
- 테이블 수정은 Prefs가 아니라 “원본 데이터(txt) 내보내기”로 귀결되도록 설계합니다.
- 대상 선택(플레이어/몬스터 등)은 툴 상단에 배치하고, 런타임 오버라이드/읽기전용 모드는 최소화합니다.
