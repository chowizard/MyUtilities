# v1.1.2 — 기존 구현 분석 (Research)

> 이 문서는 v1.1.2 기준 기존 프로젝트의 구조 및 동작을 분석한 결과물이다.

---

## 소스 구조

```
Summarizer/
├── Program.cs              # 진입점. Version 상수 보유
├── Configuration.cs        # 설정 데이터 클래스 (POCO)
├── FormMain.cs             # 핵심 로직 (모든 텍스트 변환 처리)
├── FormMain.Designer.cs    # WinForms 디자이너 생성 코드 (직접 편집 금지)
├── App.config              # 앱 설정 (직원명, 예약 확인 메시지, 양식 문자열 목록)
├── AppSettings.settings    # 설정 스키마 (디자이너 파일)
└── AppSettings.Designer.cs # 설정 접근자 자동 생성 코드 (직접 편집 금지)
```

---

## 핵심 로직 (`FormMain.cs`)

### 텍스트 변환 흐름

```
입력 텍스트
    └─> ConvertText()
            ├─ 카카오톡 메시지 시간 패턴 감지?
            │       ├─ YES: 각 단락을 화자 구분 (직원 vs 고객)
            │       │        ├─ 직원 메시지: ConvertStaffText()
            │       │        └─ 고객 메시지: ConvertCustomerText()
            │       └─ NO: 전체를 고객 메시지로 간주 → ConvertCustomerText()
            └─> " / " 로 합쳐서 최종 출력
```

### 고객 메시지 변환 (`ConvertCustomerText`)
- `[ ... ]` 대괄호로 감쌈
- 각 항목을 ` / ` 로 구분
- 양식 문자열(FormMessages) 제거 — 내용이 있을 때만
- 휴대전화번호 표준화 (`010-XXXX-XXXX`)
- 생년월일 표준화 (`YY-MM-DD`)

### 직원 메시지 변환 (`ConvertStaffText`)
- 대괄호 없음
- 불필요한 인삿말(SliceMessages) 제거
- 마지막 문장이 예약 메시지이면 → `/ {ReservationConfirmMessage} // {StaffName}` 자동 추가

---

## 정규표현식 (주요 패턴)

| 대상 | 정규식 위치 | 설명 |
|------|------------|------|
| 휴대전화번호 | `PhoneNumberRegex()` | `+82`, `82`, `010` 등 다양한 형식 인식 |
| 생년월일 | `BirthNumberRegex()` | 6자리~8자리, 년/월/일 구분자 다수 지원 |
| 카톡 메시지 시간 | `KakaoTalkMessageTimeRegex()` | `오전HH:MM`, `오후HH:MM` |
| 직원 메시지 식별 | `KakaoTalkStaffMessageRegex()` | `"님이 보냄 보낸 메시지 가이드"` |
| 예약 메시지 | `ReservationRegex()` | `N월 N일 요일 N시 N분 XXX원장님 상담 예약` |

---

## 설정 (`App.config`)

앱 설정은 `Summarizer.AppSettings.xml`에서 관리된다. 런타임에 `AppSettings.Default`를 통해 읽어 `Configuration` 객체에 로드된다.

| 설정 키 | 설명 | 기본값 예시 |
|---------|------|------------|
| `StaffName` | 직원 이름 (예약 메시지 끝에 추가) | `아무개` |
| `ReservationConfirmMessage` | 예약 확인 문자열 | `채널로 예약문자 전송` |
| `FormMessages` | 고객 양식의 고정 질문 문자열 목록 | `상담받을 분의 성함 / 연락처 - ` 등 |
| `SliceStaffMessages` | 직원 메시지에서 제거할 불필요 문구 목록 | `안녕하세요~`, `^^` 등 |

---

## 빌드

```bash
dotnet build Summarizer.csproj
dotnet build Summarizer.csproj -c Release
```

UI 동작 확인은 반드시 빌드 후 직접 실행해야 한다 (WinForms이므로 브라우저 테스트 불가).

---

## 버전 업데이트 시 수정해야 할 위치

버전을 올릴 때 아래 두 곳을 동기화해야 한다:

1. [Program.cs](../../Program.cs) — `internal const string Version`
2. [Summarizer.csproj](../../Summarizer.csproj) — `<FileVersion>` 및 `<AssemblyVersion>`

---

## 주의사항

- `FormMain.Designer.cs`, `AppSettings.Designer.cs`는 Visual Studio/디자이너가 자동 생성한다. **직접 편집하지 말 것.**
- 정규표현식은 `[GeneratedRegex]` 소스 제너레이터를 사용한다. .NET 8 이상 필수.
- `App.config`의 `FormMessages` / `SliceStaffMessages`는 배열 형태의 XML이다. 편집 시 스키마 구조 유지 필요.
- `SummarizeTextContents`는 양식 문자열이 내용과 **정확히 일치하는 경우**(내용 없음)에는 제거하지 않는다 — 의도된 동작임.
