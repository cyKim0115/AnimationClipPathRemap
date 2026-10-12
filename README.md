# AnimationClip Path Remap

하이어라키를 정리하거나 Transform 이름을 바꾼 뒤, **AnimationClip에 남아 있는 이전 바인딩 경로를 여러 클립에서 한 번에 치환**하는 Unity Editor 도구입니다.

예를 들어 `Body`를 `Torso`로 바꿨는데 애니메이션이 여전히 `Character/Body/Hand`를 가리킨다면, 키프레임을 다시 만들지 않고 경로를 `Character/Torso/Hand`로 바꿀 수 있습니다.

- **Preview**로 변경할 클립·경로·프로퍼티를 먼저 확인합니다.
- **Apply**로 원본 클립을 수정하고 저장합니다. Unity **Undo**를 지원합니다.
- Transform 등 숫자 커브(`float`)와 Sprite 등 오브젝트 참조 커브(`obj`)를 모두 처리합니다.
- 클립, 폴더, Animator가 붙은 GameObject로 대상을 모을 수 있습니다.

> 바꾸는 것은 클립의 **바인딩 path**입니다. GameObject 이름이나 하이어라키를 직접 변경하거나, Animator Controller를 다시 구성하지는 않습니다.

[설치](#설치) · [빠른 시작](#빠른-시작) · [UI 구성](#ui-구성) · [Match Mode](#match-mode-선택) · [문제 해결](#문제-해결) · [상세 가이드 / API](Doc/AnimationClipPathRemapWindow.md)

---

## UI 미리보기

**Tools → Animation → Clip Path Remap**으로 엽니다.

![Clip Path Remap 창: 대상 클립, 치환 조건, Preview와 Apply, 미리보기 영역](Doc/Images/clip-path-remap-overview.png)

아래는 두 샘플 클립에서 `Body` → `Torso` 치환을 **Preview**한 실제 화면입니다. 총 4개 바인딩이 표시되며, 이 단계에서는 클립을 수정하지 않습니다.

![Body를 Torso로 치환하는 Preview: 샘플 클립 2개와 변경 예정 바인딩 4개](Doc/Images/clip-path-remap-preview.png)

*Unity 6000.6.0f1, 문서용 메모리 샘플 클립으로 캡처했습니다. Unity 테마와 창 크기에 따라 모양이 조금 다를 수 있습니다.*

---

## 설치

`UnityEditor` / `UnityEngine`만 사용합니다. **현재 저장소는 `package.json`이 없는 Editor 스크립트 배포 형태**이므로, Unity Package Manager의 Git URL 설치 대신 아래 방법을 사용합니다.

### 방법 1. 스크립트 복사

1. 이 저장소를 다운로드하거나 clone합니다.
2. 다음 두 파일을 대상 프로젝트의 `Assets/Editor/AnimationClipPathRemap/`에 복사합니다.

   ```text
   Assets/
   └─ Editor/
      └─ AnimationClipPathRemap/
         ├─ AnimationClipPathRemapEditorWindow.cs
         └─ AnimationClipPathRemapper.cs
   ```

3. Unity의 import와 컴파일이 끝나면 **Tools → Animation → Clip Path Remap** 메뉴를 확인합니다.

이미 같은 클래스의 스크립트가 들어 있다면 중복 설치하지 않습니다. 중복 클래스 컴파일 오류가 생길 수 있습니다.

### 방법 2. Git 서브모듈

대상 Unity 프로젝트 루트에서 실행합니다. 설치할 폴더는 비어 있어야 합니다.

```bash
git submodule add https://github.com/cyKim0115/AnimationClipPathRemap.git Assets/CyKimExtension/Editor/Animation
git submodule update --init --recursive
```

서브모듈이 포함된 프로젝트를 다른 환경에서 받을 때:

```bash
git clone --recurse-submodules <프로젝트-Git-URL>
# 이미 clone한 프로젝트라면:
git submodule update --init --recursive
```

`.meta`는 Unity가 생성하게 둡니다. 생성된 `.meta`의 버전 관리 여부는 대상 프로젝트·서브모듈의 정책을 따르며, 직접 작성하지 않습니다.

**확인 환경:** Unity 6000.6.0f1. 최소 지원 Unity 버전은 별도로 검증하지 않았습니다.

---

## 빠른 시작

예시: Animator 아래의 `Body` 이름을 `Torso`로 변경한 경우입니다.

```text
AnimatorRoot                 ← Animator가 있는 오브젝트
└─ Character
   └─ Body → Torso           ← 하이어라키에서 이미 변경한 이름
      └─ Hand

클립의 이전 path: Character/Body/Hand
치환할 새 path:   Character/Torso/Hand
```

1. 수정할 `.anim` 파일을 버전 관리하거나 백업합니다.
2. **Tools → Animation → Clip Path Remap**을 엽니다.
3. 관련 클립이나 클립이 모인 폴더를 상단 드롭 영역에 놓습니다. Project에서 선택한 뒤 **Selection**을 눌러도 됩니다.
4. **Match Mode**를 **Path Segment**로 둡니다.
5. **Old Path / Name**에 `Body`, **New Path / Name**에 `Torso`를 입력합니다.
6. **Preview**를 누릅니다. 변경 경로와 프로퍼티가 의도한 대상인지 확인합니다.
7. **Apply (N bindings)** 버튼을 누른 뒤 확인 창에서 **Apply**를 선택합니다. **Cancel**을 누르면 적용하지 않습니다.
8. Animation 창이나 실제 재생으로 결과를 확인합니다. 잘못 적용했다면 **Edit → Undo**로 되돌리고 클립을 다시 저장합니다.

> **조건을 바꿀 때마다 Preview를 다시 누르세요.** Match Mode / Old / New를 편집해도 기존 미리보기는 자동으로 갱신되지 않습니다. Apply는 화면의 이전 미리보기 대신 **현재 입력값**으로 변경을 다시 계산합니다.

---

## UI 구성

| 컨트롤 | 사용 방법 |
|---|---|
| **대상 클립 (N)** | 등록된 서로 다른 클립 수입니다. 같은 클립을 여러 번 추가해도 한 번만 등록됩니다. |
| **드래그 앤 드롭 영역** | AnimationClip, 폴더, Animator가 붙은 GameObject를 놓습니다. |
| **Selection** | 현재 Unity 선택 항목에서 지원 대상을 찾아 기존 목록에 추가합니다. |
| **Remove** | 해당 클립만 목록에서 제외합니다. 클립 에셋을 삭제하지 않습니다. |
| **Clear** | 대상 목록과 미리보기를 비웁니다. Match Mode / Old / New 입력은 유지합니다. |
| **Match Mode** | 어느 범위까지 경로가 일치해야 치환할지 선택합니다. |
| **Old Path / Name** | 찾을 이전 이름 또는 경로입니다. 비어 있으면 Preview와 Apply가 비활성입니다. |
| **New Path / Name** | 바꿀 이름 또는 경로입니다. 빈 값도 허용하므로 의도하지 않은 삭제에 주의합니다. |
| **Preview** | 클립을 수정하지 않고 변경 예정 바인딩을 계산합니다. |
| **Apply (N bindings)** | 미리보기에 변경이 있을 때 활성화됩니다. 확인 후 원본 클립을 수정하고 `SaveAssets`를 호출합니다. |
| **미리보기 (N)** | 클립 이름, `[float]` / `[obj]`, 이전 → 새 경로, 아래 줄의 프로퍼티 이름을 보여줍니다. |
| **하단 상태 메시지** | Preview의 대상 클립 수·변경 바인딩 수 또는 Apply의 수정 클립 수·바인딩 수를 표시합니다. |

### 대상 추가 방식

| 입력 | 모으는 대상 / 주의점 |
|---|---|
| **AnimationClip** | 지정한 클립을 추가합니다. 여러 클립을 함께 선택할 수 있습니다. |
| **폴더** | 하위 폴더까지 `t:AnimationClip`으로 검색합니다. 편집 가능한 `.anim`을 모은 폴더 사용을 권장합니다. |
| **GameObject / 프리팹** | **그 오브젝트 자체**의 Animator에 연결된 `runtimeAnimatorController.animationClips`를 추가합니다. 자식 Animator는 자동 검색하지 않습니다. |
| **Animator Controller 에셋** | 직접 드롭하는 방식은 지원하지 않습니다. 이를 사용하는 Animator의 GameObject나 클립을 추가합니다. |

폴더 검색은 에셋 경로당 `LoadAssetAtPath<AnimationClip>`로 하나를 로드합니다. **FBX 안의 여러 하위 클립을 전부 수집하는 기능은 아닙니다.**

### 미리보기 읽는 법

```text
Demo_Walk  [float]  Character/Body/Hand → Character/Torso/Hand
    .m_LocalPosition.x
```

이는 `Demo_Walk` 클립의 X 위치 커브가 가리키는 경로 하나가 바뀐다는 뜻입니다. 같은 Hand의 X·Y·Z 커브는 각각 별도 바인딩으로 셉니다. **바인딩 수는 오브젝트 수나 키프레임 수와 다릅니다.**

---

## Match Mode 선택

UI에는 `Path Segment`처럼 띄어쓰기가 보이고, API에서는 `MatchMode.PathSegment`처럼 사용합니다. 모든 모드는 **대소문자를 구분**합니다.

| UI / API | 언제 사용하나요? | Old → New | 기존 path → 결과 |
|---|---|---|---|
| **Path Segment** / `PathSegment` | Transform 이름 하나를 바꿀 때 권장합니다. `/`로 나눈 이름이 정확히 같은 부분만 바꿉니다. | `Body` → `Torso` | `Character/Body/Hand` → `Character/Torso/Hand` |
| **Path Prefix** / `PathPrefix` | 특정 부모 아래의 전체 경로를 옮길 때 사용합니다. Old 자체와 `Old/…`에 일치합니다. | `Character/Body` → `Rig/Torso` | `Character/Body/Hand` → `Rig/Torso/Hand` |
| **Exact Path** / `ExactPath` | 경로가 완전히 같은 바인딩만 바꿉니다. 자식 경로에는 적용하지 않습니다. | `Character/Body/Hand` → `Hand` | `Character/Body/Hand` → `Hand` |
| **Substring** / `Substring` | 경계 구분 없이 문자열의 모든 일치 부분을 바꿉니다. | `Body` → `Torso` | `Bodyguard/Hand` → `Torsoguard/Hand` |

- **PathSegment:** Old에 `/`를 넣으면 경고가 나고 변경하지 않습니다. `Body/Body/Hand`의 두 `Body`는 모두 바뀝니다. 이름 교체에는 New도 단일 이름을 사용합니다.
- **PathPrefix:** `Character/Body`는 `Character/Bodyguard`에 일치하지 않습니다. 접두를 제거하려면 New를 비웁니다. `Character/Body/Hand` → `Hand`가 됩니다.
- **ExactPath:** Old와 같은 path를 가진 여러 프로퍼티는 함께 바뀝니다. 프로퍼티 하나만 고르는 UI는 없습니다.
- **Substring:** 이름 일부까지 바꾸므로 Preview에서 예상하지 못한 항목이 없는지 확인합니다.

---

## 안전하게 사용하기

- **경로는 애니메이션을 재생하는 루트 기준의 상대 경로**입니다. 씬 전체 경로나 `Assets/…` 에셋 경로를 넣지 않습니다. 실제 클립의 binding path를 기준으로 입력합니다.
- **새 경로의 존재 여부를 검사하지 않습니다.** 치환 결과가 실제 하이어라키와 맞는지 직접 확인해야 합니다.
- **같은 path·컴포넌트 타입·프로퍼티로 합쳐지는 바인딩을 검사하지 않습니다.** 기존 커브를 덮어쓸 수 있으므로 이런 충돌은 적용 전에 피합니다.
- **New가 빈 값일 때는 특히 주의합니다.** 접두 제거는 PathPrefix를 사용합니다. PathSegment에서 중간 이름만 빈 값으로 바꾸면 `Character//Hand`처럼 빈 세그먼트가 남을 수 있습니다.
- **FBX/모델 내부 클립이나 읽기 전용 에셋은 피합니다.** 이 도구에는 편집 가능 여부 검사나 `.anim` 추출 기능이 없습니다. 먼저 편집 가능한 별도 `.anim`을 준비합니다.
- **여러 Animator가 같은 클립을 공유한다면 모두 영향을 받습니다.** 일부 대상만 바꾸려면 클립을 복제하고 해당 대상의 연결도 별도로 바꿉니다.
- **원본을 바로 수정합니다.** 자동 백업·복제 기능은 없습니다. Unity Undo 외에 Git 등 버전 관리도 사용합니다.
- **Apply 직후 미리보기는 방금 적용한 변경 목록으로 남습니다.** 새 상태를 확인하려면 Preview를 다시 누릅니다.

---

## 문제 해결

| 증상 | 확인할 내용 |
|---|---|
| 메뉴가 보이지 않습니다. | 두 스크립트가 Editor 폴더 아래에 있는지, Console에 컴파일 오류나 중복 클래스 오류가 있는지 확인합니다. |
| GameObject를 추가해도 클립이 없습니다. | 선택한 오브젝트 자체에 Animator가 있고 Controller가 연결되어 있는지 확인합니다. 자식에 있다면 그 자식을 추가합니다. |
| Preview / Apply가 비활성입니다. | 대상 클립과 Old 입력이 필요합니다. Apply에는 변경이 있는 Preview 결과도 필요합니다. |
| Preview 결과가 0개입니다. | Old의 철자·대소문자·루트 기준 경로·Match Mode를 확인합니다. Old와 New가 같아도 변경은 없습니다. |
| 입력을 바꿨는데 목록이 그대로입니다. | 조건 편집 시 미리보기는 자동 갱신되지 않습니다. Preview를 다시 실행합니다. |
| 적용했는데 애니메이션이 여전히 동작하지 않습니다. | 새 path가 실제 재생 루트 아래에 존재하는지, 컴포넌트와 프로퍼티가 유효한지 확인합니다. 경로 외의 리깅·Avatar 문제를 고치는 도구는 아닙니다. |
| 적용 결과를 되돌리고 싶습니다. | **Edit → Undo** 또는 Windows `Ctrl+Z` / macOS `Cmd+Z`를 사용하고 클립을 다시 저장합니다. Editor를 재시작했다면 버전 관리·백업에서 복원합니다. |

---

## 스크립트 / Agent에서 사용

창 열기:

```csharp
AnimationClipPathRemapEditorWindow.Open();
```

Editor 코드에서 변경 없이 확인하기:

```csharp
var clips = AnimationClipPathRemapper.FindClipsUnderFolder("Assets/Animations/Character");
var preview = AnimationClipPathRemapper.Preview(
    clips, "Body", "Torso", AnimationClipPathRemapper.MatchMode.PathSegment);

Debug.Log($"대상 클립: {preview.ClipCount}, 변경 바인딩: {preview.BindingChangeCount}");
```

적용 API, `dryRun`, 결과 데이터의 의미는 [상세 가이드의 API 절](Doc/AnimationClipPathRemapWindow.md#editor-api)을 참고하세요. **API의 `Remap`은 UI 확인 창 없이 즉시 적용**하므로 Preview 결과 확인 후 호출합니다.

---

## 관련 파일

| 파일 | 역할 |
|---|---|
| [AnimationClipPathRemapEditorWindow.cs](AnimationClipPathRemapEditorWindow.cs) | 대상 수집, 치환 UI, Preview / Apply 확인 창 |
| [AnimationClipPathRemapper.cs](AnimationClipPathRemapper.cs) | 경로 매칭, 커브 수집·치환, Undo / 저장 |
| [Doc/AnimationClipPathRemapWindow.md](Doc/AnimationClipPathRemapWindow.md) | 추가 사용 시나리오, API, 내부 동작과 데이터 구조 |
