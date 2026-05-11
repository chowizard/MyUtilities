# Summarizer v1.2.3 — 구현 계획 (Plan)

> research.md 분석 완료 기준으로 작성된 구현 계획이다.
> 각 스토리는 `[신규]` 태그로 표시되며, 작업 진행 시 `[진행]` / `[완료]`로 갱신한다.

---

## Story 1 — `replaceMessages` → `replaceStaffMessages` 이름 변경 및 하위 호환

### 배경

고객 메시지는 있는 그대로 옮기고, 직원 메시지만 축약·대체를 적용하는 정책이 명확해졌다.
JSON 키 이름을 `replaceMessages` → `replaceStaffMessages`로 변경하고, 기존 배포본 파일도 읽을 수 있어야 한다.

### [신규] 1-1. `AppSettings.cs` 수정

- `ReplaceMessages` 속성 이름 → `ReplaceStaffMessages`
- `{ get; init; }` → `{ get; set; }` (다이얼로그 편집을 위해)
- 기존: `public ReplaceMessage[] ReplaceMessages { get; init; } = [];`
- 변경: `public ReplaceMessage[] ReplaceStaffMessages { get; set; } = [];`

- 아울러, Story 4(AppSettingsDialog)에서 편집하는 다른 속성들도 `init` → `set`으로 변경:
  - `ReservationConfirmMessage`: `init` → `set`
  - `FormMessages`: `init` → `set`

### [신규] 1-2. `AppSettingsLoader.cs` 수정 — 하위 호환 마이그레이션

로드 후 `JsonDocument`를 사용하여 구 키(`replaceMessages`)가 존재할 경우 마이그레이션한다.

```csharp
public static AppSettings Load(string filePath)
{
    if (!File.Exists(filePath))
    {
        var defaultSettings = new AppSettings();
        Save(filePath, defaultSettings);
        return defaultSettings;
    }

    var json = File.ReadAllText(filePath);
    var settings = JsonSerializer.Deserialize<AppSettings>(json, jsonOptions) ?? new AppSettings();

    // 하위 호환: "replaceMessages" → "replaceStaffMessages" 자동 마이그레이션
    if (settings.ReplaceStaffMessages.Length == 0)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("replaceMessages", out var legacyElement))
        {
            var legacy = JsonSerializer.Deserialize<ReplaceMessage[]>(
                legacyElement.GetRawText(), jsonOptions);
            if (legacy?.Length > 0)
                settings.ReplaceStaffMessages = legacy;
        }
    }

    return settings;
}
```

> 저장(`Save`)은 항상 `ReplaceStaffMessages` 키로 직렬화되어 구 키는 자연스럽게 소멸한다.

### [신규] 1-3. `MessageConverter.cs` 수정

- 생성자 및 `ApplyReplaceMessages()`에서 `settings.ReplaceMessages` → `settings.ReplaceStaffMessages`로 변경

### [신규] 1-4. `AppSettings.json` 수정

- `"replaceMessages"` 키 → `"replaceStaffMessages"` 로 변경

---

## Story 2 — `StandardizeBirthNumber` ON/OFF 옵션 (기본값: 비활성)

### 배경

생년월일 표준화가 필요 없는 경우를 위해 기능을 끌 수 있어야 하며, 기본값은 비활성(false)이다.
함수 자체는 삭제하지 않고, 호출 여부만 설정으로 제어한다.

### [신규] 2-1. `AppSettings.cs` 수정

```csharp
public bool NormalizeBirthNumber { get; set; } = false;
```

### [신규] 2-2. `AppSettings.json` 수정

```json
"normalizeBirthNumber": false
```

### [신규] 2-3. `MessageConverter.cs` 수정

`ConvertCustomerText`와 `ConvertStaffText` 양쪽 모두, `StandardizeBirthNumber` 호출 부분을 조건 분기로 감싼다.

```csharp
if (IsCellPhoneNumber(currentText))
    currentText = StandardizeCellPhoneNumber(currentText);
else if (settings.NormalizeBirthNumber)
    currentText = StandardizeBirthNumber(currentText);
```

> `AppSettingsDialog`에서 ON/OFF 체크박스로 편집 가능하게 한다 (Story 4 참조).

---

## Story 3 — `formMessages` 정규표현식 지원

### 배경

`replaceStaffMessages`와 동일하게 `"regex:"` 접두어로 정규표현식을 사용할 수 있어야 한다.
`AppSettings.FormMessages`의 타입(`string[]`)은 유지하고, `MessageConverter` 내부에서 파싱한다.

### [신규] 3-1. `MessageConverter.cs` 수정

**`FormMatcher` private record struct 추가** (`ReplaceMatcher`와 동일한 구조):

