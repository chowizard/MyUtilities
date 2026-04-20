## Summarizer v1.2.0 계획

---

## 기본사항

- 사용 버전 : v1.2.0
- 이전 버전 : v1.1.2
- 이전 버전에서 작업했던 내용들을 이어서, 여기에서 작업해라.

---

## 목표사항

- .NET 10 및 C# 14를 사용하는 프로젝트로 전환
- WinForm -> WPF 사용하는 프로젝트로 전환
  - 아키텍처: MVVM
  - WinForms 관련 파일 및 설정은 모두 제거
- 비즈니스 로직을 별도 라이브러리 프로젝트(`Summarizer.Core`)로 분리
  - 향후 CLI / Web 기반 도구 등으로 확장할 수 있는 가능성에 대비
- 설정 파일 형식을 변경
  - XML 문서 기반의 AppSettings.settings → JSON 기반의 AppSettings.json
  - AppSettings.Designer 사용하는 방식이 아닌, 순수하게 JSON 데이터를 편집하는 방식으로 전환
  - AppSettings.json이 없을 경우: 기본값 fallback 적용 후, 그 값으로 AppSettings.json을 생성

---

## 기존 구현 분석 (v1.1.2 기준)

v1.1.2의 상세 분석은 [../v1.1.2/research.md](../v1.1.2/research.md)를 참고한다.

### 현재 구조 요약

```
Summarizer/
├── Program.cs              # 진입점 ([STAThread] Main, ApplicationConfiguration.Initialize)
├── Configuration.cs        # 설정 데이터 POCO
├── FormMain.cs             # WinForms 메인 폼 + 모든 텍스트 변환 로직 통합
├── FormMain.Designer.cs    # 디자이너 자동생성 (직접 편집 금지)
├── FormMain.resx           # 리소스 파일
├── App.config              # XML 앱 설정
├── AppSettings.settings    # VS 디자이너 스키마
└── AppSettings.Designer.cs # System.Configuration 기반 자동생성 접근자
```

### 현재 UI 구성 요소

| 컨트롤 | 용도 |
|--------|------|
| `textBoxInput` | 카카오톡 메시지 붙여넣기 입력창 (다중행) |
| `textBoxOutput` | 변환 결과 출력창 (읽기전용) |
| `buttonConvert` | 변환 실행 |
| `buttonClear` | 입력/출력 모두 비우기 |
| `buttonCopyOutput` | 출력 결과 클립보드 복사 |
| `checkBoxAlwaysTop` | 항상 위 토글 (TopMost) |

### 현재 설정 구조의 문제점

- `AppSettings.settings` → `AppSettings.Designer.cs` 자동 생성 체인이 Visual Studio에 강하게 의존
- 배열 데이터(`StringCollection`)가 내부적으로 XML을 재직렬화하는 이중 구조 (가독성 나쁨)
- `System.Configuration.ApplicationSettingsBase`는 .NET Core/5+ 에서 레거시 지원으로만 유지
- 비즈니스 로직(`FormMain.cs`)이 UI 코드와 뒤섞여 있어 재사용 불가

---

## 목표 1: .NET 10 / C# 14 전환

### .csproj 변경 내용

| 항목 | 이전 | 이후 |
|------|------|------|
| TargetFramework | `net8.0-windows` | `net10.0-windows` (App 프로젝트) / `net10.0` (Core 프로젝트) |
| UseWindowsForms | `true` | 제거 |
| UseWPF | 없음 | `true` (App 프로젝트만) |
| ApplicationHighDpiMode | `SystemAware` | 제거 (WPF는 기본 DPI 인식) |
| ForceDesignerDpiUnaware | `true` | 제거 |
| FileVersion / AssemblyVersion | `1.1.2` | `1.2.0` |

### C# 14 주요 신기능 (이 프로젝트 활용 가능한 것)

| 기능 | 활용처 |
|------|-------|
| `field` 키워드 (semi-auto property) | ViewModel의 INotifyPropertyChanged 프로퍼티 — backing field 생략 가능 |
| Extension members | 파싱 유틸리티 메서드 구조 개선 시 검토 |
| `[GeneratedRegex]` | .NET 10에서도 동일하게 사용 가능. 변경 없음 |

