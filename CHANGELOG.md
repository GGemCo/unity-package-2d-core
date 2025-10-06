# CHANGELOG
All notable changes to this project will be documented in this file.  

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
This project adheres to [Semantic Versioning](https://semver.org/).

## [2.7.0] – 2025-10-02
### Added  
- 고정 위치 공격 오브젝트 기능 추가  
- 지연 데미지 트리거 기능  
- 이동 공격 트랩 기능  
- `Projectile` 관련 테이블 컬럼 연동 및 값 복사 기능  
- 툴 → Map: 프리팹 기반 트랩 생성 메뉴 추가  
- 이펙트 위치 / Layer 수정용 Editor 툴 기능 추가  

### Changed  
- `Projectile` 구조 개편: Base / 직선 / 곡선 / 레이저 클래스로 재구성  
- Map 툴의 워프 배치 초기화 로직 변경  

### Fixed  
- `AddressableLoaderSettings` null 처리 예외 보완  
- Monster 이동 속도 0일 경우 비정상 동작 보완  

---

## [2.6.0] – 2025-09-29
### Added  
- 사망 처리 로직 확장: `DeathReason` enum, `EndTilemapY`, 사망 팝업 기능  
- Input System 분기 로직 보완 (PreIntro / Intro / Scene)  
- Config / Addressable: Key / Path 관리 파일 분리  

### Changed  
- UI / UX: 버튼/윈도우/텍스트 처리 정밀화 (fade in/out, 초기 값 반영 등)  
- Scene / System: 데이터 삭제 후 버튼 표시 로직 개선  

### Fixed  
- Dialogue 툴 GUI 오류  
- Addressable 자동 등록 경고 버그  
- Null 참조 예외 보완  

---

## [2.5.0] – 2025-10-15
### Added  
- 캐릭터 y 좌표 < 0일 경우 사망 처리  
- `PlayerActionSettings` 분리 및 실시간 수치 적용 개선  
- 타일맵 드로우콜 디버그 및 물리 디버그 정보 표시 기능  
- MapEditor: 워프 배치 시 scale 저장 추가  

### Changed  
- 이벤트 / 몬스터 Delegate 구조 일부 변경  
- UI 로직 분리 및 리팩터링  

### Fixed  
- Quest/NPC 관련 버그  
- 점프 상태 처리 오류  
- Debug 툴의 중복 생성 문제  

---

## [2.4.0] – 2025-09-04
### Added  
- Character 이동 충돌 처리 개선 (FixedUpdate 연동)  
- 넉백 / 피격 애니메이션 루프 보강  
- UI / UX 개선: 옵션/조작 탭 리뉴얼, 토글/페이드 처리  
- Localization: UI 문구 현지화 확장  

### Changed  
- `TableLoaderManager` 접근 방식 변경  
- Control 패키지 연동 구조 개선  

### Fixed  
- 일부 문구나 리소스 누락 문제 (명시된 것은 없지만 안정화 중심)  

---

## [2.3.2] – 2025-08-07
### Added  
- `SoundSettings`에 UI 버튼 효과음 기본 항목 추가  
- Tool: Intro 씬 자동 `ClickSoundEventBroadcaster` 삽입  
- Addressable 툴 버튼 레이아웃 개선  

### Fixed  
- (없음 특별히 강조된 항목 없음)  

---

## [2.3.1] – 2025-08-07
### Added  
- Tool / Addressable: 맵 등록 시 `regen` 파일 읽고 label 자동 부착 복원  
- Tool / Settings: ScriptableObject 자동 생성 기능  
- 샘플 중복 String Table 제거  

### Fixed  
- (없음 강조된 항목 없음)  

---

## [2.3.0] – 2025-08-06
### Added  
- 사운드 시스템: UI 버튼/효과음, BGM vs 효과음 구분, 볼륨 설정  
- PreIntro 씬 추가  
- 툴 자동 설정 기능  
- Editor / WindowBase 클래스 도입  

### Changed  
- Character 기본 구조 이름 리팩터링  
- 옵션 / 설정 로딩 구조 변경  

### Fixed  
- Tool / Cutscene GUILayout 처리 오류  

---

## [2.2.0] – 2025-07-30
### Added  
- Spine2D 리소스 이벤트 이름 변경  
- 애니메이션 이벤트 처리 모듈화  
- Sprite 애니메이션 처리 기능  
- 테이블 구조 확장: Sorting Layer 컬럼, 위치 컬럼 등  
- 스킬 테이블 `ProjectileUid` 추가  
- 발사체 관리 매니저 추가  
- 방향 변수 (`facing`) 추가  
- 스킬 Duration / CoolTime 보정  

### Changed  
- 일부 테이블 컬럼 구조 변경  
- 리소스 / 애니메이션 처리 흐름 조정  

### Fixed  
- 캐릭터 장비 이미지 갱신 누락  
- Localization 포맷 오류  

---

## [2.1.2] – 2025-07-18
### Fixed  
- Localization 싱글톤 처리 문제 복원 (revert)  

---

## [2.1.1] – 2025-07-15
### Added  
- 공용 String Table + System String Table 패키지 포함  
- 샘플 UIWindow에 Localize 적용  
- 씬 설정 시 Localize 자동 적용  

---

## [2.0.1] – 2025-07-08
### Fixed  
- v2.0.0 릴리즈의 초기 버그 수정 (구체 항목 명시 없음)  

---

## [2.0.0] – 2025-06-30
### Added  
- 초기 버전: 패키지 기본 구조, 핵심 모듈, 리소스 / 테이블 시스템 등  
- 기본 사용자 인터페이스, 리소스 관리, 데이터 시스템 공개  

---

## [Links]  
- [Unreleased]: –  
- [2.7.0]: https://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.7.0  
- [2.6.0]: https://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.6.0  
- [2.5.0]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.5.0  
- [2.4.0]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.4.0  
- [2.3.2]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.3.2  
- [2.3.1]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.3.1  
- [2.3.0]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.3.0  
- [2.2.0]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.2.0  
- [2.1.2]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.1.2  
- [2.1.1]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.1.1  
- [2.0.1]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.0.1  
- [2.0.0]: https://github://github.com/GGemCo/unity-package-2d-core/releases/tag/v2.0.0  
