# Summarizer v1.2.0 — 구현 계획 (Plan)

> research.md 승인 완료 기준으로 작성된 구현 계획이다.

---

## 전체 구조 변경 요약

```
(이전)
Summarizer/
├── Summarizer.sln
├── Summarizer.csproj        ← WinForms, net8.0-windows
├── Program.cs
├── Configuration.cs
├── FormMain.cs / .Designer.cs / .resx
├── App.config / AppSettings.settings / AppSettings.Designer.cs
└── icon.ico

(이후)
Summarizer/
├── Summarizer.sln           ← 두 프로젝트 등록
├── .editorconfig            ← 유지
├── .gitignore               ← 신규 추가
├── Summarizer.Core/
│   ├── Summarizer.Core.csproj   (net10.0)
│   ├── AppSettings.cs
│   ├── AppSettingsLoader.cs
│   └── MessageConverter.cs
└── Summarizer.App/
    ├── Summarizer.App.csproj    (net10.0-windows, UseWPF)
    ├── icon.ico
    ├── AppSettings.json
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── ViewModels/
    │   └── MainWindowViewModel.cs
    └── Commands/
        └── RelayCommand.cs
```

---

## Story 1 — 솔루션 구조 재편 [신규]

### 1-1. 디렉토리 생성 및 파일 이동 [신규]

- `Summarizer.Core/` 디렉토리 생성
- `Summarizer.App/` 디렉토리 생성
- `icon.ico` → `Summarizer.App/icon.ico` 이동

### 1-2. `Summarizer.sln` 재구성 [신규]

기존 `Summarizer.csproj` 참조를 제거하고, 두 신규 프로젝트를 등록한다.

- 제거: `"Summarizer", "Summarizer.csproj", "{792B2D8F-...}"` 항목
- 추가: `Summarizer.Core` (`Summarizer.Core/Summarizer.Core.csproj`)
- 추가: `Summarizer.App` (`Summarizer.App/Summarizer.App.csproj`)
- 각 프로젝트에 새 GUID 발급 (기존 GUID 재사용 금지)

### 1-3. `.gitignore` 추가 [신규]

다음 항목을 포함하는 `.gitignore`를 생성한다.

- `requirements.txt` — 실제 대화 및 개인정보가 포함될 수 있으므로 저장소에서 영구 제외
- 빌드 산출물: `bin/`, `obj/`
- IDE 관련: `.vs/`, `.idea/`, `*.user`
- 기타: `.claude/.edit-baks/`

추가로 현재 git에서 추적 중인 `requirements.txt`의 추적을 해제한다:
```bash
git rm --cached requirements.txt
```

---

## Story 2 — `Summarizer.Core` 프로젝트 구성 [신규]

### 2-1. `Summarizer.Core.csproj` 생성 [신규]

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <FileVersion>1.2.0</FileVersion>
    <AssemblyVersion>1.2.0</AssemblyVersion>
  </PropertyGroup>
</Project>
```

`net10.0` (Windows 비의존) — CLI/Web 등으로 확장 시 재사용 가능.

### 2-2. `AppSettings.cs` 구현 [신규]

설정 데이터 POCO. JSON 역직렬화 대상이자, 기본값의 단일 정의 지점.

- 프로퍼티: `StaffName`, `ReservationConfirmMessage`, `FormMessages`, `SliceStaffMessages`
- 모든 프로퍼티: `{ get; init; }` (불변 객체)
- 기본값을 프로퍼티 초기값으로 직접 정의
- 배열 타입은 `string[]` 사용 (`StringCollection` 완전 제거)
- 네임스페이스: `Summarizer.Core`

### 2-3. `AppSettingsLoader.cs` 구현 [신규]

JSON 파일 로딩 및 파일 자동 생성 로직.

- `static AppSettings Load(string filePath)` 메서드 제공
  - 파일 존재: `JsonSerializer.Deserialize<AppSettings>()` 후 반환
  - 파일 없음:
    1. `new AppSettings()` (기본값 인스턴스) 생성
    2. 해당 인스턴스를 JSON으로 직렬화 후 `filePath`에 파일 저장
    3. 인스턴스 반환
- `JsonSerializerOptions`: `PropertyNameCaseInsensitive = true`, `WriteIndented = true`
- 네임스페이스: `Summarizer.Core`

### 2-4. `MessageConverter.cs` 구현 [신규]

현재 `FormMain.cs`의 텍스트 변환 로직 전체를 추출. UI 의존성 없음.

- 생성자: `MessageConverter(AppSettings settings)` — settings를 `readonly` 필드에 저장
- 공개 메서드: `string Convert(string text)` (현재 `ConvertText()` 역할)
- private 메서드 — 현재 `FormMain.cs`에서 그대로 이전:
  - `ConvertCategorizedText()`
  - `ConvertCustomerText()`
  - `ConvertStaffText()`
  - `SplitTextContents()`
  - `SummarizeTextContents()`
  - `SliceStaffMessageText()`
  - `IsCellPhoneNumber()`
  - `StandardizeCellPhoneNumber()`
  - `StandardizeBirthNumber()`
- `[GeneratedRegex]` 정규표현식 5개: 클래스 내에 그대로 이전
- 네임스페이스: `Summarizer.Core`

---

## Story 3 — `Summarizer.App` 프로젝트 구성 [신규]

### 3-1. `Summarizer.App.csproj` 생성 [신규]

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <FileVersion>1.2.0</FileVersion>
    <AssemblyVersion>1.2.0</AssemblyVersion>
    <ApplicationIcon>icon.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="icon.ico" />
    <Content Include="AppSettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Summarizer.Core\Summarizer.Core.csproj" />
  </ItemGroup>
</Project>
```

