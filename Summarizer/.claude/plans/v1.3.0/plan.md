# Summarizer v1.3.0 — 구현 계획 (Plan)

> `research.md`(사용자 작성)의 목표사항을 기준으로 작성한 구현 계획이다.
> 각 스토리는 `[신규]` 태그로 표시되며, 작업 진행 시 `[진행]` / `[완료]`로 갱신한다.

---

## 분석 요약

### 현재 구조 (v1.2.5 기준)

`GangnamUnniMessageConverter.cs`에서 강남언니 메시지 필드를 추출할 때 사용하는 레이블 텍스트들이 소스 코드에 직접 하드코딩되어 있다.

```csharp
// 현재 코드 (하드코딩 예시)
var name  = ExtractFieldValue(lines, "고객명") ?? string.Empty;
var phone = ExtractFieldValue(lines, "연락처") ?? string.Empty;
```

- `ExtractFieldValue()`는 `[{fieldLabel}] : ` 접두어로 시작하는 줄을 찾아 값을 반환한다.
- `AppSettings`에는 강남언니 관련 필드 레이블 설정이 없다.
- `MessageConverter.cs`에서 `new GangnamUnniMessageConverter()`로 의존성 없이 생성한다.

### 변경 필요 사항

강남언니 서비스의 메시지 형식이 변경되면서 필드 레이블이 바뀌었다.

| 항목 | 기존 레이블 | 변경 후 레이블 |
|------|------------|--------------|
| 고객 이름 | `[고객명]` | `[방문자명]` |
| 연락처 | `[연락처]` | `[방문자 전화번호]` |

이전/이후 레이블 모두 동일한 항목으로 인식해야 하므로, 각 레이블 항목에 복수의 키워드를 배열로 지원하는 방식으로 설계한다.

### `희망 일시 중 가능한 시간을 선택해 주세요.` 항목 처리

메시지 말미에 추가된 이 항목은 번호 없는 일반 텍스트로 보인다. 현재 `ExtractDateLines()`는 `^\d+\.\s+(.+)$` 패턴과 일치하지 않는 줄에서 `break`를 호출하므로, 이 항목이 번호 없이 나타난다면 자동으로 필터링된다. 가상 시나리오 검증에서 확인한다.

---

## Story 1 — `GangnamUnniFieldLabels` 클래스 추가 및 `AppSettings` 연동

### [신규] 1-1. `GangnamUnniFieldLabels.cs` 신규 생성

**파일**: `Summarizer.Core/GangnamUnniFieldLabels.cs`
**네임스페이스**: `Summarizer.Core`

```csharp
public class GangnamUnniFieldLabels
{
    public string[] CustomerName            { get; set; } = ["고객명", "방문자명"];
    public string[] PhoneNumber             { get; set; } = ["연락처", "방문자 전화번호"];
    public string[] ReservationItem         { get; set; } = ["예약 항목"];
    public string[] PreferredDateTime       { get; set; } = ["희망 일시"];
    public string[] ChangePreferredDateTime { get; set; } = ["변경 희망 일시"];
}
```

- 각 프로퍼티는 하나 이상의 레이블 텍스트를 허용한다.
- 기본값에 기존 레이블과 변경 후 레이블을 모두 포함한다.
- `ReplaceMessage.cs`와 동일한 패턴으로 `Summarizer.Core` 네임스페이스에 별도 파일로 생성한다.

### [신규] 1-2. `AppSettings.cs` 수정

`AppSettings` 클래스에 `GangnamUnniFieldLabels` 프로퍼티를 추가한다.

```csharp
public GangnamUnniFieldLabels GangnamUnniFieldLabels { get; set; } = new();
```

- `AppSettingsLoader`는 `JsonNamingPolicy.CamelCase`를 사용하므로, JSON 키는 `gangnamUnniFieldLabels`로 직렬화된다.
- 기존 `AppSettings.json`에 이 키가 없으면 역직렬화 시 기본값을 사용한다. (마이그레이션 불필요)

### [신규] 1-3. `AppSettings.json` 기본값 추가

`Summarizer.App/AppSettings.json`에 `gangnamUnniFieldLabels` 항목을 추가한다.

