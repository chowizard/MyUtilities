## Summarizer v1.2.1 계획

---

## 기본사항

- 사용 버전 : v1.2.1
- 이전 버전 : v1.2.0
- 이전 버전에서 작업했던 내용들을 이어서, 여기에서 작업해라.

---

## 목표사항

- 리펙토링
  - CLAUDE.md에 개정한 규칙사항을 반영하여, 프로젝트 코드의 스타일을 다듬을 것.

- `AppSettings.json`의 `sliceStaffMessages` 항목을 `replaceMessages` 항목으로 대체할 것.
  - 기존의 `sliceStaffMessages`
    - 기능 삭제. 주 기능들은 `replaceMessages`의 기능으로 대체함.
  - 대체할 `replaceMessages` 객체
    - 주어진 텍스트를 (주로 축약 및 생략을 위한) 다른 텍스트로 치환하는 기능
    - 기존의 `sliceStaffMessages` 기능은 `replaceMessages`에서 해당 메시지를 빈 문자열로 치환하는 방법으로 지원한다.
    - 정규표현식 지원
      - 검색 대상(`pattern`)에는 `sliceStaffMessages`와 마찬가지로 `regex:` 접두사를 사용하는 정규표현식 텍스트를 지원한다.
      - 치환값(`replacement`)은 plain text여도 무방하나, 정규표현식 캡처 그룹 참조(`$1` 등)도 지원한다.

- 앱에 메뉴 바 UI를 추가
  - 파일 탭
    - 설정 : AppSettings.json을 기본 텍스트 에디터로 열게 한다.
    - 끝내기 : 앱을 종료한다.
  - 보기 탭
    - 항상 맨 위로 고정 : `항상 맨 위로 고정` CheckBox와 연동
    - 테마 : 앱에 테마 설정 적용 (라이트 / 다크 / 시스템 설정)
  - 도움말 탭
    - 정보 : 앱 설명 및 버전 정보 표시

- 다크 / 라이트 테마 적용
  - 라이트 테마
  - 다크 테마
  - 시스템 설정에 따름 (기본값)

---