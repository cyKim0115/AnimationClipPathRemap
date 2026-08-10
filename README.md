# AnimationClip Path Remap

Unity Editor 도구: 하이어라키/부모 Transform 이름 변경 후 깨진 **AnimationClip 바인딩 경로**를 여러 클립에 일괄 치환합니다.

## 설치 (서브모듈)

대상 Unity 프로젝트에서:

```bash
git submodule add https://github.com/cyKim0115/AnimationClipPathRemap.git Assets/CyKimExtension/Editor/Animation
git submodule update --init --recursive
```

MyUtil에서는 위 경로로 이미 연결되어 있습니다.

`.meta`는 Unity Editor가 생성합니다. 첫 import 후 GUID 고정을 위해 서브모듈에 `.meta`를 커밋하는 것을 권장합니다.

## 사용

- 메뉴: **Tools → Animation → Clip Path Remap**
- Agent: `AnimationClipPathRemapEditorWindow.Open();`
- API: `AnimationClipPathRemapper.Preview(...)` / `Remap(...)`

상세: [Doc/AnimationClipPathRemapWindow.md](Doc/AnimationClipPathRemapWindow.md)

## Match Mode

| Mode | 용도 |
|---|---|
| `PathSegment` | 단일 Transform 이름 교체 (권장) |
| `PathPrefix` | 경로 접두 교체 |
| `ExactPath` | path 완전 일치만 |
| `Substring` | 문자열 치환 (충돌 주의) |

## 의존성

`UnityEditor` / `UnityEngine`만 사용합니다.
