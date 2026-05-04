# Summarizer v1.2.1 — 구현 계획 (Plan)

> research.md 승인 완료 기준으로 작성된 구현 계획이다.

---

## 현황 파악 (v1.2.0 기준)

```
Summarizer/
├── Summarizer.Core/
│   ├── AppSettings.cs            — POCO, 모든 프로퍼티 { get; init; }
│   ├── AppSettingsLoader.cs      — Load() 정적 메서드
│   └── MessageConverter.cs       — partial class, [GeneratedRegex] 5개
└── Summarizer.App/
    ├── AppSettings.json
    ├── App.xaml / App.xaml.cs    — Version = "1.2.0"
    ├── MainWindow.xaml           — 3열 Grid (입력/버튼/출력)
    ├── MainWindow.xaml.cs
    ├── ViewModels/
    │   └── MainWindowViewModel.cs
    └── Commands/
        └── RelayCommand.cs
```

---

## Story 1 — 리팩토링 [신규]

> CLAUDE.md 코딩 규칙 기준으로 기존 코드의 스타일 위반 사항을 수정한다.

### 1-1. `MessageConverter.cs` 수정 [신규]

**위반 사항 (중괄호 비대칭)** — `Convert()` 내부 (lines 38-83):

```csharp
// 현재 (나쁜 스타일)
if (matches.Count > 0)
{
    ...
    return string.Join(" / ", convertedTexts);
}
else
    return ConvertCustomerText(text);
```

→ `else` 구문에도 `{ }` 블록을 추가한다:

```csharp
// 수정 후 (좋은 스타일)
if (matches.Count > 0)
{
    ...
    return string.Join(" / ", convertedTexts);
}
else
{
    return ConvertCustomerText(text);
}
```

### 1-2. `MainWindowViewModel.cs` 수정 [신규]

**위반 사항 1 — 동일 줄에 복수 구문** (`ClearCommand` 람다):

```csharp
// 현재 (나쁜 스타일)
ClearCommand = new RelayCommand(() => { InputText = string.Empty; OutputText = string.Empty; });
```

→ 구문마다 별도 줄로 분리:

```csharp
// 수정 후
ClearCommand = new RelayCommand(() =>
{
    InputText = string.Empty;
    OutputText = string.Empty;
});
```

**위반 사항 2 — 들여쓰기 오류 및 후행 공백**:

- `InputText.get` 블록 내 `return field;` 앞 들여쓰기가 5칸 → 4칸으로 수정
- `get ` (후행 공백), `{ ` (후행 공백), `return field; ` (후행 공백) 등 제거

---

## Story 2 — `replaceMessages` 구현 [신규]

> **목표 변경**: 기존에 구현한 `sliceStaffMessages` 정규표현식 지원을 폐기하고,
> 보다 범용적인 텍스트 치환 기능인 `replaceMessages`로 대체한다.

### 2-1. 설계 방향

**`replaceMessages`는 텍스트 치환 규칙의 배열이다.**

- 기존 `sliceStaffMessages`의 "메시지 제거" 동작은 `replacement: ""`로 표현한다.
- 각 항목은 `pattern`(검색 대상)과 `replacement`(치환값) 두 필드를 갖는다.
- `pattern`에 `regex:` 접두사를 붙이면 정규표현식으로 처리한다.
- `replacement`는 plain text이며, regex 패턴의 경우 `$1` 등 캡처 그룹 참조도 사용할 수 있다.
- 각 항목은 순서대로 한 번씩 적용된다 (모든 일치 항목 치환, 루프 없음).

**기존 `sliceStaffMessages`와의 대응:**

```json
// 기존 sliceStaffMessages (제거)
"sliceStaffMessages": ["안녕하세요~", "regex:안녕하세요[~!^]+"]

// 대체 replaceMessages (replacement: "" 로 동일한 효과)
"replaceMessages": [
    { "pattern": "안녕하세요~",            "replacement": "" },
    { "pattern": "regex:안녕하세요[~!^]+", "replacement": "" }
]
```

