# Summarizer v1.2.4 — 구현 계획 (Plan)

> research.md 분석 완료 기준으로 작성된 구현 계획이다.
> 각 스토리는 `[신규]` 태그로 표시되며, 작업 진행 시 `[진행]` / `[완료]`로 갱신한다.

---

## 분석 요약

### 기능 개요

`강남언니` 서비스의 고객 예약 알림 메시지를 기존 KakaoTalk Business 메시지와 동일한 입력/출력 UI로 처리한다.
메시지 첫 줄로 종류를 구분하며, 각 종류별로 다른 변환 규칙을 적용한다.

### 메시지 종류별 감지 및 변환 규칙

| 종류 | 첫 줄 | 출력 1번째 줄 | 출력 2번째 줄 |
|------|-------|--------------|--------------|
| 신규 예약 | `고객이 예약을 신청했어요.` | `강남언니 / 이름 / 전화번호 / 원장명 \| 진료항목` | 희망 일시 목록 |
| 예약 변경 | `고객이 일정 변경을 요청했어요.` | `강남언니 / 이름 / 전화번호 / 예약 변경` | 변경 희망 일시 목록 |
| 예약 취소 | `고객이 예약을 취소했어요.` | `강남언니 / 이름 / 전화번호 / 예약 취소` | (없음) |

### `[예약 항목]` 파싱 규칙

원본 형식: `(병원) | (의사) 원장명 | (이벤트) 눈성형 중점 진료항목1 진료항목2 ...`

- 구분자 ` | `로 분할하여 각 부분을 분류
- `(의사) ` 접두어 부분: 접두어 제거 후 원장 성명 추출
- `(이벤트) 눈성형 중점 ` 접두어 부분: 접두어 제거 후 진료항목 추출
- `(병원)` 부분: 출력에서 생략
- 신규 예약 출력: `원장명 | 진료항목` (한쪽만 있으면 해당 값만, 둘 다 없으면 4번째 필드 생략)

### 희망 일시 파싱 규칙

- `[희망 일시] :` 또는 `[변경 희망 일시] :` 레이블 줄 이후, `N.` 으로 시작하는 줄들을 수집
- 출력: `1. 날짜1 시간1 / 2. 날짜2 시간2 / 3. 날짜3 시간3 ...`

### 반환 문자열 구조

- 신규 예약 / 예약 변경: 두 줄을 `\n`으로 결합하여 반환
- 예약 취소: 단일 줄 반환
- `Convert()` 메서드는 기존과 동일하게 단일 `string`을 반환하며, 출력 TextBox의 줄바꿈 표시로 처리됨

### 확장성 설계

`IRecruitmentMessageConverter` 인터페이스를 도입하여, 향후 새 모객 서비스 추가 시 구현체만 추가하면 되는 구조로 만든다.
`MessageConverter` 생성자에서 구현체 배열을 초기화하며, 설정(AppSettings) 의존성 없이 동작한다.

---

## Story 1 — `IRecruitmentMessageConverter` 인터페이스 설계

### [신규] 1-1. `IRecruitmentMessageConverter.cs` 신규 생성

**파일**: `Summarizer.Core/RecruitmentConverters/IRecruitmentMessageConverter.cs`  
**네임스페이스**: `Summarizer.Core.RecruitmentConverters`

```csharp
public interface IRecruitmentMessageConverter
{
    // 첫 줄 텍스트를 받아 이 변환기가 처리 가능한 메시지인지 판별
    bool CanConvert(string firstLine);

    // 전체 텍스트를 받아 변환 결과 문자열을 반환
    string Convert(string text);
}
```

---

## Story 2 — `GangnamUnniMessageConverter` 구현

### [신규] 2-1. `GangnamUnniMessageConverter.cs` 신규 생성

**파일**: `Summarizer.Core/RecruitmentConverters/GangnamUnniMessageConverter.cs`  
**네임스페이스**: `Summarizer.Core.RecruitmentConverters`

#### 감지 상수 (private const)

```csharp
private const string NewReservationFirstLine    = "고객이 예약을 신청했어요.";
private const string ChangeReservationFirstLine = "고객이 일정 변경을 요청했어요.";
private const string CancelReservationFirstLine = "고객이 예약을 취소했어요.";
```

#### `CanConvert(string firstLine)`

```csharp
return firstLine == NewReservationFirstLine
    || firstLine == ChangeReservationFirstLine
    || firstLine == CancelReservationFirstLine;
```

#### `Convert(string text)`

1. 텍스트를 줄 단위로 분할 (`\r\n`, `\r`, `\n` 기준, 빈 줄 · 앞뒤 공백 제거)
2. 첫 줄로 분기:
   - `NewReservationFirstLine` → `ConvertNewReservation(lines)`
   - `ChangeReservationFirstLine` → `ConvertChangeReservation(lines)`
   - `CancelReservationFirstLine` → `ConvertCancelReservation(lines)`
   - 그 외 → `string.Empty`