```json
"gangnamUnniFieldLabels": {
    "customerName": ["고객명", "방문자명"],
    "phoneNumber": ["연락처", "방문자 전화번호"],
    "reservationItem": ["예약 항목"],
    "preferredDateTime": ["희망 일시"],
    "changePreferredDateTime": ["변경 희망 일시"]
}
```

- `AppSettings.cs` 기본값과 동일해야 한다.

---

## Story 2 — `GangnamUnniMessageConverter` 수정

### [신규] 2-1. 생성자에 `GangnamUnniFieldLabels` 주입

```csharp
private readonly GangnamUnniFieldLabels fieldLabels;

public GangnamUnniMessageConverter(GangnamUnniFieldLabels fieldLabels)
{
    this.fieldLabels = fieldLabels;
}
```

### [신규] 2-2. `ExtractFieldValue()` 다중 레이블 지원으로 수정

단일 `fieldLabel` 파라미터를 `string[] fieldLabels` 배열로 변경하고, 배열 내 모든 레이블에 대해 순차 탐색한다.

```csharp
private static string? ExtractFieldValue(string[] lines, string[] fieldLabels)
{
    foreach (var fieldLabel in fieldLabels)
    {
        var prefix = $"[{fieldLabel}] : ";
        foreach (var line in lines)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return line[prefix.Length..].Trim();
        }
    }
    return null;
}
```

기존 호출부를 `fieldLabels` 프로퍼티를 사용하도록 변경한다.

```csharp
var name  = ExtractFieldValue(lines, fieldLabels.CustomerName) ?? string.Empty;
var phone = ExtractFieldValue(lines, fieldLabels.PhoneNumber)  ?? string.Empty;
// ... 이하 동일
```

`ExtractDateLines()` 또한 같은 방식으로 `string[] fieldLabels`를 받도록 수정한다.

### [신규] 2-3. `MessageConverter.cs` 수정

`GangnamUnniMessageConverter` 생성 시 `AppSettings`의 필드 레이블 설정을 주입한다.

```csharp
// 기존
recruitmentConverters = [new GangnamUnniMessageConverter()];

// 변경 후
recruitmentConverters = [new GangnamUnniMessageConverter(settings.GangnamUnniFieldLabels)];
```

---

## Story 3 — 버전 업데이트 및 빌드 검증

### [신규] 3-1. 버전 v1.3.0으로 업데이트

- `App.xaml.cs`: `Version = "1.3.0"`
- `MainWindow.xaml`: `Title="Summarizer (v1.3.0)"`
- `Summarizer.App.csproj`: `1.2.5` → `1.3.0`
- `Summarizer.Core.csproj`: `1.2.5` → `1.3.0`

### [신규] 3-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [신규] 3-3. 동작 검증 (가상 시나리오)

모든 시나리오는 `[가상]` 인물·내용만 사용한다.

**(a) 변경 후 레이블 인식 — 신규 예약**

| 입력 | 기대 결과 |
|------|----------|
| `[방문자명] : 홍길순` | `홍길순` 추출 |
| `[방문자 전화번호] : 010-1234-5678` | `010-1234-5678` 추출 |

**(b) 기존 레이블 인식 (회귀) — 신규 예약**

| 입력 | 기대 결과 |
|------|----------|
| `[고객명] : 홍길순` | `홍길순` 추출 |
| `[연락처] : 010-1234-5678` | `010-1234-5678` 추출 |

**(c) `희망 일시 중 가능한 시간을 선택해 주세요.` 항목 필터링**

[가상] 신규 예약 메시지 (변경 후 형식):

```
고객이 예약을 신청했어요.
예약 일시를 확정해 주세요.

[방문자명] : 홍길순
[방문자 전화번호] : 010-1234-5678
[예약 항목] : (병원) | (의사) 김가나 | (이벤트) 눈성형 중점 쌍꺼풀
[희망 일시] :
1. 2026. 12. 10 (목) 오전 10:00
2. 2026. 12. 11 (금) 오후 02:00
희망 일시 중 가능한 시간을 선택해 주세요.
```

기대 출력:
```
강남언니 / 홍길순 / 010-1234-5678 / 김가나 | 쌍꺼풀
1. 2026. 12. 10 (목) 오전 10:00 / 2. 2026. 12. 11 (금) 오후 02:00
```

- `희망 일시 중 가능한 시간을 선택해 주세요.`는 결과에서 제거되어야 한다.

---

