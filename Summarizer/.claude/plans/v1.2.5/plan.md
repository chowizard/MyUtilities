# Summarizer v1.2.5 — 구현 계획 (Plan)

> `research.md`(사용자 작성)의 목표사항을 기준으로 작성한 구현 계획이다.
> 각 스토리는 `[신규]` 태그로 표시되며, 작업 진행 시 `[진행]` / `[완료]`로 갱신한다.

---

## 분석 요약

### 목표 1 — `formMessages` 기본값 변경 (정규표현식화)

현재 구조 (v1.2.4 기준):

- `AppSettings.FormMessages` (`AppSettings.cs`) 와 `AppSettings.json`에 동일한 기본값 8건이 존재한다.
- `MessageConverter.ParseFormMatcher()`는 `regex:` 접두어가 붙은 항목을 정규표현식(`Regex`)으로, 그 외는 평문(`string.Contains` / `Replace`)으로 취급한다. **이 기능은 이미 구현되어 있으므로 새로 만들 필요가 없다.**
- `ApplyFormMatcher()`는 매칭된 부분을 `string.Empty`로 치환하여 레이블을 제거한다.
- 설정창(`AppSettingsDialogViewModel`)은 `regex:` 접두어를 인식하여 체크박스로 표시/저장한다. 따라서 설정창 수정은 불필요하다.
- `AppSettingsLoader.Load()`는 파일이 **없을 때만** 기본값으로 파일을 생성한다. 즉, 이미 `AppSettings.json`을 가진 사용자의 `formMessages`는 이번 변경으로 바뀌지 않는다. (결정 필요 사항 1번 참조)

### 목표 2 — KakaoTalk Business 변환 결과 맨 앞에 `고객 성함 / 대화 시작 시간` 추가

현재 구조 (v1.2.4 기준):

- `Convert()`는 `KakaoTalkMessageTimeRegex()` (`^오(전|후)\d{2}:\d{2}`)로 대화를 시간 표시 단위로 분할한다.
- 문단 구조
  - 최초 화자의 문단: 시간 표시가 없고 `match.Index > 0`일 때만 존재. `splited[0]` = 발신자 줄(`...프로필 사진`), `splited[1]` = 고객 이름.
  - 이후 문단: `splited[0]` = 시간, `splited[1]` = 발신자 줄(`프로필 사진`), `splited[2]` = 고객 이름.
- 직원 메시지는 `KakaoTalkStaffMessageRegex()` (`님이 보냄 보낸 메시지 가이드`)로 구분된다.
- 변환된 문단들은 `string.Join(" / ", convertedTexts)`로 결합된다.

필요한 추가 정보:

| 항목 | 출처 | 비고 |
|------|------|------|
| 고객 성함 | 최초의 고객 메시지 문단에서 `프로필 사진` 포함 줄 **바로 다음 줄** | 직원 문단은 제외 |
| 대화 시간 | `matches[0]` (가장 먼저 등장한 시간 표시 텍스트) | 정렬 불필요. 이미 KST 로컬 시간이므로 시간대 변환 없음 |
| 시간 형식 | 24시간제 `HH:mm` | `오전12:30` → `00:30`, `오후12:30` → `12:30`, `오후01:05` → `13:05` |

### 출력 형식 (research.md 추가 요구사항 반영)

```
{HH:mm} 채널({고객 이름}) - {기존의 변환 텍스트}
```

예) `14:30 채널(홍길순) - [ 쌍꺼풀 / 010-1234-5678 ] / ...`

- `[ ... ]`는 기존 변환 텍스트가 이미 포함하는 대괄호이므로 이중으로 감싸지 않는다.
- 이름이 없으면 `채널`, 시간이 없으면 시간 생략.

---

## Story 1 — `formMessages` 기본값 변경

### [완료] 1-1. 기본값 정규표현식 설계