### 2-2. 신규 파일: `Summarizer.Core/ReplaceMessage.cs`

`AppSettings`와 동일 어셈블리에 별도 파일로 작성한다.

```csharp
namespace Summarizer.Core
{
    public record ReplaceMessage
    {
        public string Pattern { get; init; } = string.Empty;
        public string Replacement { get; init; } = string.Empty;
    }
}
```

### 2-3. `AppSettings.cs` 수정

- `SliceStaffMessages` 프로퍼티 **제거**
- `ReplaceMessages` 프로퍼티 **추가** (기본값: 빈 배열)

```csharp
public ReplaceMessage[] ReplaceMessages { get; init; } = [];
```

### 2-4. `AppSettingsLoader.cs` 수정

`jsonOptions`에 `PropertyNamingPolicy`가 설정되어 있지 않아, `Save()` 호출 시 PascalCase로 직렬화된다. `replaceMessages` 추가 시점에 이 문제를 함께 수정한다.

```csharp
private static readonly JsonSerializerOptions jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // 추가
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    IndentSize = 4,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
```

- 기존 파일이 PascalCase로 저장되어 있더라도 `PropertyNameCaseInsensitive = true` 덕분에 읽기는 정상 동작한다.
- 이후 `Save()` 호출 시부터 camelCase로 통일된다.

### 2-5. `MessageConverter.cs` 수정

**제거 대상:**
- `SliceMatcher` record struct
- `sliceMatchers` 필드
- `ParseSliceMatcher()` 정적 메서드
- `SliceStaffMessageText()` 메서드
- `TryApplySliceMatcher()` 정적 메서드

**추가 구조:**

```csharp
private readonly record struct ReplaceMatcher(bool IsRegex, string PlainPattern, string Replacement, Regex? CompiledPattern);

private readonly ReplaceMatcher[] replaceMatchers;
```

**생성자 수정:**

```csharp
public MessageConverter(AppSettings settings)
{
    this.settings = settings;
    replaceMatchers = [.. settings.ReplaceMessages.Select(ParseReplaceMatcher)];
}

private static ReplaceMatcher ParseReplaceMatcher(ReplaceMessage message)
{
    const string regexPrefix = "regex:";
    if (message.Pattern.StartsWith(regexPrefix, StringComparison.OrdinalIgnoreCase))
    {
        var pattern = message.Pattern[regexPrefix.Length..];
        return new ReplaceMatcher(true, pattern, message.Replacement, new Regex(pattern, RegexOptions.Compiled));
    }
    return new ReplaceMatcher(false, message.Pattern, message.Replacement, null);
}
```

**`ApplyReplaceMessages()` (`SliceStaffMessageText` 대체):**

```csharp
private string ApplyReplaceMessages(string text)
{
    var current = text;
    foreach (var matcher in replaceMatchers)
    {
        if (matcher.IsRegex)
            current = matcher.CompiledPattern!.Replace(current, matcher.Replacement);
        else
            current = current.Replace(matcher.PlainPattern, matcher.Replacement);
    }
    return current;
}
```

**`ConvertStaffText()` 수정:**
- `currentText = SliceStaffMessageText(currentText)` → `currentText = ApplyReplaceMessages(currentText)`

### 2-6. `AppSettings.json` 수정

- `sliceStaffMessages` 배열 **제거**
- `replaceMessages` 배열 **추가** (기존 sliceStaffMessages 값 변환):

```json
"replaceMessages": [
    { "pattern": "안녕하세요~",   "replacement": "" },
    { "pattern": "안녕하세요^^",  "replacement": "" },
    { "pattern": "안녕하세요~^^", "replacement": "" },
    { "pattern": "^^",           "replacement": "" },
    { "pattern": "네~^^",        "replacement": "" }
]
```

---

## Story 3 — `AppSettings` 및 `AppSettingsLoader` 확장 [신규]

### 3-1. `AppSettings.cs` 수정 [신규]

`Theme` 프로퍼티를 추가한다. 런타임에 변경이 필요하므로 `set` 접근자를 사용한다.

```csharp
public string Theme { get; set; } = "System";  // "Light" | "Dark" | "System"
```