```csharp
private readonly record struct FormMatcher(bool IsRegex, string PlainPattern, Regex? CompiledPattern);
```

**생성자 수정**: `formMatchers` 필드 파싱 추가

```csharp
private readonly FormMatcher[] formMatchers;

public MessageConverter(AppSettings settings)
{
    this.settings = settings;
    replaceMatchers = [.. settings.ReplaceStaffMessages.Select(ParseReplaceMatcher)];
    formMatchers = [.. settings.FormMessages.Select(ParseFormMatcher)];
}

private static FormMatcher ParseFormMatcher(string formMessage)
{
    const string regexPrefix = "regex:";
    if (formMessage.StartsWith(regexPrefix, StringComparison.OrdinalIgnoreCase))
    {
        var pattern = formMessage[regexPrefix.Length..];
        return new FormMatcher(true, pattern, new Regex(pattern, RegexOptions.Compiled));
    }
    return new FormMatcher(false, formMessage, null);
}
```

**`SummarizeTextContents()` 수정**: `StringBuilder` 기반 → 문자열 반환 방식으로 정리

```csharp
private List<string> SummarizeTextContents(ICollection<string> texts)
{
    List<string> summarizedTexts = new(texts.Count);
    foreach (var text in texts)
    {
        int index = Array.FindIndex(formMatchers, matcher => FormMatcherMatches(text, matcher));
        summarizedTexts.Add(index >= 0 ? ApplyFormMatcher(text, formMatchers[index]) : text);
    }
    return summarizedTexts;
}

private static bool FormMatcherMatches(string text, FormMatcher matcher)
{
    if (string.Compare(text, matcher.PlainPattern, StringComparison.InvariantCultureIgnoreCase) == 0)
        return false;   // 전체가 패턴과 동일한 경우는 제외 (기존 ContainsFormMessage 동작 유지)

    return matcher.IsRegex
        ? matcher.CompiledPattern!.IsMatch(text)
        : text.Contains(matcher.PlainPattern);
}

private static string ApplyFormMatcher(string text, FormMatcher matcher)
{
    return matcher.IsRegex
        ? matcher.CompiledPattern!.Replace(text, string.Empty)
        : text.Replace(matcher.PlainPattern, string.Empty);
}
```

---

## Story 4 — `AppSettingsDialog` 구현

### 배경 및 설계 방침

- 기존 `파일 > 설정` 메뉴가 텍스트 에디터로 AppSettings.json을 여는 동작을 대체한다.
- Dialog는 열 때 AppSettings.json을 디스크에서 새로 로드하고, 닫을 때 디스크에 저장한다.
- 테마는 현재 앱 테마를 그대로 적용한다.
- `MainWindowViewModel`이 Dialog 닫힌 후 설정을 재로드하고 `MessageConverter`를 재생성한다.

---

### [신규] 4-1. Item ViewModel 클래스 신규 생성

**파일**: `Summarizer.App/ViewModels/FormMessageItemViewModel.cs`

```
FormMessageItemViewModel : INotifyPropertyChanged
├── Text : string (TextBox에 바인딩)
├── IsRegex : bool (정규표현식 CheckBox)
└── IsSelected : bool (선택 CheckBox)
```

**파일**: `Summarizer.App/ViewModels/ReplaceStaffMessageItemViewModel.cs`

```
ReplaceStaffMessageItemViewModel : INotifyPropertyChanged
├── IsSelected : bool
├── HasComment : bool   (추가 CheckBox 상태; true이면 Comment TextBox 표시)
├── Comment : string    (메모 TextBox 내용)
├── Pattern : string    (찾을 메시지 TextBox)
├── IsPatternRegex : bool  (찾을 메시지 정규표현식 CheckBox)
├── HasReplacement : bool  (추가 CheckBox 상태; true이면 Replacement TextBox 표시)
├── Replacement : string   (바꿀 메시지 TextBox)
└── IsReplacementRegex : bool  (바꿀 메시지 정규표현식 CheckBox — HasReplacement=true일 때만 유효)
```

`Comment` 및 `Replacement`의 "추가" 체크박스 행동:
- CheckBox 체크 → TextBox 표시 (`HasComment` / `HasReplacement` → true)
- TextBox 내용 비움 + 포커스 이탈 → CheckBox로 복귀 (`HasComment` / `HasReplacement` → false)
  - ViewModel의 setter에서 처리 (`LostFocus` 이벤트 → ViewModel 속성 갱신)

---

### [신규] 4-2. `AppSettingsDialogViewModel.cs` 신규 생성

**파일**: `Summarizer.App/ViewModels/AppSettingsDialogViewModel.cs`

