# Summarizer v1.2.1 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.

---

## Story 1 — 리팩토링

### [완료] 1-1. `MessageConverter.cs` 중괄호 비대칭 수정

- `Convert()` 내부 `else` 구문에 `{ }` 블록 추가

### [완료] 1-2. `MainWindowViewModel.cs` 스타일 수정

- `ClearCommand` 람다 내 복수 구문 줄 분리
- `InputText.get` 들여쓰기 오류 수정 (5칸 → 4칸)
- 후행 공백 및 `this.` 불필요한 한정자 제거 (`this.converter` → `converter`)

---

## Story 2 — `sliceStaffMessages` 정규표현식 지원

### [완료] 2-1. `MessageConverter.cs` regex 지원 구현

- `SliceMatcher` 내부 record struct 추가
- 생성자에 `sliceMatchers` 배열 파싱 로직 추가 (`ParseSliceMatcher()`)
  - 컬렉션 식 `[.. ]` 사용 (IDE 힌트 반영)
- `SliceStaffMessageText()` 재구현 → `TryApplySliceMatcher()` 분리
  - 분리 이유: SonarQube S3776 인지 복잡도(17) 초과 → 분리 후 허용 범위 이내로 감소
- 기존 로컬 함수 `ContainsSliceMessage()` 제거

---

## Story 3 — `AppSettings` 및 `AppSettingsLoader` 확장

### [완료] 3-1. `AppSettings.cs` 수정

- `Theme` 프로퍼티 추가 (`{ get; set; }`, 기본값 `"System"`)

### [완료] 3-2. `AppSettingsLoader.cs` 수정

- `Save(string filePath, AppSettings settings)` 정적 메서드 추가

### [완료] 3-3. `AppSettings.json` 수정

- `theme` 항목 추가 (`"System"`)

---

## Story 4 — 메뉴 바 UI

### [완료] 4-1. `MainWindowViewModel.cs` 수정

- 생성자 시그니처 확장: `(AppSettings settings, string settingsFilePath, Action<string> applyTheme)`
- `CurrentTheme` 프로퍼티 추가 (C# 14 `field` 키워드)
- `IsLightTheme`, `IsDarkTheme`, `IsSystemTheme` 읽기 전용 프로퍼티 추가
- 커맨드 추가: `OpenSettingsFileCommand`, `ExitCommand`, `ShowAboutCommand`
- 커맨드 추가: `SetLightThemeCommand`, `SetDarkThemeCommand`, `SetSystemThemeCommand`
- `ChangeTheme()` 내부 메서드로 테마 변경·저장 로직 통합
- `ExitCommand`: 람다 대신 메서드 그룹 사용 (IDE 힌트 반영)

### [완료] 4-2. `App.xaml.cs` 수정

- `settingsPath`, `settings` 멤버 변수 추가
- `ApplyTheme(string theme)` 메서드 추가
- `ResolveThemeUri(string theme)` 정적 메서드 추가
  - pack:// 대신 상대 URI(`UriKind.Relative`) 사용 (SonarQube S1075 대응)
- `ResolveThemeName(string theme)` 정적 메서드 추가
- `IsWindowsLightTheme()` 정적 메서드 추가 (레지스트리 기반 시스템 테마 감지)
- `OnStartup()`: 테마 적용 및 ViewModel 생성자 인자 확장
- 버전 `"1.2.0"` → `"1.2.1"`

### [완료] 4-3. `MainWindow.xaml` 수정

- 루트 요소를 `DockPanel`로 변경
- `Menu` 추가 (파일 / 보기 / 도움말 탭)
  - 파일: 설정, 끝내기
  - 보기: 항상 맨 위로 고정, 테마(라이트/다크/시스템 설정에 따름)
  - 도움말: 정보
- Title 업데이트: `"Summarizer (v1.2.1)"`

---

## Story 5 — 테마 리소스

### [완료] 5-1. `Themes/LightTheme.xaml` 생성

- 색상 팔레트 및 SolidColorBrush 리소스 정의
- 암묵적 스타일(`x:Key` 없음) 정의: Window, TextBox, Button, CheckBox, Label, Menu, MenuItem, Separator

### [완료] 5-2. `Themes/DarkTheme.xaml` 생성

- 색상 팔레트 및 SolidColorBrush 리소스 정의 (다크 색상)
- 암묵적 스타일 정의: LightTheme.xaml과 동일 구조

### [완료] 5-3. `Summarizer.App.csproj` 수정

- 테마 XAML 파일 `<Resource>` 항목 추가
- `<FileVersion>`, `<AssemblyVersion>` → `1.2.1`

---

## Story 6 — 버전 업데이트

### [완료] 6-1. 버전 v1.2.1로 업데이트

- `App.xaml.cs`: `Version = "1.2.1"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.1)"`
- `Summarizer.App.csproj`: `<FileVersion>`, `<AssemblyVersion>` → `1.2.1`
- `Summarizer.Core.csproj`: `<FileVersion>`, `<AssemblyVersion>` → `1.2.1`

---

## Story 7 — 빌드 및 동작 검증

### [완료] 7-1. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0