### 3-2. `AppSettingsLoader.cs` 수정 [신규]

`Save()` 정적 메서드를 추가한다:

```csharp
public static void Save(string filePath, AppSettings settings)
{
    var json = JsonSerializer.Serialize(settings, jsonOptions);
    File.WriteAllText(filePath, json);
}
```

---

## Story 4 — 메뉴 바 UI [신규]

### 4-1. `MainWindowViewModel.cs` 수정 [신규]

**추가 의존성**: 생성자 매개변수 확장

```csharp
public MainWindowViewModel(AppSettings settings, string settingsFilePath, Action<string> applyTheme)
```

- `settingsFilePath`: 설정 파일 열기 및 테마 저장 시 사용
- `applyTheme`: `App.xaml.cs`에서 전달하는 테마 적용 콜백

**추가 프로퍼티**:

```csharp
// C# 14 field 키워드 사용
public string CurrentTheme
{
    get { return field; }
    private set
    {
        field = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
    }
} = "System";

public bool IsLightTheme => CurrentTheme == "Light";
public bool IsDarkTheme => CurrentTheme == "Dark";
public bool IsSystemTheme => CurrentTheme == "System";
```

**추가 커맨드**:

```csharp
public ICommand OpenSettingsFileCommand { get; }
public ICommand ExitCommand { get; }
public ICommand ShowAboutCommand { get; }
public ICommand SetLightThemeCommand { get; }
public ICommand SetDarkThemeCommand { get; }
public ICommand SetSystemThemeCommand { get; }
```

커맨드 구현:
- `OpenSettingsFileCommand`: `Process.Start(new ProcessStartInfo(settingsFilePath) { UseShellExecute = true })`
- `ExitCommand`: `Application.Current.Shutdown()`
- `ShowAboutCommand`: `MessageBox.Show(...)` — 앱 이름, 버전, 설명 표시
- `SetXxxThemeCommand`: `CurrentTheme = "Xxx"` → `applyTheme("Xxx")` → `settings.Theme = "Xxx"` → `AppSettingsLoader.Save(settingsFilePath, settings)`

테마 초기값: 생성자에서 `settings.Theme`을 `CurrentTheme`에 대입한다.

### 4-2. `App.xaml.cs` 수정 [신규]

`OnStartup()` 수정:

```csharp
internal const string Version = "1.2.1";

private string settingsPath = string.Empty;
private AppSettings settings = new();

private void OnStartup(object sender, StartupEventArgs e)
{
    settingsPath = Path.Combine(AppContext.BaseDirectory, "AppSettings.json");
    settings = AppSettingsLoader.Load(settingsPath);
    ApplyTheme(settings.Theme);

    var viewModel = new MainWindowViewModel(settings, settingsPath, ApplyTheme);
    var window = new MainWindow(viewModel);
    window.Show();
}

private void ApplyTheme(string theme)
{
    var uri = ResolveThemeUri(theme);
    Resources.MergedDictionaries.Clear();
    Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
}

private static Uri ResolveThemeUri(string theme)
{
    if (theme == "Light")
        return new Uri("pack://application:,,,/Themes/LightTheme.xaml");
    if (theme == "Dark")
        return new Uri("pack://application:,,,/Themes/DarkTheme.xaml");
    return IsWindowsLightTheme()
        ? new Uri("pack://application:,,,/Themes/LightTheme.xaml")
        : new Uri("pack://application:,,,/Themes/DarkTheme.xaml");
}

private static bool IsWindowsLightTheme()
{
    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
    return key?.GetValue("AppsUseLightTheme") is int value && value == 1;
}
```

### 4-3. `MainWindow.xaml` 수정 [신규]

루트 요소를 `DockPanel`로 바꾸고, 상단에 `Menu`를 배치한다:

