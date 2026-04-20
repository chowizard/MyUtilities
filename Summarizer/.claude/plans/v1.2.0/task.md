# Summarizer v1.2.0 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.

---

## Story 1 — 솔루션 구조 재편

### [신규] 1-1. 디렉토리 생성 및 파일 이동

- `Summarizer.Core/` 디렉토리 생성
- `Summarizer.App/` 디렉토리 생성
- `icon.ico` → `Summarizer.App/icon.ico` 이동

### [신규] 1-2. `Summarizer.sln` 재구성

- 기존 `Summarizer.csproj` 참조 제거
- `Summarizer.Core`, `Summarizer.App` 두 프로젝트 등록

### [신규] 1-3. `.gitignore` 추가

- `requirements.txt` 추적 해제 (`git rm --cached`)
- `.gitignore` 생성 (requirements.txt, bin/, obj/, .vs/, .idea/, *.user, .claude/.edit-baks/ 포함)

---

## Story 2 — `Summarizer.Core` 프로젝트 구성

### [신규] 2-1. `Summarizer.Core.csproj` 생성

### [신규] 2-2. `AppSettings.cs` 구현

### [신규] 2-3. `AppSettingsLoader.cs` 구현

### [신규] 2-4. `MessageConverter.cs` 구현

---

## Story 3 — `Summarizer.App` 프로젝트 구성

### [신규] 3-1. `Summarizer.App.csproj` 생성

### [신규] 3-2. `AppSettings.json` 생성

### [신규] 3-3. `Commands/RelayCommand.cs` 구현

### [신규] 3-4. `ViewModels/MainWindowViewModel.cs` 구현

### [신규] 3-5. `App.xaml` / `App.xaml.cs` 구현

### [신규] 3-6. `MainWindow.xaml` / `MainWindow.xaml.cs` 구현

---

## Story 4 — 구 파일 제거

### [신규] 4-1. 구 WinForms 및 설정 파일 삭제

- `Program.cs`, `Configuration.cs`
- `FormMain.cs`, `FormMain.Designer.cs`, `FormMain.resx`
- `App.config`, `AppSettings.settings`, `AppSettings.Designer.cs`
- `Summarizer.csproj`

---

## Story 5 — 빌드 및 동작 검증

### [신규] 5-1. 빌드 검증

### [신규] 5-2. AppSettings.json 자동 생성 검증

### [신규] 5-3. UI 및 기능 동작 검증
