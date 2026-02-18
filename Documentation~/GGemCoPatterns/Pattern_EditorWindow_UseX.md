# Pattern_EditorWindow_UseX (Golden: UseProjectile)

골든 샘플:
- GGemCo2DCoreEditor.GGemCoTool.Test.UseProjectile (core_editor.zip: GGemCoTool/Test/UseProjectile.cs)

목표:
- **테이블 기반 데이터**를 EditorWindow에서 즉시 수정/테스트하고, txt로 Export(저장)까지 일관되게 제공
- Undo/Redo 지원
- “대상 선택”을 최상단에 배치(플레이어/몬스터/프리팹 등)

---

## 1) 폴더/어셈블리 규칙

- 위치: `*/Editor/GGemCoTool/...`
- 네임스페이스: `...Editor`
- Runtime 코드/테이블 파서는 `...Runtime`을 사용하고, Editor에서만 편집/Export 수행

(런타임/에디터 분리는 Unity asmdef 권장)

---

## 2) 표준 UI 레이아웃

1. **대상 선택(상단 고정)**
   - Scene에서 캐릭터/오브젝트 선택
   - 대상이 없으면 나머지 UI는 비활성 + 가드 메시지

2. **데이터 소스 선택**
   - 테이블 Row 선택(검색 가능한 드롭다운 권장)
   - Row가 바뀌면 “편집 세션”을 새로 구성

3. **편집 필드**
   - 단일 Row의 컬럼을 직접 편집
   - 값 검증(범위/enum/key 존재)을 즉시 수행

4. **즉시 테스트 버튼**
   - 선택한 Row 기준으로 런타임에 즉시 적용(예: 발사체 발사, CC 적용)

5. **테이블 리로드 / Export**
   - Reload: txt를 다시 읽어서 캐시 갱신
   - Export: 현재 편집 내용을 txt로 내보내기(저장)

---

## 3) 데이터 편집 세션 표준(Undo/Redo 포함)

- “편집 중인 Row”는 별도의 Session 객체로 관리(Dirty 추적)
- Undo는 아래 중 하나로 구현
  - `Undo.RecordObject`(SO/UnityEngine.Object 대상) + Apply
  - 또는 “직렬화 가능한 임시 객체”를 Undo 스택에 저장

권장:
- Dirty가 true일 때만 Apply/Export 버튼 활성화
- 값이 같으면 Dirty false 유지

---

## 4) 예외/가드 정책

- 테이블/로더가 없으면: 명확한 안내 + 버튼 비활성
- UID/Key 누락: 에러 로그 + UI 경고
- 대상 오브젝트가 없으면: 테스트 기능 비활성

---

## 5) 성능/유지보수

- OnGUI/UITK에서 매 프레임 테이블 전체 재빌드 금지
- 이름 목록/UID 목록은 캐시하고, Reload 시에만 갱신

---

## 6) 체크리스트

- [ ] 대상 선택이 상단에 있고, 대상 없음 처리됨
- [ ] Row 변경 시 세션 갱신/Dirty 리셋
- [ ] Undo/Redo 정상 동작
- [ ] Reload/Export 정상 동작
- [ ] Export 후 재로드해도 동일 데이터 유지
