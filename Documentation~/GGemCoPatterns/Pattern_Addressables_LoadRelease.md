# Pattern_Addressables_LoadRelease (Golden: AddressableLoader 계열)

목표:
- Addressables 로딩/해제가 누락되지 않도록 “정책”을 고정

---

## 1) 키 관리

- 키 문자열을 코드에 흩뿌리지 않고,
  - `Config*.Keys` 또는 전용 Keys 클래스로 중앙화

---

## 2) Load/Release 규칙

- Load한 핸들은 반드시 Release
- 캐시를 둔다면 참조 카운팅 정책을 명확히
- 씬 전환/풀링 오브젝트 등에서 누수가 발생하기 쉬우므로 테스트 케이스를 포함

---

## 3) 체크리스트

- [ ] 로딩 실패 로그 + fallback(가능하면)
- [ ] 반복 진입/종료 시 메모리 증가 없음