#### `ConvertNewReservation(string[] lines)` (private static)

1. `ExtractFieldValue(lines, "고객명")` → 이름
2. `ExtractFieldValue(lines, "연락처")` → 전화번호
3. `ExtractFieldValue(lines, "예약 항목")` → `ParseReservationItem(value)` → `(원장명, 진료항목)`
4. `ExtractDateLines(lines, "희망 일시")` → 날짜 목록
5. 출력 1번째 줄 조립:
   - 4번째 필드: 원장명과 진료항목 모두 있으면 `원장명 | 진료항목`, 하나만 있으면 그 값, 둘 다 없으면 필드 자체 생략
   - `강남언니 / {이름} / {전화번호}` + (4번째 필드가 있으면 `/ {4번째 필드}`)
6. 출력 2번째 줄 조립: `JoinDateLines(날짜 목록)`
7. `\n`으로 두 줄 결합하여 반환

#### `ConvertChangeReservation(string[] lines)` (private static)

1. `ExtractFieldValue(lines, "고객명")` → 이름
2. `ExtractFieldValue(lines, "연락처")` → 전화번호
3. `ExtractDateLines(lines, "변경 희망 일시")` → 날짜 목록
4. 출력 1번째 줄: `강남언니 / {이름} / {전화번호} / 예약 변경`
5. 출력 2번째 줄: `JoinDateLines(날짜 목록)`
6. `\n`으로 두 줄 결합하여 반환

#### `ConvertCancelReservation(string[] lines)` (private static)

1. `ExtractFieldValue(lines, "고객명")` → 이름
2. `ExtractFieldValue(lines, "연락처")` → 전화번호
3. 출력: `강남언니 / {이름} / {전화번호} / 예약 취소`

#### 헬퍼 메서드들 (private static)

**`ExtractFieldValue(string[] lines, string fieldLabel)`**

```
prefix = $"[{fieldLabel}] : "
lines에서 prefix로 시작하는 첫 줄을 찾아, prefix 이후 텍스트를 반환
찾지 못하면 null 반환
```

**`ParseReservationItem(string? fieldValue)`** → `(string? doctor, string? treatments)`

```
fieldValue가 null이면 (null, null) 반환
" | "로 분할
각 부분을 순회하며:
  - "(의사) " 접두어 → 접두어 제거 후 doctor에 저장
  - "(이벤트) 눈성형 중점 " 접두어 → 접두어 제거 후 treatments에 저장
  - "(병원)" → 무시
(doctor, treatments) 반환
```

**`ExtractDateLines(string[] lines, string fieldLabel)`** → `List<string>`

```
prefix = $"[{fieldLabel}] : "
lines에서 prefix로 시작하는 줄의 다음 줄부터 순회
^\d+\.\s+(.+)$ 패턴과 일치하는 줄들을 수집 (캡처 그룹만 추출)
다른 패턴의 줄이 나오면 수집 중단
결과 반환 (빈 경우 빈 리스트)
```

**`JoinDateLines(List<string> dateLines)`** → `string`

```
각 항목에 "N. " 접두어 붙이고, " / "로 연결
dateLines가 비어 있으면 string.Empty 반환
예: "1. 2025. 5. 12 (월) 오전 10:00 / 2. 2025. 5. 13 (화) 오후 02:00"
```

<!-- 
 원본 모객 메시지에서 이미 각 희망 일시의 텍스트마다 1. 2. 3. ~ 식으로 번호를 매겨두고 있다.
 JoinDateLines() 구현에 의해 순번 텍스트가 중복으로 출력되지 않도록 주의할 것.
-->
> `ExtractDateLines`가 이미 내용만 추출하므로 `JoinDateLines`에서 인덱스 기반으로 "N. " 접두어를 붙인다.

---

## Story 3 — `MessageConverter` 통합

### [신규] 3-1. `MessageConverter.cs` 수정

**필드 추가**:

```csharp
private readonly IRecruitmentMessageConverter[] recruitmentConverters;
```

**생성자 수정**:

```csharp
using Summarizer.Core.RecruitmentConverters;

// ...

recruitmentConverters = [new GangnamUnniMessageConverter()];
```

**`Convert()` 메서드 수정** — KakaoTalk 감지 이후, `ConvertCustomerText` 이전에 모객 메시지 감지 분기 추가:

```csharp
public string Convert(string text)
{
    if (string.IsNullOrEmpty(text))
        return string.Empty;

    // 1. KakaoTalk Business 메시지 감지 (기존 로직 유지)
    var matches = KakaoTalkMessageTimeRegex().Matches(text);
    if (matches.Count > 0)
    {
        // ... 기존 코드 ...
        return string.Join(" / ", convertedTexts);
    }

    // 2. 모객 서비스 메시지 감지
    var firstLine = GetFirstLine(text);
    foreach (var recruitmentConverter in recruitmentConverters)
    {
        if (recruitmentConverter.CanConvert(firstLine))
            return recruitmentConverter.Convert(text);
    }

    // 3. 기존 고객 텍스트 처리
    return ConvertCustomerText(text);
}
```

