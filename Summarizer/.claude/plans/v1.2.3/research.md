## Summarizer v1.2.1 계획

---

## 기본사항

- 사용 버전 : v1.2.3
- 이전 버전 : v1.2.2
- 이전 버전에서 작업했던 내용들을 이어서, 여기에서 작업해라.

---

## 목표사항

- `AppSettings.json`의 `replaceMessages` 항목을 `replaceStaffMessages`로 변경
  - 축약 및 대체는 오직 직원의 메시지 텍스트만 가능하고, 고객의 메시지는 있는 그대로 옮겨야 하는 정책 때문임.
  - 기존에 배포한 `AppSettings.json`의 `replaceMessages`도 호환 가능하게 해야 한다.
  - 다만, `AppSettings.json`을 다시 저장할 때는 `replaceStaffMessages` 저장하면 됨.
  
- 생년월일 형식의 표준화 처리의 사용 중단
  - `MessageConverter.StandardizeBirthNumber()`를 코드에서 사용하는 부분 제거
  - 다만, 기능 자체를 삭제하지는 않는다.
  - ON / OFF 옵션을 제공할 수 있다면, 그렇게 해도 된다. (단, 이 경우에는 GUI + `AppSettings.json` 환경설정 모두 지원해야 한다.)

- `AppSettings.json`의 `formMessages` 에도 정규표현식 지원 가능하게 수정
  - `replaceStaffMessages`(=`replaceMessages`)와 마찬가지로, "regex:" 구문을 통해 지원

- `AppSettings.json`의 일부 내용을 GUI에서 편집할 수 있는 기능 제공
  - 기존의 `파알` > `설정` 메뉴를 이제 이 기능으로 통합
  - `파알` > `설정` 메뉴 선택 시, 편집을 위한 Dialog GUI를 출력 (이후부터 `AppSettingsDialog`로 부르겠다.)

- `AppSettingsDialog` 명세
  - Dialog를 열 때, `AppSettings.json`을 불러온다.
  - Dialog를 닫을 때, 현재까지 편집한 내용을 `AppSettings.json`에 저장한다.
  - Dialog 및 부속 UI 컨트롤들의 색상 체계는 `AppSettings.json`에 지정한 UI 테마에 따른다.
  - v1.2.2 이하에서 기본 텍스트 에디터를 통해 `AppSettings.json`을 직접 편집할 수 있는 수단을 제공한다. (버튼도 좋고, 추천할만한 다른 방식도 좋음.)
  - 편집 가능한 항목은 다음과 같다.
    - `reservationConfirmMessage` => GUI에서는 `예약 확인 텍스트` Label로 표현
    - `formMessages` => GUI에서는 `상담 메시지 형식` Label로 표현
    - `replaceStaffMessages`(=`replaceMessages`) => GUI에서는 `직원 메시지 변환` Label로 표현
      - `comment` => GUI에서는 `메모` Label로 표현
      - `pattern` => GUI에서는 `찾을 메시지` Label로 표현
      - `replacement` => GUI에서는 `바꿀 메시지` Label로 표현
  - 배열로 구성하는 항목들은 다음과 같은 조작이 가능해야 한다. 
    - 항목 선택 기능
      - 한개 또는 여러 개 선택 가능해야 한다.
      - 배경 컨트롤 색상의 변경 또는 checkbox 등의 방법으로 구현
    - `추가` : 새로운 항목을 맨 뒤에 추가
    - `삭제` : 선택한 항목들을 삭제
    - `위로` : 선택한 항목을 배열의 이전 인덱스로 이동
      - 단일 선택에서만 사용할 수 있다. 
      - 가장 처음 항목은 이 명령을 사용할 수 없다.
      - 내부 구현에서는 data swap 등으로 최적화하여 처리해도 무방
    - `아래로` : 선택한 항목을 배열의 다음 인덱스로 이동
      - 단일 선택에서만 사용할 수 있다. 
      - 가장 마지막 항목은 이 명령을 사용할 수 없다.
      - 내부 구현에서는 data swap 등으로 최적화하여 처리해도 무방
    - `모두선택` : 모든 항목을 선택한다.
  - 정규표현식 지원하는 텍스트 설정 항목들은 `정규표현식` checkbox로 설정파일에 "regex:" 사용 여부를 구분
  - `직원 메시지 변환` GUI (=`replaceStaffMessages`)
    - `메모`(=`comment`) 항목
      - 기본 상태는 `추가` checkbox만 있는 상태 (기본값 체크하지 않음.)
      - `추가` checkbox를 체크하지 않음 : `comment` 없는 상태
      - `추가` checkbox를 체크함. 텍스트 입력 컨트롤 나타남.
      - 텍스트 입력 컨트롤의 내용을 비우고 포커스를 잃으면, 다시 원래대로 `추가` 체크 버튼으로 감춘다.
    - `찾을 메시지`(=`pattern`) 항목
      - 기본적으로 텍스트 입력 컨트롤 + `정규표현식` checkbox 제공
    - `바꿀 메시지`(=`replacement`) 항목
      - 기본 상태는 `추가` checkbox만 있는 상태 (기본값 체크하지 않음.)
      - `추가` checkbox를 체크하지 않음 : 빈 문자열로 치환
      - `추가` checkbox를 체크 : 텍스트 입력 컨트롤 + `정규표현식` checkbox 제공(정규표현식 캡처 지원용) 나타남.
      - 텍스트 입력 컨트롤의 내용을 비우고 포커스를 잃으면, 다시 원래대로 `추가` 체크 버튼으로 감춘다.

---