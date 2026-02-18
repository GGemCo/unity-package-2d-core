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


## 0. 공통 체크리스트(모든 변경)

- [ ] Runtime/Editor 분리 위반 여부 확인
- [ ] Addressables 로딩/해제 경로 확인
- [ ] 이벤트 구독/해제 짝 확인
- [ ] GC 할당(특히 Update) 유무 확인
- [ ] 변경된 테이블/설정이 Editor Tool에서도 갱신되는지 확인

---

## 1. 테이블 컬럼/행 추가(예: crowd_control, affect, skill 등 공통 기반)

1) 테이블 정의/구조체(`StruckTable*`) 수정
2) 파서/로더(TableLoader) 수정
3) 캐시/Dictionary 갱신 로직 수정
4) 에디터 툴(Use*)의 UI 필드 추가 + Undo 지원
5) Export/Save(txt 출력) 반영
6) 샘플 데이터 갱신
7) 런타임 사용처(컨트롤러/시스템) 반영
8) 테스트
- [ ] 로딩 성공/실패 로그 확인
- [ ] Export 후 재로드 시 동일하게 동작

---

## 2. 새로운 Projectile 정책 추가(예: 경계 반사, 타겟팅 확장)

1) 정책 enum 추가(예: BoundaryMode)
2) ProjectileController(또는 관련 핸들러) 확장 포인트에 연결
3) Inspector/EditorWindow(UseProjectile) 입력 UI 추가
4) 테이블 또는 설정(SO)에 저장할지 결정
5) 디버그 표시(Gizmo/DebugDraw) 옵션 제공
6) 테스트
- [ ] 반사 횟수/속도 배수/패딩 등 파라미터 조합 테스트
- [ ] 성능: 충돌/레이캐스트 호출 수 확인

---

## 3. UI 표시/바인딩 변경(예: 스탯 포인트 UI)

1) 데이터 소스(PlayerData/CharacterStat/세션) 변경
2) UI 갱신 트리거(이벤트/Dirty/Refresh) 확인
3) UIElement/Window에서 값 적용 위치(텍스트/버튼 interactable) 점검
4) PlayMode 테스트
- [ ] 레벨업/수치 변경 시 즉시 반영
- [ ] Apply/Reset 흐름이 정상 동작

---

## 4. Editor 툴 추가(UseX)

1) `GGemCoTool/` 아래 EditorWindow 생성
2) 대상 선택(씬 오브젝트) → 파라미터 입력 → 실행 버튼 흐름 구성
3) Undo/Redo
4) 테이블 리로드 버튼/데이터 Export 포함
5) 에러/가드(대상 없음/테이블 없음/잘못된 UID)
6) 메뉴 등록(MenuItem)
