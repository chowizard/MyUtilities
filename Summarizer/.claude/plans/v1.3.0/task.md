# Summarizer v1.3.0 — 작업 결과 (Task)

> plan.md 승인 완료 기준으로 작성된 세부 과업 목록이다.
> 각 과업이 완료되면 해당 과업에 대한 커밋을 수행한다.

---

## Story 1 — `GangnamUnniFieldLabels` 클래스 추가 및 `AppSettings` 연동

### [완료] 1-1. `GangnamUnniFieldLabels.cs` 신규 생성

- `Summarizer.Core/GangnamUnniFieldLabels.cs` 생성
- 프로퍼티: `CustomerName`, `PhoneNumber`, `ReservationItem`, `PreferredDateTime`, `ChangePreferredDateTime` (모두 `string[]`)
- 기본값에 기존/변경 후 레이블 모두 포함

### [완료] 1-2. `AppSettings.cs` 수정

- `AppSettings` 클래스에 `GangnamUnniFieldLabels GangnamUnniFieldLabels` 프로퍼티 추가

### [완료] 1-3. `AppSettings.json` 기본값 추가

- `gangnamUnniFieldLabels` 항목 추가 (`AppSettings.cs` 기본값과 동일)

---

## Story 2 — `GangnamUnniMessageConverter` 수정

### [완료] 2-1. 생성자에 `GangnamUnniFieldLabels` 주입

- `private readonly GangnamUnniFieldLabels fieldLabels` 필드 추가
- 생성자를 `public GangnamUnniMessageConverter(GangnamUnniFieldLabels fieldLabels)`로 변경

### [완료] 2-2. `ExtractFieldValue()` 및 `ExtractDateLines()` 다중 레이블 지원

- `string fieldLabel` → `string[] fieldLabels` 파라미터로 변경
- 각 레이블에 대해 순차 탐색
- 호출부를 `fieldLabels.CustomerName`, `fieldLabels.PhoneNumber` 등으로 변경

### [완료] 2-3. `MessageConverter.cs` 수정

- `new GangnamUnniMessageConverter()` → `new GangnamUnniMessageConverter(settings.GangnamUnniFieldLabels)`
- **비고**: 2-1~2-3은 원자적 변경이므로 한 커밋으로 묶음

---

## Story 3 — 버전 업데이트 및 빌드 검증

### [완료] 3-1. 버전 v1.3.0으로 업데이트

- `App.xaml.cs`, `MainWindow.xaml`, `Summarizer.App.csproj`, `Summarizer.Core.csproj`

### [완료] 3-2. 빌드 검증

- Debug 빌드: 경고 0, 오류 0 ✓
- Release 빌드 (Core): 경고 0, 오류 0 ✓
- Release 빌드 (App): 실행 중인 앱(PID 50012)이 DLL 점유 → 환경 문제로 코드 오류 없음

### [완료] 3-3. 동작 검증

저장소 외부(임시 콘솔 프로젝트)로 [가상] 시나리오 실행 (저장소에는 포함하지 않음)

- (a) 변경 후 레이블(`[방문자명]`, `[방문자 전화번호]`) 인식 → `강남언니 / 홍길순 / 010-1234-5678 / 김가나 | 쌍꺼풀` ✓
- (a) `희망 일시 중 가능한 시간을 선택해 주세요.` 필터링 → 결과에 없음 ✓
- (b) 기존 레이블(`[고객명]`, `[연락처]`) 회귀 → 동일 출력 ✓
- (c) 예약 취소 — 변경 후 레이블 → `강남언니 / 이순신 / 010-9876-5432 / 예약 취소` ✓
- (d) 예약 변경 — 혼합 레이블(`[방문자명]` + `[연락처]`) → `강남언니 / 강감찬 / 010-1111-2222 / 예약 변경` ✓

---

## Story 4 — 설정 GUI에서 `gangnamUnniFieldLabels` 편집 지원

### [완료] 4-1. 레이블 편집용 뷰모델 추가 및 저장/불러오기 연동

- `FieldLabelItemViewModel`, `FieldLabelGroupViewModel` 신규 생성 (선택/추가/삭제/위로/아래로/모두선택 커맨드는 기존 목록 편집 방식과 동일)
- `AppSettingsDialogViewModel`에 5개 그룹 프로퍼티 추가, `LoadFromSettings()`/`SaveToSettings()` 연동
- 저장 시 빈 텍스트 제외, 앞뒤 공백 제거

### [완료] 4-2. 설정 GUI에 편집 영역 추가

- `AppSettingsDialog.xaml`에 "강남언니 메시지 필드 레이블" 영역 추가 (공용 `DataTemplate`으로 5개 그룹 표시)
- 일반 텍스트 접두어 일치 방식이므로 정규표현식 체크박스는 없음

### [완료] 4-3. 빌드 검증

- Debug / Release 빌드: 경고 0, 오류 0 ✓
- 설정 GUI 실행 확인: 사용자가 실행하여 확인 완료 ✓

---

## 결정 사항 이행 현황

1. 기존 `AppSettings.json` 마이그레이션 → 구현하지 않음. 기존 JSON에 키가 없으면 기본값 사용
2. `희망 일시 중 가능한 시간을 선택해 주세요.` 처리 → 3-3 검증에서 확인 예정
3. `CanConvert()` 첫 줄 감지 텍스트 → 소스 코드 const 유지