### 3-2. `AppSettings.json` 생성 [신규]

`Summarizer.App/` 루트에 배치. `AppSettings.cs` 기본값과 동일한 내용.

```json
{
  "staffName": "아무개",
  "reservationConfirmMessage": "채널로 예약문자 전송",
  "formMessages": [
    "상담받을 분의 성함 / 연락처 - ",
    "생년월일 - ",
    "상담부위 - ",
    "첫수술or 재수술 (재수술일경우 마지막 수술시기 ) - ",
    "상담 희망 날짜와 시간대 - ",
    "상담 원하는 원장님 - ",
    "저희 병원 알게되신 경로 - ",
    "소개자 있으실 경우 소개자 성함과 연락처 뒷번호 - "
  ],
  "sliceStaffMessages": [
    "안녕하세요~",
    "안녕하세요^^",
    "안녕하세요~^^",
    "^^"
  ]
}
```

### 3-3. `Commands/RelayCommand.cs` 구현 [신규]

외부 라이브러리 없이 `ICommand` 직접 구현.

- 생성자 1: `RelayCommand(Action execute)` — 항상 실행 가능
- 생성자 2: `RelayCommand(Action execute, Func<bool> canExecute)` — 조건부
- `CanExecute()`: `canExecute` 없으면 항상 `true`
- `Execute()`: `execute` 호출
- `CanExecuteChanged`: `CommandManager.RequerySuggested` 위임
- 네임스페이스: `Summarizer.App.Commands`

### 3-4. `ViewModels/MainWindowViewModel.cs` 구현 [신규]

MVVM ViewModel. UI 상태와 커맨드를 보유.