---

## 목표 2: 프로젝트 구조 분리 (Core 라이브러리)

### 새 솔루션 구조

```
Summarizer/                          ← 솔루션 루트 (git 루트)
├── Summarizer.sln                   ← 두 프로젝트를 모두 포함하도록 업데이트
├── .claude/                         ← Claude 작업 디렉토리 (변경 없음)
│
├── Summarizer.Core/                 ← [신규] 비즈니스 로직 라이브러리
│   ├── Summarizer.Core.csproj      #  TargetFramework: net10.0 (Windows 비의존)
│   ├── AppSettings.cs              #  설정 POCO + 기본값
│   ├── AppSettingsLoader.cs        #  JSON 로딩/저장 로직
│   └── MessageConverter.cs         #  텍스트 변환 로직 (FormMain.cs에서 추출)
│
└── Summarizer.App/                  ← [신규] WPF 애플리케이션
    ├── Summarizer.App.csproj       #  TargetFramework: net10.0-windows, UseWPF: true
    ├── App.xaml / App.xaml.cs      #  진입점, 설정 로딩
    ├── MainWindow.xaml             #  UI 레이아웃
    ├── MainWindow.xaml.cs          #  Code-behind (최소화)
    ├── ViewModels/
    │   └── MainWindowViewModel.cs  #  바인딩 프로퍼티 + ICommand
    ├── Commands/
    │   └── RelayCommand.cs         #  ICommand 헬퍼 (외부 라이브러리 없이 직접 구현)
    └── AppSettings.json            #  설정 파일 (출력 디렉토리에 복사)
```

### 현재 파일 처리 방침

| 현재 파일 | 처리 |
|----------|------|
| `Program.cs` | 삭제 (WPF App.xaml이 진입점) |
| `Configuration.cs` | 삭제 (`Summarizer.Core/AppSettings.cs`로 대체) |
| `FormMain.cs` | 삭제 — 로직은 `MessageConverter.cs`로, UI는 `MainWindow.xaml`로 분리 |
| `FormMain.Designer.cs` | 삭제 |
| `FormMain.resx` | 삭제 |
| `App.config` | 삭제 |
| `AppSettings.settings` | 삭제 |
| `AppSettings.Designer.cs` | 삭제 |
| `Summarizer.csproj` | 삭제 → `Summarizer.App.csproj`으로 교체 |
| `Summarizer.sln` | 수정 (두 프로젝트 등록) |
| `.editorconfig` | 유지 |
| `icon.ico` | `Summarizer.App/`으로 이동 |

### `Summarizer.Core` 설계

**`AppSettings.cs`**
- JSON 역직렬화 대상 POCO
- `init` 프로퍼티 사용 (불변 객체)
- 기본값을 `static readonly Default` 프로퍼티로 제공

```csharp
public class AppSettings
{
    public string StaffName { get; init; } = "아무개";
    public string ReservationConfirmMessage { get; init; } = "채널로 예약문자 전송";
    public string[] FormMessages { get; init; } = [
        "상담받을 분의 성함 / 연락처 - ",
        "생년월일 - ",
        "상담부위 - ",
        "첫수술or 재수술 (재수술일경우 마지막 수술시기 ) - ",
        "상담 희망 날짜와 시간대 - ",
        "상담 원하는 원장님 - ",
        "저희 병원 알게되신 경로 - ",
        "소개자 있으실 경우 소개자 성함과 연락처 뒷번호 - "
    ];
    public string[] SliceStaffMessages { get; init; } = [
        "안녕하세요~", "안녕하세요^^", "안녕하세요~^^", "^^"
    ];
}
```

**`AppSettingsLoader.cs`**
- JSON 파일 로딩 / 저장
- 파일 없을 시: 기본값(`AppSettings` 기본 인스턴스) 사용 후 파일 생성
- `System.Text.Json` 사용 (외부 NuGet 없음)

