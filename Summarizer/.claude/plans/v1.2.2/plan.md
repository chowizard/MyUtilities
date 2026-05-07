# Summarizer v1.2.2 — 구현 계획 (Plan)

> research.md 기반으로 작성. 승인 후 task.md로 이행.

---

## 스토리 구성 개요

| Story | 주제 | 변경 파일 |
|---|---|---|
| 1 | 직원 이름 입력 UI | `AppSettings.cs`, `MainWindowViewModel.cs`, `MainWindow.xaml` |
| 2 | 배치 파일 변환 코어 | `BatchFileConverter.cs` (신규), `Summarizer.Core.csproj` |
| 3 | CLI 지원 | `App.xaml.cs` |
| 4 | 버전 업데이트 | `App.xaml.cs`, `MainWindow.xaml`, `*.csproj` |
| 5 | 빌드 및 검증 | — |

---

## Story 1 — 직원 이름 입력 UI [신규]

### 1-1. `AppSettings.cs` 수정

- `StaffName` 속성의 접근자를 `init` → `set`으로 변경
  - `Theme`와 동일한 방식으로, 런타임 중 변경 가능해야 하기 때문

**변경 전:**
```csharp
public string StaffName { get; init; } = "아무개";
```
**변경 후:**
```csharp
public string StaffName { get; set; } = "아무개";
```

---

### 1-2. `MainWindowViewModel.cs` 수정

- `StaffName` 바인딩 프로퍼티 추가
  - `get`: `settings.StaffName` 반환
  - `set`: `settings.StaffName = value` 갱신 → `AppSettingsLoader.Save()` 호출 → `OnPropertyChanged()` 발생
  - 테마 변경의 `ChangeTheme()` 패턴과 동일한 구조

```csharp
public string StaffName
{
    get
    {
        return settings.StaffName;
    }
    set
    {
        settings.StaffName = value;
        AppSettingsLoader.Save(settingsFilePath, settings);
        OnPropertyChanged();
    }
}
```

> **저장 시점 설계**: TextBox의 `UpdateSourceTrigger=LostFocus` 바인딩을 사용.
> 포커스가 벗어날 때 setter가 호출되어 저장. 키 입력마다 JSON 파일을 쓰는 비효율을 피함.

---

### 1-3. `MainWindow.xaml` 수정

- 좌측 상단에 직원 이름 입력 컨트롤 추가
- **배치 방식**: 본문 `Grid`에 행(RowDefinition) 추가
  - Row 0 (Height=Auto): 직원 이름 입력 컨트롤 (Column 0에만)
  - Row 1 (Height=\*): 기존 3열 본문
- 모든 기존 컬럼 요소에 `Grid.Row="1"` 추가
- Title을 `"Summarizer (v1.2.1)"` → `"Summarizer (v1.2.2)"`로 갱신 (Story 4에서 일괄 수행 가능하나, 이 파일을 수정하는 이 스토리에서 함께 처리)

**직원 이름 입력 컨트롤 구조:**
```xml
<StackPanel Grid.Row="0" Grid.Column="0"
            Orientation="Horizontal"
            Margin="0,0,0,8"
            VerticalAlignment="Center">
    <Label Content="직원 이름" Padding="0,0,6,0" VerticalAlignment="Center" />
    <TextBox Text="{Binding StaffName, UpdateSourceTrigger=LostFocus}"
             Width="120"
             VerticalAlignment="Center" />
</StackPanel>
```

---

## Story 2 — 배치 파일 변환 코어 [신규]

### 2-1. `BatchFileConverter.cs` 신규 생성 (Summarizer.Core)

**역할**: `[]` 구분 텍스트 파일을 읽어 각 대화를 변환하고 결과 파일로 출력.

**클래스 설계:**
```csharp
public class BatchFileConverter
{
    public BatchFileConverter(AppSettings settings);
    public void ConvertFile(string inputFilePath);

    private static List<string> ParseConversations(string text);
    private static string BuildOutputFilePath(string inputFilePath);
}
```

**파싱 규칙 (`ParseConversations`)**:
- 줄 단위로 처리 (trim 후 비교)
- `[`만 있는 줄 → 블록 수집 시작
- `]`만 있는 줄 → 블록 수집 종료, 수집된 줄들을 `Environment.NewLine`으로 결합하여 리스트에 추가
- 그 외 줄 → 블록 수집 중이면 추가, 아니면 무시
- `[` 안에 `[`가 중첩되는 경우는 명세에 없으므로 무시 (단순 상태 기계)