- `INotifyPropertyChanged` 구현 (`OnPropertyChanged([CallerMemberName])` 헬퍼 메서드)
- **프로퍼티** (C# 14 `field` 키워드 활용):
  - `string InputText` — 입력 텍스트 (양방향 바인딩)
  - `string OutputText` — 변환 결과 출력
  - `bool IsAlwaysOnTop` — 창 최상위 고정 여부
- **커맨드**:
  - `ICommand ConvertCommand` — `MessageConverter.Convert(InputText)` → `OutputText`
  - `ICommand ClearCommand` — `InputText`, `OutputText` 빈 문자열로 초기화
  - `ICommand CopyOutputCommand` — `Clipboard.SetText(OutputText)`
- **생성자**: `MainWindowViewModel(AppSettings settings)`
  - `MessageConverter` 인스턴스 생성 (settings 전달)
- 네임스페이스: `Summarizer.App.ViewModels`

### 3-5. `App.xaml` / `App.xaml.cs` 구현 [신규]

**`App.xaml`**
- `StartupUri` 미사용 (코드에서 직접 Window 생성 — ViewModel 주입 필요)
- `Startup="OnStartup"` 이벤트 핸들러 지정

**`App.xaml.cs`**
- `internal const string Version = "1.2.0"` 상수 보유
- `OnStartup()` 오버라이드:
  1. 설정 경로: `Path.Combine(AppContext.BaseDirectory, "AppSettings.json")`
  2. `AppSettingsLoader.Load(path)` → `AppSettings` 로딩
  3. `new MainWindowViewModel(settings)` 생성
  4. `new MainWindow(viewModel)` 생성 → `Show()`
- 네임스페이스: `Summarizer.App`

### 3-6. `MainWindow.xaml` / `MainWindow.xaml.cs` 구현 [신규]

**`MainWindow.xaml`**

현재 WinForms UI(좌측 입력 / 중앙 버튼 / 우측 출력)를 WPF로 구현.

레이아웃 구조:
```
Window
  Title="Summarizer (v1.2.0)"
  Topmost="{Binding IsAlwaysOnTop}"
└── Grid (3열: *, Auto, *)
    ├── Column 0 — DockPanel
    │   ├── Label "변환할 텍스트 입력"
    │   └── TextBox
    │         Text="{Binding InputText, UpdateSourceTrigger=PropertyChanged}"
    │         AcceptsReturn="True"
    │         TextWrapping="Wrap"
    │         VerticalScrollBarVisibility="Auto"
    ├── Column 1 — StackPanel (버튼 세로 배치, 중앙 정렬)
    │   ├── CheckBox
    │   │     Content="항상 맨 위로 고정"
    │   │     IsChecked="{Binding IsAlwaysOnTop}"
    │   ├── Button Content="변환하기"  Command="{Binding ConvertCommand}"
    │   ├── Button Content="복사하기"  Command="{Binding CopyOutputCommand}"
    │   └── Button Content="비우기"   Command="{Binding ClearCommand}"
    └── Column 2 — DockPanel
        ├── Label "변환 결과"
        └── TextBox
              Text="{Binding OutputText, Mode=OneWay}"
              IsReadOnly="True"
              TextWrapping="Wrap"
              VerticalScrollBarVisibility="Auto"
```

**`MainWindow.xaml.cs`**
- 생성자: `MainWindow(MainWindowViewModel viewModel)` — `DataContext = viewModel` 설정만 수행
- 그 외 로직 없음 (Code-behind 최소화)
- 네임스페이스: `Summarizer.App`

---

## Story 4 — 구 파일 제거 [신규]

| 파일 | 대체 |
|------|------|
| `Program.cs` | `App.xaml.cs` (OnStartup) |
| `Configuration.cs` | `Summarizer.Core/AppSettings.cs` |
| `FormMain.cs` | `MessageConverter.cs` + `MainWindow` + `MainWindowViewModel` |
| `FormMain.Designer.cs` | WPF 불필요 |
| `FormMain.resx` | WPF 불필요 |
| `App.config` | `AppSettings.json` |
| `AppSettings.settings` | VS 디자이너 방식 폐기 |
| `AppSettings.Designer.cs` | 자동생성 파일 폐기 |
| `Summarizer.csproj` | `Summarizer.App.csproj` + `Summarizer.Core.csproj` |

---

## Story 5 — 빌드 및 동작 검증 [신규]

### 5-1. 빌드 검증 [신규]

```bash
dotnet build Summarizer.sln
dotnet build Summarizer.sln -c Release
```

- 빌드 경고/오류 없음 확인
- `Summarizer.Core` 단독 빌드 성공 확인 (Windows 비의존 검증)

### 5-2. AppSettings.json 자동 생성 검증 [신규]

- 출력 디렉토리에서 `AppSettings.json` 삭제 후 앱 실행
- 앱이 정상 시작되고, `AppSettings.json`이 기본값으로 자동 생성되는지 확인

### 5-3. UI 및 기능 동작 검증 [신규]

- 가공된 예제 시나리오(허구의 인물/내용)로 변환 결과 확인 (v1.1.2와 동일한 출력 형식)
- "항상 맨 위로 고정" 체크박스 동작 확인
- "복사하기" 버튼 → 클립보드 복사 확인
- "비우기" 버튼 → 입력/출력 초기화 확인

---

## 버전 업데이트 위치

| 위치 | 항목 |
|------|------|
| `App.xaml.cs` | `internal const string Version = "1.2.0"` |
| `Summarizer.App.csproj` | `<FileVersion>`, `<AssemblyVersion>` |
| `Summarizer.Core.csproj` | `<FileVersion>`, `<AssemblyVersion>` |
