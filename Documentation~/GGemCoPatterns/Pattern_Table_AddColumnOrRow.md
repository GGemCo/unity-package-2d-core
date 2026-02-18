# Pattern_Table_AddColumnOrRow (Golden: Core TableLoader + Use* Tools)

골든 샘플 참고:
- Core Runtime: TableLoaderBase / TableLoaderManager / TableRegistry (core.zip: TableLoader/*)
- Editor Loader: TableLoaderManagerBase (core_editor.zip: GGemCoTool/TableLoader/*)
- Editor Tool: UseProjectile (core_editor.zip)

목표:
- 테이블 변경이 “로딩/캐시/툴/Export/샘플 데이터/런타임 사용처”까지 **한 번에** 반영되도록 절차를 고정

---

## 변경 순서(고정)

1) Table 파일 스펙 변경
- 컬럼 추가/삭제/의미 변경 문서화

2) Runtime Table 클래스/구조체 수정
- `TableX` / `StruckTableX` / 파싱 로직 반영

3) Loader/Registry 갱신
- 로딩/캐시(Dictionary) 구축/갱신 로직 반영

4) Editor Loader 반영
- 에디터에서 Reload 가능한 경로 제공

5) Use* Tool UI 반영
- 필드 노출 + Undo + 즉시 테스트

6) Export 반영
- txt 저장 시 신규 컬럼 포함

7) 샘플 데이터 갱신
- 최소 1~3줄 샘플 추가(정상/경계 케이스 포함)

8) 런타임 사용처 반영
- 새로운 컬럼 의미가 실제 로직에 반영되는지 확인

---

## 체크리스트

- [ ] Reload → UI 목록 갱신
- [ ] Export → 재로드 후 동일
- [ ] 누락/잘못된 값 처리(로그 + 안전 기본값)
