# Summarizer v1.2.6 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.
> 각 과업이 완료되면 해당 과업에 대한 커밋을 수행한다.

---

## Story 1 — `GangnamUnniFieldLabels` 클래스 추가 및 `AppSettings` 연동

### [완료] 1-1. `GangnamUnniFieldLabels.cs` 신규 생성

- `Summarizer.Core/GangnamUnniFieldLabels.cs` 생성
- 프로퍼티: `CustomerName`, `PhoneNumber`, `ReservationItem`, `PreferredDateTime`, `ChangePreferredDateTime` (모두 `string[]`)
- 기본값에 기존/변경 후 레이블 모두 포함

### [신규] 1-2. `AppSettings.cs` 수정

- `AppSettings` 클래스에 `GangnamUnniFieldLabels GangnamUnniFieldLabels` 프로퍼티 추가

### [신규] 1-3. `AppSettings.json` 기본값 추가

- `gangnamUnniFieldLabels` 항목 추가 (`AppSettings.cs` 기본값과 동일)

---

## Story 2 — `GangnamUnniMessageConverter` 수정

### [신규] 2-1. 생성자에 `GangnamUnniFieldLabels` 주입

- `private readonly GangnamUnniFieldLabels fieldLabels` 필드 추가
- 생성자를 `public GangnamUnniMessageConverter(GangnamUnniFieldLabels fieldLabels)`로 변경

### [신규] 2-2. `ExtractFieldValue()` 및 `ExtractDateLines()` 다중 레이블 지원

- `string fieldLabel` → `string[] fieldLabels` 파라미터로 변경
- 각 레이블에 대해 순차 탐색
- 호출부를 `fieldLabels.CustomerName`, `fieldLabels.PhoneNumber` 등으로 변경

### [신규] 2-3. `MessageConverter.cs` 수정

- `new GangnamUnniMessageConverter()` → `new GangnamUnniMessageConverter(settings.GangnamUnniFieldLabels)`

---

## Story 3 — 버전 업데이트 및 빌드 검증

### [신규] 3-1. 버전 v1.2.6으로 업데이트

- `App.xaml.cs`, `MainWindow.xaml`, `Summarizer.App.csproj`, `Summarizer.Core.csproj`

### [신규] 3-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0
- Release 빌드: 경고 0, 오류 0

### [신규] 3-3. 동작 검증

모든 시나리오는 `[가상]` 인물·내용만 사용한다.

- 변경 후 레이블(`[방문자명]`, `[방문자 전화번호]`) 인식 확인
- 기존 레이블(`[고객명]`, `[연락처]`) 회귀 확인
- `희망 일시 중 가능한 시간을 선택해 주세요.` 필터링 확인

---

## 결정 사항 이행 현황

1. 기존 `AppSettings.json` 마이그레이션 → 구현하지 않음. 기존 JSON에 키가 없으면 기본값 사용
2. `희망 일시 중 가능한 시간을 선택해 주세요.` 처리 → 3-3 검증에서 확인 예정
3. `CanConvert()` 첫 줄 감지 텍스트 → 소스 코드 const 유지