```
AppSettingsDialogViewModel : INotifyPropertyChanged
├── [Fields]
│   ├── settingsFilePath : string
│   └── settings : AppSettings  (Dialog 열 때 디스크에서 새로 로드한 객체)
│
├── [Bindable Properties]
│   ├── ReservationConfirmMessage : string
│   ├── NormalizeBirthNumber : bool
│   ├── FormMessages : ObservableCollection<FormMessageItemViewModel>
│   └── ReplaceStaffMessages : ObservableCollection<ReplaceStaffMessageItemViewModel>
│
└── [Commands]
    ├── OpenJsonFileCommand     (AppSettings.json을 텍스트 에디터로 열기)
    ├── CloseCommand            (Save + 창 닫기)
    │
    ├── AddFormMessageCommand
    ├── DeleteFormMessagesCommand   (선택된 항목 삭제)
    ├── MoveFormMessageUpCommand    (단일 선택 + 첫 항목 아님)
    ├── MoveFormMessageDownCommand  (단일 선택 + 마지막 항목 아님)
    ├── SelectAllFormMessagesCommand
    │
    ├── AddReplaceStaffMessageCommand
    ├── DeleteReplaceStaffMessagesCommand
    ├── MoveReplaceStaffMessageUpCommand
    ├── MoveReplaceStaffMessageDownCommand
    └── SelectAllReplaceStaffMessagesCommand
```

**생성자**: `settingsFilePath` 수신 → `AppSettingsLoader.Load()` 호출 → ObservableCollection 초기화

**저장 메서드** (`SaveToSettings`): ObservableCollection → `settings` 객체 갱신 → `AppSettingsLoader.Save()`

**`CloseCommand`**: `SaveToSettings()` 실행 후 `RequestClose` 이벤트 발행 (또는 생성자에서 `Window` 참조를 받아 Close 호출)
- 권장 방식: `Action closeAction`을 생성자로 주입받아 호출

**`MoveUp`/`MoveDown` CanExecute 조건**:
- 선택된 항목이 정확히 1개
- Up: 선택 항목이 인덱스 0이 아님
- Down: 선택 항목이 마지막 인덱스가 아님

---

### [신규] 4-3. `AppSettingsDialog.xaml` + `AppSettingsDialog.xaml.cs` 신규 생성

**파일**: `Summarizer.App/AppSettingsDialog.xaml`

**레이아웃 구조**:

```
Window (Title="설정", Width=680, SizeToContent=Height, MaxHeight=720)
└── DockPanel
    ├── [Top] ToolBar or StackPanel (Horizontal)
    │   └── Button "JSON 파일로 열기" (OpenJsonFileCommand)
    ├── [Bottom] Border (Padding)
    │   └── Button "닫기" (CloseCommand)  — HorizontalAlignment=Right
    └── [Fill] ScrollViewer
        └── StackPanel (Vertical, Margin=12)
            │
            ├── [Section] 예약 확인 텍스트
            │   ├── Label "예약 확인 텍스트"
            │   └── TextBox (ReservationConfirmMessage)
            │
            ├── Separator
            │
            ├── [Section] 생년월일 표준화
            │   └── CheckBox "생년월일 표준화 사용" (NormalizeBirthNumber)
            │
            ├── Separator
            │
            ├── [Section] 상담 메시지 형식 (formMessages)
            │   ├── Label "상담 메시지 형식"
            │   ├── [ActionBar] StackPanel(Horizontal): 추가 / 삭제 / 위로 / 아래로 / 모두선택 버튼
            │   └── ItemsControl (FormMessages)
            │       └── DataTemplate → FormMessageItemView
            │           [☐ IsSelected] [TextBox Text] [☐ 정규표현식]
            │
            ├── Separator
            │
            └── [Section] 직원 메시지 변환 (replaceStaffMessages)
                ├── Label "직원 메시지 변환"
                ├── [ActionBar] StackPanel(Horizontal): 추가 / 삭제 / 위로 / 아래로 / 모두선택 버튼
                └── ItemsControl (ReplaceStaffMessages)
                    └── DataTemplate → ReplaceStaffMessageItemView
                        Border (IsSelected → Background 강조)
                        └── Grid (2열: 선택 체크박스 | 필드 그룹)
                            ├── [☐ IsSelected]
                            └── StackPanel(Vertical)
                                ├── Row: "메모" — [☐ 추가] or [TextBox Comment] (HasComment로 전환)
                                ├── Row: "찾을 메시지" — [TextBox Pattern] [☐ 정규표현식]
                                └── Row: "바꿀 메시지" — [☐ 추가] or ([TextBox Replacement] [☐ 정규표현식]) (HasReplacement로 전환)
```

**항목 선택 UI**:
- `FormMessageItem`: 왼쪽에 `CheckBox`(IsSelected 바인딩)
- `ReplaceStaffMessageItem`: 왼쪽에 `CheckBox`(IsSelected 바인딩) + Border Background Trigger