## Story 4 — 설정 GUI에서 `gangnamUnniFieldLabels` 편집 지원

기존 설정 GUI(`AppSettingsDialog`)의 목록 편집 방식(체크박스 선택 + 추가/삭제/위로/아래로/모두선택 버튼, 창 닫을 때 자동 저장)과 동일한 방식으로 동작한다.

### [완료] 4-1. 레이블 편집용 뷰모델 추가 및 저장/불러오기 연동

- `FieldLabelItemViewModel` (`IsSelected`, `Text`), `FieldLabelGroupViewModel` (제목, 레이블 목록, 편집 커맨드 5종) 신규 생성
- `AppSettingsDialogViewModel`: 5개 그룹(`CustomerNameLabels` 등) 노출, `LoadFromSettings()`/`SaveToSettings()`에서 `GangnamUnniFieldLabels`와 연동
- 저장 시 빈 텍스트는 제외하고 앞뒤 공백을 제거한다.

### [완료] 4-2. 설정 GUI에 편집 영역 추가

- `AppSettingsDialog.xaml`: "상담 메시지 형식"과 "직원 메시지 변환" 사이에 "강남언니 메시지 필드 레이블" 영역 추가
- 그룹별 `DataTemplate`을 공용으로 사용 (고객 이름 / 연락처 / 예약 항목 / 희망 일시 / 변경 희망 일시)
- 레이블은 `[레이블] : ` 접두어 일치(일반 텍스트) 방식이므로 정규표현식 체크박스는 제공하지 않는다.

### [완료] 4-3. 빌드 검증

- Debug / Release 빌드: 경고 0, 오류 0

---

## 파일 변경 목록

### 신규 생성
- `Summarizer.Core/GangnamUnniFieldLabels.cs`
- `Summarizer.App/ViewModels/FieldLabelItemViewModel.cs` (Story 4)
- `Summarizer.App/ViewModels/FieldLabelGroupViewModel.cs` (Story 4)

### 수정
- `Summarizer.Core/AppSettings.cs` — `GangnamUnniFieldLabels` 프로퍼티 추가
- `Summarizer.App/AppSettings.json` — `gangnamUnniFieldLabels` 기본값 추가
- `Summarizer.Core/RecruitmentConverters/GangnamUnniMessageConverter.cs` — 생성자 주입, 다중 레이블 지원
- `Summarizer.Core/MessageConverter.cs` — `GangnamUnniMessageConverter` 생성자 호출 수정
- `Summarizer.App/ViewModels/AppSettingsDialogViewModel.cs` — 필드 레이블 그룹 연동 (Story 4)
- `Summarizer.App/AppSettingsDialog.xaml` — 필드 레이블 편집 영역 추가 (Story 4)
- `Summarizer.App/App.xaml.cs` — Version
- `Summarizer.App/MainWindow.xaml` — Title
- `Summarizer.App/Summarizer.App.csproj` — 버전
- `Summarizer.Core/Summarizer.Core.csproj` — 버전

---

## 결정 필요 사항

1. **기존 사용자 `AppSettings.json`의 처리**
   → 기존 JSON에 `gangnamUnniFieldLabels` 키가 없으면 역직렬화 시 기본값(`AppSettings.cs`의 `new()`)이 사용된다. 별도 마이그레이션은 구현하지 않는다.
   → 단, 기존 파일을 수동으로 편집하거나 설정 저장을 통해 새 항목이 반영된다.

2. **`희망 일시 중 가능한 시간을 선택해 주세요.` 처리**
   → 현재 `ExtractDateLines()`에서 번호 없는 줄이 나타나면 자동으로 수집을 중단(`break`)하므로 별도 처리가 필요 없는 것으로 판단한다.
   → 3-3(c) 가상 시나리오 검증에서 확인한다. 만약 이 항목이 번호가 붙어 등장한다면 별도 필터링 로직이 필요하므로 지시해 달라.

3. **`CanConvert()`의 감지 첫 줄 텍스트 설정 여부**
   → `고객이 예약을 신청했어요.` 등 `CanConvert()`에서 사용하는 첫 줄 텍스트들은 `research.md`에서 변경 대상으로 명시되지 않았으므로 이번 버전에서는 소스 코드 const로 유지한다.
   → 향후 필요하다면 별도로 지시해 달라.
