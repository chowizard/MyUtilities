# Summarizer v1.2.3 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.

---

## Story 1 — `replaceMessages` → `replaceStaffMessages` 이름 변경 및 하위 호환

### [완료] 1-1. `AppSettings.cs` 수정

- `ReplaceMessages` → `ReplaceStaffMessages` (이름 변경)
- `{ get; init; }` → `{ get; set; }` (ReplaceStaffMessages, ReservationConfirmMessage, FormMessages)

### [완료] 1-2. `AppSettingsLoader.cs` 수정

- 하위 호환 마이그레이션: 로드 후 `replaceMessages` 구 키 감지 → `ReplaceStaffMessages`로 이전

### [완료] 1-3. `MessageConverter.cs` 수정

- `settings.ReplaceMessages` → `settings.ReplaceStaffMessages` 변경

### [완료] 1-4. `AppSettings.json` 수정

- `"replaceMessages"` → `"replaceStaffMessages"` 키 이름 변경

---

## Story 2 — `StandardizeBirthNumber` ON/OFF 옵션

### [완료] 2-1. `AppSettings.cs` 수정

- `NormalizeBirthNumber : bool = false` 속성 추가

### [완료] 2-2. `AppSettings.json` 수정

- `"normalizeBirthNumber": false` 항목 추가

### [완료] 2-3. `MessageConverter.cs` 수정

- `ConvertCustomerText`, `ConvertStaffText` 양쪽: `else` → `else if (settings.NormalizeBirthNumber)` 변경

---

## Story 3 — `formMessages` 정규표현식 지원

### [완료] 3-1. `MessageConverter.cs` 수정

- `FormMatcher` private record struct 추가
- `formMatchers` 필드 및 `ParseFormMatcher()` 추가
- `SummarizeTextContents()` 리팩토링: FormMatcher 기반으로 교체
- `FormMatcherMatches()`, `ApplyFormMatcher()` 헬퍼 추가

---

## Story 4 — `AppSettingsDialog` 구현

### [완료] 4-1. `FormMessageItemViewModel.cs` 신규 생성

- `IsSelected`, `Text`, `IsRegex` 프로퍼티 + `INotifyPropertyChanged`

### [완료] 4-2. `ReplaceStaffMessageItemViewModel.cs` 신규 생성

- `IsSelected`, `HasComment`, `Comment`, `Pattern`, `IsPatternRegex`, `HasReplacement`, `Replacement`, `IsReplacementRegex` 프로퍼티
- `Comment` / `Replacement` setter: 값이 비어있으면 `HasComment` / `HasReplacement` → false

### [완료] 4-3. `AppSettingsDialogViewModel.cs` 신규 생성

- 생성자에서 `AppSettingsLoader.Load()` 호출
- `ReservationConfirmMessage`, `NormalizeBirthNumber`, `FormMessages`, `ReplaceStaffMessages` 바인딩 프로퍼티
- 각 배열 항목 조작 커맨드 (추가 / 삭제 / 위로 / 아래로 / 모두선택) — FormMessages, ReplaceStaffMessages 양쪽
- `CloseCommand`: SaveToSettings() → AppSettingsLoader.Save() → CloseRequested 이벤트 발행
- `OpenJsonFileCommand`: Process.Start로 AppSettings.json 텍스트 에디터 열기

### [완료] 4-4. `AppSettingsDialog.xaml` + `AppSettingsDialog.xaml.cs` 신규 생성

- 레이아웃: DockPanel (상단 툴바 / 하단 닫기 버튼 / 중앙 ScrollViewer)
- 테마 ResourceDictionary: 생성자에서 외부로부터 Uri 수신하여 적용
- 예약 확인 텍스트, 생년월일 표준화 체크박스 섹션
- 상담 메시지 형식(formMessages) 섹션: 아이템별 CheckBox + TextBox + 정규표현식 CheckBox
- 직원 메시지 변환(replaceStaffMessages) 섹션: 아이템별 세로 카드 레이아웃

### [완료] 4-5. `App.xaml.cs` 수정

- `ResolveThemeUri()` → `internal static`으로 변경 (MainWindowViewModel에서 접근 가능하도록)

### [완료] 4-6. `MainWindowViewModel.cs` 수정

- `converter` 필드: `readonly` 제거
- `OpenSettingsFileCommand`: AppSettingsDialog 열기로 교체
- Dialog 종료 후: `AppSettingsLoader.Load()` + `MessageConverter` 재생성

---

## Story 5 — 버전 업데이트 및 빌드 검증

### [완료] 5-1. 버전 v1.2.3으로 업데이트

- `App.xaml.cs`: `Version = "1.2.3"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.3)"`
- `Summarizer.App.csproj`: `1.2.2` → `1.2.3`
- `Summarizer.Core.csproj`: `1.2.2` → `1.2.3`

### [완료] 5-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [완료] 5-3. 동작 검증

- 구 `AppSettings.json` (replaceMessages 키) 로드 → 마이그레이션 확인
- `formMessages` regex 항목 동작 확인
- `파일 > 설정` → AppSettingsDialog 열림, 편집, 저장 확인
- 기존 기능 정상 동작 확인

---

## 버그 수정

### [완료] BugFix-1. `KakaoTalkMessageTimeRegex` — 대화 내 시간 표현 오매칭 수정

- **증상**: 대화 내용에 "오전10:30에 오겠습니다" 같은 시간 표현이 줄 끝에 위치할 때, KakaoTalk 타임스탬프로 오인식하여 변환 결과가 의도치 않게 분리됨
- **원인**: 기존 정규표현식 `오(전|후)\d{2}:\d{2}(\r?\n)`은 줄 내 위치와 무관하게 패턴을 매칭함
- **수정**: `^` 앵커와 `RegexOptions.Multiline` 추가
  - 변경 전: `오(전|후)\d{2}:\d{2}(\r?\n)`
  - 변경 후: `^오(전|후)\d{2}:\d{2}(\r?\n)` (Multiline)
  - KakaoTalk Business 타임스탬프는 항상 줄 맨 앞에 단독으로 위치하므로, 이 조건으로 충분히 구분됨
- **수정 파일**: `Summarizer.Core/MessageConverter.cs`