**"추가" CheckBox ↔ TextBox 전환**:
- `Visibility` Converter (`bool → Visibility`) 또는 DataTrigger로 처리
- TextBox의 `LostFocus` 이벤트: Code-behind에서 ViewModel 속성 업데이트 (또는 UpdateSourceTrigger=LostFocus 바인딩으로 처리)

**파일**: `Summarizer.App/AppSettingsDialog.xaml.cs`

```csharp
public partial class AppSettingsDialog : Window
{
    public AppSettingsDialog(AppSettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
```

---

### [신규] 4-4. `MainWindowViewModel.cs` 수정

**`OpenSettingsFileCommand`** 구현 교체:

```csharp
OpenSettingsFileCommand = new RelayCommand(() =>
{
    var dialogViewModel = new AppSettingsDialogViewModel(settingsFilePath);
    var dialog = new AppSettingsDialog(dialogViewModel);
    ApplyThemeToDialog(dialog);
    dialog.ShowDialog();

    // Dialog 닫힌 후: 설정 재로드 + MessageConverter 재생성
    settings = AppSettingsLoader.Load(settingsFilePath);
    converter = new MessageConverter(settings);
});
```

> `converter` 필드를 `readonly` → 일반 필드로 변경

**`ApplyThemeToDialog` 헬퍼** 추가:

```csharp
private void ApplyThemeToDialog(Window dialog)
{
    var uri = App.ResolveThemeUri(settings.Theme);  // App의 정적 메서드 접근 (또는 applyTheme 콜백 활용)
    dialog.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
}
```

> `App.ResolveThemeUri`를 `internal static`으로 공개하거나, `MainWindowViewModel` 생성자에서 수신한 `applyTheme` 콜백의 URI를 별도로 받는 방식 중 선택.  
> 권장: `App.xaml.cs`의 `ResolveThemeUri`를 `internal static`으로 노출

---

### [신규] 4-5. `App.xaml.cs` 수정

- `ResolveThemeUri` 메서드를 `private` → `internal static`으로 변경
  (다이얼로그에 테마 ResourceDictionary를 적용하기 위해 `MainWindowViewModel`에서 접근)

---

## Story 5 — 버전 업데이트 및 빌드 검증

### [신규] 5-1. 버전 v1.2.3으로 업데이트

- `App.xaml.cs`: `Version = "1.2.3"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.3)"`
- `Summarizer.App.csproj`: `<FileVersion>1.2.3</FileVersion>`, `<AssemblyVersion>1.2.3</AssemblyVersion>`
- `Summarizer.Core.csproj`: `<FileVersion>1.2.3</FileVersion>`, `<AssemblyVersion>1.2.3</AssemblyVersion>`

### [신규] 5-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [신규] 5-3. 동작 검증

- 구 `AppSettings.json` (`replaceMessages` 키 포함) 로드 → `replaceStaffMessages`로 정상 마이그레이션 확인
- `formMessages`에 `"regex:"` 항목 추가 → 정규표현식 매칭으로 폼 메시지 제거 확인
- `생년월일 표준화` ON/OFF → AppSettings.json 저장 및 변환 동작 확인
- `파일 > 설정` 메뉴 → `AppSettingsDialog` 열림 확인
  - 각 항목 편집 후 닫기 → AppSettings.json 갱신 확인
  - 항목 추가 / 삭제 / 위로 / 아래로 / 모두선택 동작 확인
  - `메모` 및 `바꿀 메시지` 추가 CheckBox ↔ TextBox 전환 동작 확인
  - `JSON 파일로 열기` 버튼 → 텍스트 에디터로 파일 열림 확인
  - 테마 변경 후 다이얼로그 재열기 → 새 테마 적용 확인
- 기존 기능(변환 / 복사 / 직원 이름 저장 / 배치 CLI) 정상 동작 확인

---

## 결정 필요 사항

다음 항목은 구현 전 확인이 필요하다. 기본 방향을 제시하였으니, 다른 의견이 있을 경우 알려 달라.

1. **`AppSettingsDialog` 닫기 동작**: 항상 저장 후 닫기 (취소 버튼 없음).  
   → 이의 없으면 그대로 진행한다.

2. **`NormalizeBirthNumber` 위치**: `AppSettingsDialog` 내에 체크박스로 배치.  
   → 메인 창에는 추가하지 않는다.

3. **선택 UI**: 각 리스트 아이템 왼쪽에 CheckBox (`IsSelected` 바인딩).  
   → 배경색 변경 방식 대신 CheckBox를 사용한다.

4. **`ReplaceStaffMessageItem` 표시 방식**: 아이템별 세로 카드(Border + StackPanel).  
   → 필드가 많아 가로 1행으로 배치하면 가독성이 나쁘다.