**`MessageConverter.cs`**
- 현재 `FormMain.cs`의 `ConvertText()`, `ConvertCustomerText()`, `ConvertStaffText()` 등 추출
- `AppSettings`를 생성자 매개변수로 받음 (의존성 주입 가능 구조)
- UI 의존성 없음 → Windows 비의존 → `net10.0` 타겟팅 가능

### `Summarizer.App` 설계 (WPF + MVVM)

**`App.xaml.cs`**
- `AppSettingsLoader`로 설정 로딩
- `MainWindowViewModel`에 설정 전달
- `Version` 상수 보유 (현재 `Program.cs`의 역할)

**`MainWindowViewModel.cs`**
- `INotifyPropertyChanged` 구현
- 프로퍼티: `InputText`, `OutputText`, `IsAlwaysOnTop`
- 커맨드: `ConvertCommand`, `ClearCommand`, `CopyOutputCommand`
- `MessageConverter` 인스턴스 보유

**`RelayCommand.cs`**
- `ICommand` 구현체
- `Action` + `Func<bool>` 기반
- 외부 라이브러리(CommunityToolkit 등) 없이 직접 구현

---

## 목표 3: 설정 파일 JSON 전환

### 목표 JSON 구조 (`AppSettings.json`)

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

### 직렬화 방식

- `System.Text.Json` 직접 사용 (외부 NuGet 없음)
- `JsonSerializerOptions.Web` → camelCase 키 자동 매핑
- `AppSettings` POCO의 프로퍼티를 PascalCase로 유지하면서 JSON에서 camelCase 읽기 가능

### 설정 파일 위치

**실행파일 옆 (`AppSettings.json`)** 채택:
- 클리닉 직원이 직접 메모장으로 편집 가능
- 개인 또는 소수 지정 PC 사용 환경이므로 권한 문제 없음
- `.csproj`: `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

### 파일 없을 때 처리 (`AppSettingsLoader`)

```
AppSettings.json 존재 여부 확인
    ├─ 존재: 역직렬화하여 AppSettings 반환
    └─ 없음: AppSettings 기본값 인스턴스 생성
              → 해당 기본값으로 AppSettings.json 파일을 생성
              → AppSettings 반환
```

---

## WinForms → WPF 컨트롤 매핑

| WinForms | WPF XAML |
|----------|----------|
| `TextBox` (multiline) | `TextBox AcceptsReturn="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"` |
| `TextBox` (readonly) | `TextBox IsReadOnly="True"` |
| `Button` | `Button Command="{Binding XxxCommand}"` |
| `CheckBox` | `CheckBox IsChecked="{Binding IsAlwaysOnTop}"` |
| `Form.TopMost` | `Window.Topmost="{Binding IsAlwaysOnTop}"` |
| `Form.Text` (타이틀) | `Window.Title` (정적 또는 바인딩) |
| `Clipboard.SetText()` | `Clipboard.SetText()` (WPF에서도 동일) |

---

## 리스크 및 주의사항

1. **`[GeneratedRegex]` 호환성**: .NET 10에서도 동일 작동. 변경 없음.
2. **`StringCollection` → `string[]`**: 기존 패턴(`CopyTo` 등) 제거. JSON 배열로 자연스럽게 대체.
3. **WPF + WinForms 공존 불가**: `UseWindowsForms`와 `UseWPF`를 동시에 `true`로 설정하면 충돌. WinForms 관련 설정 및 파일 완전 제거 필요.
4. **`Summarizer.Core` 타겟 `net10.0`**: Windows API를 사용하지 않도록 주의. `Clipboard`, `Application` 등 WPF/WinForms 전용 API는 Core에 포함하지 말 것.
5. **솔루션 파일 재구성**: 현재 `Summarizer.sln`은 단일 프로젝트만 참조. 두 개의 새 프로젝트(`Summarizer.Core`, `Summarizer.App`)를 등록하고, 기존 프로젝트 참조는 제거해야 함.
6. **`icon.ico` 이동**: 현재 루트에 있는 아이콘 파일을 `Summarizer.App/`으로 이동. `.csproj`의 `<ApplicationIcon>` 경로 업데이트 필요.