```
Window
└── DockPanel
    ├── Menu (DockPanel.Dock="Top")
    │   ├── MenuItem Header="파일"
    │   │   ├── MenuItem Header="설정" Command="{Binding OpenSettingsFileCommand}"
    │   │   ├── Separator
    │   │   └── MenuItem Header="끝내기" Command="{Binding ExitCommand}"
    │   ├── MenuItem Header="보기"
    │   │   ├── MenuItem Header="항상 맨 위로 고정"
    │   │   │     IsCheckable="True"
    │   │   │     IsChecked="{Binding IsAlwaysOnTop}"
    │   │   ├── Separator
    │   │   └── MenuItem Header="테마"
    │   │       ├── MenuItem Header="라이트"
    │   │       │     IsCheckable="True"
    │   │       │     IsChecked="{Binding IsLightTheme, Mode=OneWay}"
    │   │       │     Command="{Binding SetLightThemeCommand}"
    │   │       ├── MenuItem Header="다크"
    │   │       │     IsCheckable="True"
    │   │       │     IsChecked="{Binding IsDarkTheme, Mode=OneWay}"
    │   │       │     Command="{Binding SetDarkThemeCommand}"
    │   │       └── MenuItem Header="시스템 설정에 따름"
    │   │             IsCheckable="True"
    │   │             IsChecked="{Binding IsSystemTheme, Mode=OneWay}"
    │   │             Command="{Binding SetSystemThemeCommand}"
    │   └── MenuItem Header="도움말"
    │       └── MenuItem Header="정보" Command="{Binding ShowAboutCommand}"
    └── Grid Margin="12"   ← 기존 3열 Grid 그대로 유지
        ...
```

**메뉴 바 내 `항상 맨 위로 고정`과 기존 CheckBox 연동**:
- `MainWindow.xaml`의 버튼 영역에 있는 `CheckBox`는 유지한다 (메뉴와 동일한 `IsAlwaysOnTop` 바인딩).

---

## Story 5 — 테마 리소스 [신규]

### 5-1. 테마 파일 구조 [신규]

```
Summarizer.App/
└── Themes/
    ├── LightTheme.xaml
    └── DarkTheme.xaml
```

### 5-2. `LightTheme.xaml` 생성 [신규]

라이트 테마 색상 및 컨트롤 스타일 정의:

| 키 | 색상 | 용도 |
|---|---|---|
| `AppBackground` | `#F5F5F5` | Window 배경 |
| `PanelBackground` | `#FFFFFF` | TextBox, 내용 영역 배경 |
| `ControlBackground` | `#E8E8E8` | Button, CheckBox 배경 |
| `AppForeground` | `#1E1E1E` | 기본 텍스트 |
| `BorderColor` | `#CCCCCC` | 테두리 |
| `MenuBackground` | `#F0F0F0` | Menu 배경 |
| `MenuForeground` | `#1E1E1E` | Menu 텍스트 |

적용 대상 `Style` (TargetType 기준, `x:Key` 없이 암묵적 스타일):
- `Window`
- `Menu`
- `MenuItem`
- `TextBox`
- `Button`
- `CheckBox`
- `Label`
- `Separator`

### 5-3. `DarkTheme.xaml` 생성 [신규]

다크 테마 색상 및 컨트롤 스타일 정의:

| 키 | 색상 | 용도 |
|---|---|---|
| `AppBackground` | `#1E1E1E` | Window 배경 |
| `PanelBackground` | `#2D2D2D` | TextBox, 내용 영역 배경 |
| `ControlBackground` | `#3C3C3C` | Button, CheckBox 배경 |
| `AppForeground` | `#D4D4D4` | 기본 텍스트 |
| `BorderColor` | `#555555` | 테두리 |
| `MenuBackground` | `#2D2D2D` | Menu 배경 |
| `MenuForeground` | `#D4D4D4` | Menu 텍스트 |

### 5-4. `Summarizer.App.csproj` 수정 [신규]

테마 XAML 파일을 `Resource`로 등록:

```xml
<ItemGroup>
    <Resource Include="Themes\LightTheme.xaml" />
    <Resource Include="Themes\DarkTheme.xaml" />
</ItemGroup>
```

### 5-5. `App.xaml` 수정 [신규]

