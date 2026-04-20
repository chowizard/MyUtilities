# Summarizer v1.2.0 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.

---

## Story 1 — 솔루션 구조 재편

### [완료] 1-1. 디렉토리 생성 및 파일 이동

- `Summarizer.Core/`, `Summarizer.App/ViewModels/`, `Summarizer.App/Commands/` 디렉토리 생성
- `icon.ico` → `Summarizer.App/icon.ico` 복사

### [완료] 1-2. `Summarizer.sln` 재구성

- 기존 `Summarizer.csproj` 참조 제거
- `Summarizer.Core` (GUID: A1B2C3D4-...), `Summarizer.App` (GUID: B2C3D4E5-...) 두 프로젝트 등록

### [완료] 1-3. `.gitignore` 추가

- `requirements.txt` git 추적 해제 (`git rm --cached requirements.txt`)
- `.gitignore` 생성: `requirements.txt`, `bin/`, `obj/`, `.vs/`, `.idea/`, `*.user`, `.claude/.edit-baks/`

---

## Story 2 — `Summarizer.Core` 프로젝트 구성

### [완료] 2-1. `Summarizer.Core.csproj` 생성

- TargetFramework: `net10.0` (Windows 비의존)

### [완료] 2-2. `AppSettings.cs` 구현

- `{ get; init; }` 불변 프로퍼티, 기본값 인라인 정의
- `string[]` 타입 (StringCollection 완전 제거)

### [완료] 2-3. `AppSettingsLoader.cs` 구현

- `static Load(string filePath)` — 파일 없을 시 기본값 인스턴스로 파일 자동 생성 후 반환
- `IndentSize = 4`, `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` 적용
  → 자동 생성 시 한글이 `\uXXXX` 이스케이프 없이 plain text로 저장됨

### [완료] 2-4. `MessageConverter.cs` 구현

- `FormMain.cs`의 변환 로직 전체 이전 (메서드명 동일 유지)
- 공개 메서드: `string Convert(string text)`
- `[GeneratedRegex]` 정규표현식 5개 그대로 이전
- UI 의존성 없음 (Windows 비의존 검증됨)

---

## Story 3 — `Summarizer.App` 프로젝트 구성

### [완료] 3-1. `Summarizer.App.csproj` 생성

- TargetFramework: `net10.0-windows`, `UseWPF: true`
- `AppSettings.json`: `CopyToOutputDirectory = PreserveNewest`
- `ProjectReference`: `Summarizer.Core`

### [완료] 3-2. `AppSettings.json` 생성

- `Summarizer.App/` 루트에 배치, `AppSettings.cs` 기본값과 동일한 내용
- 들여쓰기 4칸 space 적용
- 한글 값이 유니코드 이스케이프 없이 plain text로 저장

### [완료] 3-3. `Commands/RelayCommand.cs` 구현

- `ICommand` 직접 구현 (외부 라이브러리 없음)
- `CanExecuteChanged`: `CommandManager.RequerySuggested` 위임

### [완료] 3-4. `ViewModels/MainWindowViewModel.cs` 구현

- `INotifyPropertyChanged` 구현
- C# 14 `field` 키워드로 `InputText`, `OutputText`, `IsAlwaysOnTop` 프로퍼티 구현
- CLAUDE.md 중괄호 대칭 규칙 적용: `get { return field; }` + `set { ... }` 대칭 스타일
- `ConvertCommand`, `ClearCommand`, `CopyOutputCommand` 구현

### [완료] 3-5. `App.xaml` / `App.xaml.cs` 구현

- `StartupUri` 미사용, `Startup="OnStartup"` 이벤트로 ViewModel 주입
- `App.xaml.cs`: `Version = "1.2.0"` 상수, 설정 로딩 → ViewModel → Window 생성 순서

### [완료] 3-6. `MainWindow.xaml` / `MainWindow.xaml.cs` 구현

- 3열 Grid 레이아웃 (입력 / 버튼 / 출력)
- `Topmost="{Binding IsAlwaysOnTop}"` 바인딩
- `MainWindow.xaml.cs`: `DataContext = viewModel` 설정만 수행

---

## Story 4 — 구 파일 제거

### [완료] 4-1. 구 WinForms 및 설정 파일 삭제

삭제 완료:
- `Program.cs`, `Configuration.cs`
- `FormMain.cs`, `FormMain.Designer.cs`, `FormMain.resx`
- `App.config`, `AppSettings.settings`, `AppSettings.Designer.cs`
- `Summarizer.csproj`, `Summarizer.csproj.user`

---

## Story 5 — 빌드 및 동작 검증

### [완료] 5-1. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0
- `Summarizer.Core` 단독 빌드 성공 (net10.0, Windows 비의존 확인)

### [완료] 5-2. AppSettings.json 자동 생성 검증

- 출력 디렉토리에서 `AppSettings.json` 삭제 후 앱 실행
- 앱 정상 시작 및 `AppSettings.json` 기본값으로 자동 생성 확인

### [완료] 5-3. UI 및 기능 동작 검증

- 가공된 예제 시나리오로 변환 결과 확인 (v1.1.2와 동일한 출력 형식)
- "항상 맨 위로 고정" / "복사하기" / "비우기" 동작 확인

### [완료] 5-4. 실행파일 출력 위치

- `Directory.Build.props`를 솔루션 루트에 추가하여 출력 경로 통합
- bin: `Summarizer/bin/{Debug|Release}/{net10.0|net10.0-windows}/`
- obj: `Summarizer/obj/{Summarizer.Core|Summarizer.App}/{Debug|Release}/{framework}/`
  - `project.assets.json` 충돌 방지를 위해 obj 경로에 프로젝트명 포함 (`$(MSBuildProjectName)`)
- 최종 빌드 검증: Debug/Release 모두 경고 0, 오류 0
