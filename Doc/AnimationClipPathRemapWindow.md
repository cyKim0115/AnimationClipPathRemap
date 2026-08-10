# AnimationClip Path Remap 사용 가이드

## 개요

부모 Transform 이름을 바꿨을 때 AnimationClip 바인딩 경로가 깨지는 문제를, **여러 클립에 한 번에** 치환하는 에디터 도구입니다.

---

## 접근 방법

- 메뉴: **Tools → Animation → Clip Path Remap**
- Agent: `AnimationClipPathRemapEditorWindow.Open();`
- 핵심 API: `AnimationClipPathRemapper.Preview(...)` / `Remap(...)`

---

## UI 미리보기

(스크린샷은 추후 `AnimationClipPathRemap 사진.png`로 같은 폴더에 추가)

---

## UI 구성

| 영역 | 설명 |
|---|---|
| **대상 클립** | 드래그 앤 드롭 / Selection / Clear |
| **Match Mode** | PathSegment(권장) / PathPrefix / ExactPath / Substring |
| **Old Path / Name** | 바꿀 경로 또는 세그먼트 이름 |
| **New Path / Name** | 새 경로 또는 세그먼트 이름 |
| **Preview** | 변경될 바인딩 목록만 표시 |
| **Apply** | Undo 가능 치환 + SaveAssets |

드롭 가능: `AnimationClip`, 폴더(하위의 모든 `.anim`), Animator가 붙은 GameObject(컨트롤러 클립).

---

## 주요 기능

1. **PathSegment** — `A/OldParent/B` 에서 세그먼트 `OldParent`만 `NewParent`로 교체
2. **PathPrefix** — `Root/Old/...` 접두를 `Root/New/...`로 교체
3. **ExactPath** — path 전체가 일치할 때만 교체
4. **Substring** — 단순 문자열 치환 (부분 이름 충돌 주의)
5. float 커브 + object reference 커브 모두 처리

---

## 사용 시나리오

1. 프리팹에서 부모 오브젝트 이름을 `Body` → `Torso`로 변경
2. 관련 `.anim` 폴더를 창에 드롭
3. Match Mode = **PathSegment**, Old=`Body`, New=`Torso`
4. **Preview**로 목록 확인 후 **Apply**

---

## 내부 동작 원리

1. `AnimationUtility.GetCurveBindings` / `GetObjectReferenceCurveBindings`로 바인딩 수집
2. `TryRemapPath`로 새 path 계산
3. Apply 시 기존 바인딩 커브를 읽고 제거한 뒤, path만 바꾼 바인딩으로 다시 설정
4. `Undo.RecordObject` + `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets`

---

## 데이터 구조

- `AnimationClipPathRemapper.MatchMode`
- `AnimationClipPathRemapper.BindingChange`
- `AnimationClipPathRemapper.RemapResult`

런타임 저장 데이터 없음. 클립 에셋만 수정.

---

## 주의사항

- **Substring**은 `Hand` → `Arm`처럼 다른 이름 일부를 건드릴 수 있음. 가능하면 PathSegment 사용
- PathSegment의 Old는 `/` 없는 **단일 이름**이어야 함
- Model 임포트 클립(FBX 내부)은 읽기 전용일 수 있음 — 프로젝트 `.anim`에 적용하는 것을 권장
- Apply 전 반드시 Preview

---

## 관련 파일

| 파일 | 역할 |
|---|---|
| `AnimationClipPathRemapper.cs` | 핵심 API |
| `AnimationClipPathRemapEditorWindow.cs` | EditorWindow |
| `README.md` | 설치·서브모듈 안내 |

MyUtil에서는 `Assets/CyKimExtension/Editor/Animation` 서브모듈로 포함합니다.

---

## 버전 히스토리

| 날짜 | 내용 |
|---|---|
| 2026-08-10 | MVP: Preview/Apply, 4 MatchMode, 폴더·Animator 드롭 |
| 2026-08-10 | 독립 저장소 + MyUtil 서브모듈 분리 |
