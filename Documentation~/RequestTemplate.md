# 변경 요청 템플릿 (AI 요청 표준)

아래 형식을 복사하여 사용합니다.  
목표는 **요청(모델 호출) 횟수를 줄이고**, 한 번에 “정확한 변경”을 얻는 것입니다.

---

## [목표]
- (한 줄로) 무엇을 추가/변경/수정?

## [대상 패키지]
- Core / Control / Affect / Skill / AI_BT 중 선택

## [적용 패턴]
- Golden Pattern: (예: Pattern_EditorWindow_UseX)
- 참고 골든 샘플: (예: UseProjectile)

## [영향 범위]
- 변경 파일(최대 3개):
  - ...
- 신규 파일(최대 3개):
  - ...

## [데이터/설정 변경]
- 테이블 변경: (있음/없음) → 파일명/컬럼/샘플 데이터
- ScriptableObject 변경: (있음/없음) → SO 명/필드
- Addressables 변경: (있음/없음) → 키/그룹/프리팹

## [규칙]
- Dependency Contract 준수(Core→Control→Skill→AI_BT)
- Runtime/Editor 분리 유지
- 대형 클래스 수정 최소화(확장 포인트/핸들러/어댑터 우선)
- Undo/Redo(에디터) 지원 필요 여부

## [검증 시나리오] (3개 이내)
1) ...
2) ...
3) ...

## [산출물]
- 변경/추가 파일만 (압축)
- 변경 요약(불릿)