`Application.Resources`는 비워 둔다. 테마는 `App.xaml.cs`의 `ApplyTheme()`이 `MergedDictionaries`에 동적으로 추가한다. 따라서 `App.xaml`에 별도 수정은 없다.

---

## Story 6 — 버전 업데이트 [신규]

| 위치 | 항목 | 값 |
|------|------|-----|
| `App.xaml.cs` | `internal const string Version` | `"1.2.1"` |
| `Summarizer.App.csproj` | `<FileVersion>`, `<AssemblyVersion>` | `1.2.1` |
| `Summarizer.Core.csproj` | `<FileVersion>`, `<AssemblyVersion>` | `1.2.1` |
| `MainWindow.xaml` | `Title` | `"Summarizer (v1.2.1)"` |

---

## Story 7 — 빌드 및 동작 검증 [신규]

### 7-1. 빌드 검증 [신규]

```bash
dotnet build Summarizer.sln
dotnet build Summarizer.sln -c Release
```

- 경고 0, 오류 0 확인

### 7-2. 기능 동작 검증 [신규]

- **리팩토링**: 기존 변환 기능이 v1.2.0과 동일한 결과를 내는지 확인
- **`replaceMessages`**:
  - `AppSettings.json`에 `pattern`/`replacement` 쌍 추가 후 치환 동작 확인 (허구 데이터 사용)
  - `replacement: ""` 으로 기존 sliceStaffMessages와 동일한 제거 동작 확인
  - `pattern`에 `regex:` 접두사 적용 후 정규표현식 치환 확인
  - `replacement`에 캡처 그룹 참조(`$1`) 사용 동작 확인
- **메뉴 바**:
  - 파일 > 설정: `AppSettings.json`이 텍스트 에디터로 열리는지 확인
  - 파일 > 끝내기: 앱 종료 확인
  - 보기 > 항상 맨 위로 고정: 체크 상태와 `Topmost` 동기화 확인 (메뉴 / 기존 CheckBox 양방향)
  - 보기 > 테마: 라이트/다크/시스템 전환 확인, 앱 재시작 후 테마 유지 확인
  - 도움말 > 정보: 버전 및 설명 표시 확인
- **테마**: 라이트 ↔ 다크 전환 시 모든 컨트롤에 테마가 즉시 반영되는지 확인

---

## 변경 대상 파일 요약

| 파일 | 변경 내용 | 상태 |
|------|-----------|------|
| `Summarizer.Core/ReplaceMessage.cs` | 신규 생성 — `Pattern` / `Replacement` record | [신규] |
| `Summarizer.Core/AppSettings.cs` | `SliceStaffMessages` 제거, `ReplaceMessages` 추가, `Theme` 추가 | [신규] |
| `Summarizer.Core/AppSettingsLoader.cs` | `PropertyNamingPolicy = CamelCase` 추가, `Save()` 추가 | [신규] |
| `Summarizer.Core/MessageConverter.cs` | 중괄호 수정, `SliceMatcher` 제거 → `ReplaceMatcher` + `ApplyReplaceMessages()` | [신규] |
| `Summarizer.Core/Summarizer.Core.csproj` | 버전 업데이트 | [완료] |
| `Summarizer.App/App.xaml.cs` | 버전 업데이트, 테마 적용 로직 추가 | [완료] |
| `Summarizer.App/AppSettings.json` | `sliceStaffMessages` 제거, `replaceMessages` 추가, `theme` 추가 | [신규] |
| `Summarizer.App/MainWindow.xaml` | 버전 업데이트, `DockPanel` + `Menu` 추가 | [완료] |
| `Summarizer.App/ViewModels/MainWindowViewModel.cs` | 스타일 수정, 테마/메뉴 관련 프로퍼티·커맨드 추가, 생성자 확장 | [완료] |
| `Summarizer.App/Themes/LightTheme.xaml` | 신규 생성 | [완료] |
| `Summarizer.App/Themes/DarkTheme.xaml` | 신규 생성 | [완료] |
| `Summarizer.App/Summarizer.App.csproj` | 버전 업데이트, 테마 Resource 등록 | [완료] |