`regex:` 접두어를 붙이고, `-` 앞뒤 공백은 `\s*-\s*`로 허용한다. 문자열 내부의 띄어쓰기도 `\s*`로 느슨하게 처리한다. `AppSettings.json`에서는 `\`를 `\\`로 이스케이프해야 한다.

| 기존 값 | 변경 후 (JSON 표기) |
|---------|--------------------|
| `상담받을 분의 성함 / 연락처 - ` | `regex:상담\\s*받을\\s*분의\\s*성함\\s*-\\s*` |
| (신규 분리) | `regex:연락처\\s*-\\s*` |
| `생년월일 - ` | `regex:생년\\s*월일\\s*-\\s*` |
| `상담부위 - ` | `regex:상담\\s*부위\\s*-\\s*` |
| `첫수술or 재수술 (재수술일경우 마지막 수술시기 ) - ` | `regex:첫\\s*수술\\s*or\\s*재\\s*수술\\s*\\(\\s*재\\s*수술일?\\s*경우\\s*마지막\\s*수술\\s*시기\\s*\\)\\s*-\\s*` |
| `상담 희망 날짜와 시간대 - ` | `regex:상담\\s*희망\\s*날짜\\s*와\\s*시간대\\s*-\\s*` |
| `상담 원하는 원장님 - ` | `regex:상담\\s*원하는\\s*원장님\\s*-\\s*` |
| `저희 병원 알게되신 경로 - ` | `regex:저희\\s*병원\\s*알게\\s*되신\\s*경로\\s*-\\s*` |
| `소개자 있으실 경우 소개자 성함과 연락처 뒷번호 - ` | `regex:소개자\\s*있으실\\s*경우\\s*,?\\s*소개자\\s*성함\\s*과\\s*연락처\\s*뒷\\s*번호\\s*-\\s*` |

설계 기준:

- 항목 순서는 유지하되 `상담받을 분의 성함`을 `연락처`보다 앞에 둔다. 두 항목은 서로 겹치지 않으므로(`성함 -` / `연락처 -`) 순서 충돌은 없다. 단, 소개자 항목은 문장 안에 `연락처 뒷번호 - `가 있으므로, **`소개자…` 패턴이 `연락처\s*-\s*` 보다 먼저 평가되어야 한다.** (`연락처 뒷번호 -`는 `연락처\s*-`와 매칭되지 않지만, 안전을 위해 순서를 소개자 항목 앞쪽으로 배치하거나 `연락처` 패턴 앞에 `(?<!뒷번호\s*)` 류의 방어는 하지 않고 **테스트로 확인**한다.)
- 한국어 띄어쓰기 오류 허용 지점: 복합명사 경계(`상담 받을`, `첫 수술`, `재 수술`, `알게 되신`, `뒷 번호`), 조사 앞(`날짜 와`, `성함 과`), 쉼표 유무(`경우,`).
- `FormMatcherMatches()`의 정확 일치 제외 비교(`text == PlainPattern`)는 regex 항목에서는 의미가 없으나(패턴 문자열과 입력이 일치할 일이 없음) 동작에 영향이 없으므로 변경하지 않는다.

### [완료] 1-2. `AppSettings.json` 수정

- `Summarizer.App/AppSettings.json`의 `formMessages`를 위 표의 9건으로 교체한다.

### [완료] 1-3. `AppSettings.cs` 기본값 동기화

- `AppSettings.FormMessages`의 C# 기본값도 동일한 9건으로 교체한다. (C# 문자열이므로 `@"..."` verbatim 문자열을 사용해 `\` 이중 이스케이프를 피한다.)
- 두 위치(JSON / C#)의 값이 정확히 일치해야 한다.

### [완료] 1-4. 정규식 검증

가상 입력으로 각 패턴의 매칭/제거 결과를 확인한다. (아래 4-3 참조)

---

## Story 2 — 변환 결과 앞에 고객 성함 / 대화 시작 시간 추가

### [완료] 2-1. 시간 표시 정규식에 캡처 그룹 추가

`MessageConverter.KakaoTalkMessageTimeRegex()`를 수정한다.

```csharp
[GeneratedRegex(@"^오(?<meridiem>전|후)(?<hour>\d{2}):(?<minute>\d{2})(\r?\n)", RegexOptions.Multiline)]
private static partial Regex KakaoTalkMessageTimeRegex();
```

- 기존 매칭 위치/길이는 동일하므로, 문단 분할 로직(`match.Index`)에는 영향이 없다.

### [완료] 2-2. 시간 변환 헬퍼

```csharp
// "오후", 01, 05 -> "13:05" / "오전", 12, 30 -> "00:30"
private static string ConvertToTwentyFourHourText(Match timeMatch)
```

- `hour = int.Parse(hour)`; 오전이면 `hour % 12`, 오후이면 `(hour % 12) + 12`.
- 범위 밖 값(예: 오후 13시)은 파싱 가능한 범위에서만 처리하고, 비정상 값이면 `string.Empty`를 반환하여 시간 필드를 생략한다.
- 반환 형식은 `{HH:mm}` (0 패딩).

### [완료] 2-3. 고객 성함 추출 헬퍼

```csharp
// 문단(줄 배열)에서 "프로필 사진" 포함 줄의 바로 다음 줄을 이름으로 반환. 직원 문단이면 null.
private static string? ExtractCustomerName(string[] splitedLines)
```

- 직원 문단(`KakaoTalkStaffMessageRegex()` 일치)은 건너뛴다.
- 이름 줄이 없으면(`프로필 사진` 줄이 마지막이거나 없음) `null`.
- 이름 줄은 `TrimEntries` 처리된 값을 사용한다.

### [완료] 2-4. `Convert()` 흐름 수정

- 문단 순회 중, **아직 이름을 찾지 못한 상태에서** 고객 문단을 변환할 때 `ExtractCustomerName()`을 호출하여 최초 한 번만 이름을 저장한다. (`ConvertCategorizedText()`에 `ref string? customerName` 또는 `out` 인자를 추가하거나, 문단 순회부에서 별도로 호출한다. 후자가 변경 범위가 작아 권장한다.)
- 시간은 `matches[0]`에서 얻는다. (정렬하지 않음)
- 최종 반환 직전에 머리말을 조립한다.

```csharp
var channelText = string.IsNullOrEmpty(customerName) ? "채널" : $"채널({customerName})";
var headerText = string.IsNullOrEmpty(firstTimeText) ? channelText : $"{firstTimeText} {channelText}";
return $"{headerText} - {string.Join(" / ", convertedTexts)}";
```

- `convertedTexts`가 비어 있는 경우에는 머리말 없이 기존처럼 빈 문자열을 반환한다.
- 모객 메시지(`GangnamUnniMessageConverter`) 및 일반 고객 텍스트 경로(`else` 분기)는 **변경하지 않는다.**

### [완료] 2-5. 호출부 영향 확인

- `BatchFileConverter`, `MainWindowViewModel` 등이 `Convert()`의 결과 형식에 의존하는 부분이 있는지 확인한다. (현재 구조상 단순 문자열 사용으로 추정되나 구현 전에 재확인.)

---

## Story 3 — 버전 업데이트 및 빌드 검증

### [완료] 3-1. 버전 v1.2.5로 업데이트

- `App.xaml.cs`: `Version = "1.2.5"`
- `MainWindow.xaml`: `Title="Summarizer (v1.2.5)"`
- `Summarizer.App.csproj`: `1.2.4` → `1.2.5`
- `Summarizer.Core.csproj`: `1.2.4` → `1.2.5`

### [완료] 3-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [완료] 3-3. 동작 검증 (가상 시나리오)

모든 시나리오는 `[가상]` 인물·내용만 사용한다. 실제 대화가 담긴 `예제.txt`, `example.txt`(미추적 파일)는 사용하지 않고 커밋하지 않는다.

**(a) formMessages — 공백 변형 허용**

| 입력 | 기대 결과 (레이블 제거 후) |
|------|--------------------------|
| `상담받을 분의 성함 - 홍길순` | `홍길순` |
| `상담 받을 분의 성함- 홍길순` | `홍길순` |
| `연락처 -010-1234-5678` | `010-1234-5678` |
| `첫수술or 재수술 (재수술일경우 마지막 수술시기 ) - 첫수술` | `첫수술` |
| `첫 수술 or 재수술 (재수술일 경우 마지막 수술 시기 ) - 첫수술` | `첫수술` |
| `소개자 있으실 경우, 소개자 성함과 연락처 뒷번호 - 없음` | `없음` |
| `소개자 있으실 경우 소개자 성함과 연락처 뒷번호 - 없음` | `없음` |

**(b) 성함 / 시간 머리말**

[가상] 입력 (최초 문단에 시간 표시 없음 + 이후 문단):
```
[가상]고객 프로필 사진
홍길순
상담부위 - 눈
오전11:05
[가상]고객 프로필 사진
홍길순
...
```

기대: 출력이 `홍길순 / 11:05 / [ ... ] ...` 로 시작한다.

추가 케이스:
- `오후12:10` → `12:10`, `오전12:10` → `00:10`, `오후01:05` → `13:05`
- 최초 문단이 직원 메시지인 경우 → 이름은 이후 첫 고객 문단에서 취득
- `프로필 사진` 줄이 없는 경우 → 이름 생략, 시간만 표시
- 시간 표시 일치가 없는 일반 텍스트 / 강남언니 메시지 → 출력 변화 없음 (회귀 확인)

---

## 파일 변경 목록

### 수정
- `Summarizer.Core/MessageConverter.cs` — 시간 정규식 캡처 그룹, 헬퍼 2종, `Convert()` 머리말 조립
- `Summarizer.Core/AppSettings.cs` — `FormMessages` 기본값
- `Summarizer.App/AppSettings.json` — `formMessages` 기본값
- `Summarizer.App/App.xaml.cs` — Version
- `Summarizer.App/MainWindow.xaml` — Title
- `Summarizer.App/Summarizer.App.csproj` — 버전
- `Summarizer.Core/Summarizer.Core.csproj` — 버전

### 신규 생성
- 없음 (`task.md`는 `plan.md` 승인 후 작성)

---

## 결정 필요 사항

1. **기존 사용자 `AppSettings.json`의 처리**
   → `AppSettingsLoader`는 파일이 없을 때만 기본값을 쓰므로, 이미 설정 파일이 있는 PC에서는 새 기본값이 적용되지 않는다.
   → 기본 방향: **이번 버전에서는 마이그레이션을 구현하지 않는다.** 배포 대상 PC에서는 설정 파일을 삭제하거나 설정창에서 직접 수정하도록 안내한다. 자동 마이그레이션이 필요하면 별도로 지시해 달라.

2. **머리말 출력 형식** (research.md 추가 요구사항으로 확정)
   → `{HH:mm} 채널({고객 이름}) - {기존의 변환 텍스트}`

3. **성함 또는 시간을 얻지 못한 경우**
   → 이름이 없으면 `채널`, 시간이 없으면 시간 생략. 변환 결과가 없으면 머리말 없이 빈 문자열.

4. **`formMessages` 정규식의 허용 범위**
   → 위 표의 패턴은 research.md에 예시된 3건(`첫수술 or 재수술…`, `상담 받을…`, `소개자 있으실 경우,…`)과 그 유사한 띄어쓰기 오류까지 허용한다. 그 외 항목의 허용 범위(예: `생년 월일`)는 판단하여 추가했으므로, 과하다고 판단되면 알려 달라.