**파일 I/O:**
- 읽기: `File.ReadAllText(inputFilePath, new UTF8Encoding(false))`
- 쓰기: `File.WriteAllLines(outputFilePath, results, new UTF8Encoding(false))`
  - 각 변환 결과를 한 줄씩 출력

**출력 파일 경로 생성 (`BuildOutputFilePath`)**:
- 입력: `C:\path\to\conversations.txt`
- 출력: `C:\path\to\conversations-converted.txt`
- 규칙: `{디렉토리}\{이름}-converted{확장자}`

---

## Story 3 — CLI 지원 [신규]

### 3-1. `App.xaml.cs` 수정

**설계 방향**: 기존 WinExe (`Summarizer.App`)에 커맨드라인 인수를 지원하는 분기 추가.
별도 콘솔 앱 생성 없이 기존 실행 파일 하나로 두 모드를 지원하는 것이 명세의 1순위 선호.

**`OnStartup` 수정:**
```csharp
private void OnStartup(object sender, StartupEventArgs e)
{
    settingsPath = Path.Combine(AppContext.BaseDirectory, "AppSettings.json");
    settings = AppSettingsLoader.Load(settingsPath);

    if (e.Args.Length > 0)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        RunBatchMode(e.Args[0]);
        Shutdown();
        return;
    }

    ApplyTheme(settings.Theme);
    var viewModel = new MainWindowViewModel(settings, settingsPath, ApplyTheme);
    var window = new MainWindow(viewModel);
    window.Show();
}

private void RunBatchMode(string inputFilePath)
{
    var batchConverter = new BatchFileConverter(settings);
    batchConverter.ConvertFile(inputFilePath);
}
```

**주의사항:**
- WPF WinExe는 콘솔 창을 가지지 않으므로, CLI 실행 결과의 상태 출력은 파일 생성으로 대신함.
- `ShutdownMode.OnExplicitShutdown`을 설정해야 `OnStartup` 내에서 `Shutdown()` 호출이 안전하게 동작함.
- 오류 처리: `RunBatchMode`에서 try-catch로 예외를 잡아 `MessageBox.Show(오류 메시지)`로 표시한다.
  - 파일 미존재, I/O 오류, 파싱 불가 등 모든 예외를 이 방식으로 처리

**CLI 사용 방법 (예시):**
```
Summarizer.exe "C:\path\to\conversations.txt"
```

---

## Story 4 — 버전 업데이트 [신규]

- `App.xaml.cs`: `Version = "1.2.1"` → `"1.2.2"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.1)"` → `Title="Summarizer (v1.2.2)"`
  - Story 1-3에서 `MainWindow.xaml`을 수정할 때 함께 변경
- `Summarizer.App.csproj`: `<FileVersion>`, `<AssemblyVersion>` → `1.2.2`
- `Summarizer.Core.csproj`: `<FileVersion>`, `<AssemblyVersion>` → `1.2.2`

---

## Story 5 — 빌드 및 검증 [신규]

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0
- GUI 모드 동작 확인: 직원 이름 입력 → 포커스 이동 → `AppSettings.json` 반영 확인
- CLI 모드 동작 확인: 테스트용 입력 파일 → `{이름}-converted{확장자}` 출력 파일 생성 확인
  - CLI 정상 실행 확인
  - 존재하지 않는 파일 경로로 실행 시 오류 MessageBox 표시 확인

> **테스트 파일 작성 규칙 (CLAUDE.md 개인정보 보호 규칙 적용)**
> - 테스트용 입력 파일은 반드시 **허구의 인물과 가공의 내용**으로만 작성한다.
> - 저장소에 커밋할 경우 `[가상]` 또는 `[테스트]` 말머리를 붙이고 승인을 받아야 한다.
> - 실제 이름, 실제 전화번호, 실제 날짜 조합 등 식별 가능한 정보를 포함해서는 안 된다.

---

## 미결 설계 질문 (승인 필요)

1. **CLI 오류 처리 방식**
   - 현재 계획: 예외 전파, WPF 기본 처리
   - 대안: `RunBatchMode`에서 try-catch + `MessageBox.Show(오류 메시지)`
   - 어느 쪽이 더 바람직한지 확인 필요
<!--
 대안 방식으로 구현하기로 결정함.
-->

2. **직원 이름 입력 컨트롤 위치**
   - 현재 계획: 좌측 상단 (입력 영역 컬럼의 위, 별도 행)
   - 혹시 다른 위치를 원한다면 알려줘
<!--
 현재 계획대로 진행하면 됨.
-->