**`GetFirstLine(string text)` 헬퍼 추가** (private static):

```csharp
private static string GetFirstLine(string text)
{
    var newlineIndex = text.IndexOfAny(['\r', '\n']);
    return newlineIndex >= 0 ? text[..newlineIndex].Trim() : text.Trim();
}
```

---

## Story 4 — 버전 업데이트 및 빌드 검증

### [신규] 4-1. 버전 v1.2.4로 업데이트

- `App.xaml.cs`: `Version = "1.2.4"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.4)"`
- `Summarizer.App.csproj`: `1.2.3` → `1.2.4`
- `Summarizer.Core.csproj`: `1.2.3` → `1.2.4`

### [신규] 4-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [신규] 4-3. 동작 검증 (가상 시나리오)

아래 [가상] 시나리오를 입력하여 변환 결과를 확인한다.

**[가상] 신규 예약 입력**:
```
고객이 예약을 신청했어요.
예약 일시를 확정해 주세요.

[고객명] : 홍길순
[연락처] : 010-1234-5678
[예약 항목] : (병원) | (의사) 김가나 | (이벤트) 눈성형 중점 쌍꺼풀 눈매교정
[희망 일시] : 
1. 2025. 6. 10 (화) 오전 10:00
2. 2025. 6. 11 (수) 오후 02:00
3. 2025. 6. 12 (목) 오전 11:00
```

**[가상] 기대 출력**:
```
강남언니 / 홍길순 / 010-1234-5678 / 김가나 | 쌍꺼풀 눈매교정
1. 2025. 6. 10 (화) 오전 10:00 / 2. 2025. 6. 11 (수) 오후 02:00 / 3. 2025. 6. 12 (목) 오전 11:00
```

**[가상] 예약 변경 입력**:
```
고객이 일정 변경을 요청했어요.
변경 희망 일시를 확정해 주세요.

[고객명] : 홍길순
[연락처] : 010-1234-5678
[예약 항목] : (병원) | (의사) 김가나 | (이벤트) 눈성형 중점 쌍꺼풀
[변경 희망 일시] : 
1. 2025. 6. 15 (일) 오전 10:00
2. 2025. 6. 16 (월) 오후 03:00
```

**[가상] 기대 출력**:
```
강남언니 / 홍길순 / 010-1234-5678 / 예약 변경
1. 2025. 6. 15 (일) 오전 10:00 / 2. 2025. 6. 16 (월) 오후 03:00
```

**[가상] 예약 취소 입력**:
```
고객이 예약을 취소했어요.
취소 내역을 확인해 주세요.

[고객명] : 홍길순
[연락처] : 010-1234-5678
[예약 항목] : (병원) | (의사) 김가나 | (이벤트) 눈성형 중점 쌍꺼풀
[취소된 내원 일시] : 확정 전 취소
```

**[가상] 기대 출력**:
```
강남언니 / 홍길순 / 010-1234-5678 / 예약 취소
```

---

## 파일 변경 목록

### 신규 생성
- `Summarizer.Core/RecruitmentConverters/IRecruitmentMessageConverter.cs`
- `Summarizer.Core/RecruitmentConverters/GangnamUnniMessageConverter.cs`

### 수정
- `Summarizer.Core/MessageConverter.cs` — 모객 메시지 분기 추가
- `Summarizer.App/App.xaml.cs` — Version
- `Summarizer.App/MainWindow.xaml` — Title
- `Summarizer.App/Summarizer.App.csproj` — 버전
- `Summarizer.Core/Summarizer.Core.csproj` — 버전

---

## 결정 필요 사항

다음 항목은 구현 전 확인이 필요하다.

<!-- 승인함 -->
1. **`[예약 항목]`에 원장명 및 진료항목이 모두 없을 경우 처리**  
   → 기본 방향: 4번째 필드를 생략하고 `강남언니 / 이름 / 전화번호` 3개 필드만 출력.  
   → 이의 없으면 그대로 진행한다.

<!-- 승인함 -->
2. **`Convert()` 반환값이 두 줄일 때 기존 기능 영향 여부**  
   → 반환값은 `string`이고, 출력 TextBox는 이미 `TextWrapping="Wrap"` + `IsReadOnly="True"` 상태이므로 `\n` 포함 문자열 표시에 문제 없음.  
   → `CopyOutputCommand`로 클립보드에 복사 시에도 `\n` 포함된 채 복사되어 메모 앱 등에 붙여넣기 가능.  
   → 이의 없으면 그대로 진행한다.