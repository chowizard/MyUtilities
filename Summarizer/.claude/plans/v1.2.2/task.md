# Summarizer v1.2.2 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.

---

## Story 1 — 직원 이름 입력 UI

### [ToDo] 1-1. `AppSettings.cs` 수정

- `StaffName` 속성: `{ get; init; }` → `{ get; set; }`

### [ToDo] 1-2. `MainWindowViewModel.cs` 수정

- `StaffName` 바인딩 프로퍼티 추가
  - get: `settings.StaffName` 반환
  - set: `settings.StaffName = value` → `AppSettingsLoader.Save()` → `OnPropertyChanged()`

### [ToDo] 1-3. `MainWindow.xaml` 수정

- 본문 `Grid`에 `RowDefinition` 추가 (Row 0: Auto / Row 1: *)
- 기존 컬럼 요소 전부에 `Grid.Row="1"` 추가
- Row 0, Column 0에 직원 이름 입력 StackPanel 추가
  - Label: "직원 이름"
  - TextBox: `{Binding StaffName, UpdateSourceTrigger=LostFocus}`
- Title: `"Summarizer (v1.2.1)"` → `"Summarizer (v1.2.2)"`

---

## Story 2 — 배치 파일 변환 코어

### [ToDo] 2-1. `BatchFileConverter.cs` 신규 생성 (Summarizer.Core)

- `BatchFileConverter(AppSettings settings)` 생성자
- `void ConvertFile(string inputFilePath)` 공개 메서드
  - 파일 읽기 (UTF-8 No BOM)
  - `ParseConversations()` 호출
  - 각 블록 `MessageConverter.Convert()` 변환
  - 출력 파일 경로 생성 (`BuildOutputFilePath()`)
  - 결과를 한 줄씩 파일 저장 (UTF-8 No BOM)
- `static List<string> ParseConversations(string text)` 비공개 메서드
  - 줄 단위 상태 기계: `[` → 수집 시작, `]` → 수집 종료 및 emit, 그 외 → 수집 중이면 추가
- `static string BuildOutputFilePath(string inputFilePath)` 비공개 메서드
  - `{디렉토리}\{이름}-converted{확장자}`

---

## Story 3 — CLI 지원

### [ToDo] 3-1. `App.xaml.cs` 수정

- `OnStartup`에 args 분기 추가
  - `e.Args.Length > 0` → 배치 모드: `ShutdownMode.OnExplicitShutdown` 설정 → `RunBatchMode()` → `Shutdown()`
  - 그 외 → 기존 GUI 모드
- `RunBatchMode(string inputFilePath)` 비공개 메서드 추가
  - try-catch로 `BatchFileConverter.ConvertFile()` 실행
  - 예외 발생 시 `MessageBox.Show(오류 메시지)`

---

## Story 4 — 버전 업데이트

### [ToDo] 4-1. 버전 v1.2.2로 업데이트

- `App.xaml.cs`: `Version = "1.2.2"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.2)"` (Story 1-3에서 함께 처리)
- `Summarizer.App.csproj`: `<FileVersion>`, `<AssemblyVersion>` → `1.2.2`
- `Summarizer.Core.csproj`: `<FileVersion>`, `<AssemblyVersion>` → `1.2.2`

---

## Story 5 — 빌드 및 동작 검증

### [ToDo] 5-1. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [ToDo] 5-2. 동작 검증

- GUI 모드: 직원 이름 입력 → 포커스 이동 → `AppSettings.json` 반영 확인
- CLI 모드: 테스트용 입력 파일 → 출력 파일 생성 확인
- CLI 오류: 존재하지 않는 파일 경로 → MessageBox 표시 확인
