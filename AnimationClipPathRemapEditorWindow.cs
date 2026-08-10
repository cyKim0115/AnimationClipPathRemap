#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AnimationClip 바인딩 경로 일괄 치환 EditorWindow.
/// 메뉴: Tools/Animation/Clip Path Remap
/// </summary>
public class AnimationClipPathRemapEditorWindow : EditorWindow
{
    readonly List<AnimationClip> _targetClips = new List<AnimationClip>();
    readonly HashSet<AnimationClip> _targetClipSet = new HashSet<AnimationClip>();

    string _oldPath = string.Empty;
    string _newPath = string.Empty;
    AnimationClipPathRemapper.MatchMode _matchMode = AnimationClipPathRemapper.MatchMode.PathSegment;

    Vector2 _clipScroll;
    Vector2 _previewScroll;
    List<AnimationClipPathRemapper.BindingChange> _previewChanges =
        new List<AnimationClipPathRemapper.BindingChange>();
    string _status = string.Empty;

    [MenuItem("Tools/Animation/Clip Path Remap")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationClipPathRemapEditorWindow>("Clip Path Remap");
        window.minSize = new Vector2(420f, 480f);
    }

    /// <summary>Agent / 스크립트에서 창을 열 때 사용.</summary>
    public static void Open() => ShowWindow();

    void OnGUI()
    {
        EditorGUILayout.LabelField("AnimationClip Path Remap", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "부모/오브젝트 이름 변경 후 깨진 바인딩 경로를 여러 클립에 일괄 치환합니다.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4f);

        DrawClipSection();
        EditorGUILayout.Space(6f);
        DrawReplaceSection();
        EditorGUILayout.Space(6f);
        DrawActionSection();
        EditorGUILayout.Space(6f);
        DrawPreviewSection();

        if (!string.IsNullOrEmpty(_status))
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }
    }

    void DrawClipSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"대상 클립 ({_targetClips.Count})", EditorStyles.boldLabel);

                if (GUILayout.Button("Selection", GUILayout.Width(70f)))
                    AddFromSelection();

                using (new EditorGUI.DisabledScope(_targetClips.Count == 0))
                {
                    if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                        ClearTargets();
                }
            }

            var dropRect = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "AnimationClip / Folder / GameObject(Animator) 드래그 앤 드롭", EditorStyles.helpBox);
            HandleTargetDragAndDrop(dropRect);

            _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll, GUILayout.MinHeight(100f));
            for (var i = 0; i < _targetClips.Count; i++)
                DrawClipRow(_targetClips[i]);
            EditorGUILayout.EndScrollView();
        }
    }

    void DrawClipRow(AnimationClip clip)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
            if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                RemoveTarget(clip);
        }
    }

    void DrawReplaceSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("치환", EditorStyles.boldLabel);
            _matchMode = (AnimationClipPathRemapper.MatchMode)EditorGUILayout.EnumPopup("Match Mode", _matchMode);
            _oldPath = EditorGUILayout.TextField("Old Path / Name", _oldPath);
            _newPath = EditorGUILayout.TextField("New Path / Name", _newPath);

            switch (_matchMode)
            {
                case AnimationClipPathRemapper.MatchMode.PathSegment:
                    EditorGUILayout.HelpBox(
                        "PathSegment: 경로 세그먼트(슬래시 구분 이름)가 Old와 같을 때만 New로 바꿉니다. 부모 이름 변경에 권장.",
                        MessageType.Info);
                    break;
                case AnimationClipPathRemapper.MatchMode.PathPrefix:
                    EditorGUILayout.HelpBox(
                        "PathPrefix: 경로가 Old와 같거나 Old/ 로 시작하면 접두를 New로 바꿉니다.",
                        MessageType.Info);
                    break;
                case AnimationClipPathRemapper.MatchMode.ExactPath:
                    EditorGUILayout.HelpBox(
                        "ExactPath: 바인딩 path가 Old와 완전히 같을 때만 치환합니다.",
                        MessageType.Info);
                    break;
                case AnimationClipPathRemapper.MatchMode.Substring:
                    EditorGUILayout.HelpBox(
                        "Substring: 단순 문자열 치환입니다. 부분 이름 충돌에 주의하세요.",
                        MessageType.Warning);
                    break;
            }
        }
    }

    void DrawActionSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUI.DisabledScope(_targetClips.Count == 0 || string.IsNullOrEmpty(_oldPath)))
            {
                if (GUILayout.Button("Preview", GUILayout.Height(26f)))
                    RunPreview();

                using (new EditorGUI.DisabledScope(_previewChanges.Count == 0))
                {
                    var applyLabel = _previewChanges.Count > 0
                        ? $"Apply ({_previewChanges.Count} bindings)"
                        : "Apply";
                    if (GUILayout.Button(applyLabel, GUILayout.Height(28f)))
                        RunApply();
                }
            }
        }
    }

    void DrawPreviewSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"미리보기 ({_previewChanges.Count})", EditorStyles.boldLabel);
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.MinHeight(140f));

            if (_previewChanges.Count == 0)
            {
                EditorGUILayout.LabelField("변경 없음 — Preview를 실행하세요.", EditorStyles.miniLabel);
            }
            else
            {
                for (var i = 0; i < _previewChanges.Count; i++)
                {
                    var change = _previewChanges[i];
                    var kind = change.IsObjectReference ? "obj" : "float";
                    EditorGUILayout.LabelField(
                        $"{change.Clip.name}  [{kind}]  {change.OldPath}  →  {change.NewPath}",
                        EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"    .{change.PropertyName}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    void RunPreview()
    {
        var result = AnimationClipPathRemapper.Preview(_targetClips, _oldPath, _newPath, _matchMode);
        _previewChanges = new List<AnimationClipPathRemapper.BindingChange>(result.Changes);
        _status = $"Preview: clips={result.ClipCount}, bindings={result.BindingChangeCount}";
        Repaint();
    }

    void RunApply()
    {
        if (!EditorUtility.DisplayDialog(
                "Clip Path Remap",
                $"{_previewChanges.Count}개 바인딩 경로를 치환합니다. 계속할까요?",
                "Apply",
                "Cancel"))
            return;

        var result = AnimationClipPathRemapper.Remap(
            _targetClips,
            _oldPath,
            _newPath,
            _matchMode,
            dryRun: false);
        _previewChanges = new List<AnimationClipPathRemapper.BindingChange>(result.Changes);
        _status =
            $"Applied: dirtyClips={result.ClipCount}, bindings={result.BindingChangeCount}. Undo로 되돌릴 수 있습니다.";
        Debug.Log($"[AnimationClipPathRemapEditorWindow] {_status}");
        Repaint();
    }

    void HandleTargetDragAndDrop(Rect dropRect)
    {
        var evt = Event.current;
        if (!dropRect.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = HasSupportedDraggedObject()
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddDraggedObjects(DragAndDrop.objectReferences);
            evt.Use();
        }
    }

    static bool HasSupportedDraggedObject()
    {
        foreach (var dragged in DragAndDrop.objectReferences)
        {
            if (dragged is AnimationClip)
                return true;
            if (dragged is DefaultAsset)
                return true;
            if (dragged is GameObject)
                return true;
        }

        return false;
    }

    void AddDraggedObjects(Object[] draggedObjects)
    {
        foreach (var dragged in draggedObjects)
        {
            if (dragged is AnimationClip clip)
            {
                AddTarget(clip);
                continue;
            }

            if (dragged is GameObject go)
            {
                AddClipsFromGameObject(go);
                continue;
            }

            var assetPath = AssetDatabase.GetAssetPath(dragged);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                foreach (var folderClip in AnimationClipPathRemapper.FindClipsUnderFolder(assetPath))
                    AddTarget(folderClip);
            }
        }
    }

    void AddFromSelection()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is AnimationClip clip)
            {
                AddTarget(clip);
                continue;
            }

            if (obj is GameObject go)
            {
                AddClipsFromGameObject(go);
                continue;
            }

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                foreach (var folderClip in AnimationClipPathRemapper.FindClipsUnderFolder(assetPath))
                    AddTarget(folderClip);
            }
        }
    }

    void AddClipsFromGameObject(GameObject go)
    {
        if (!go.TryGetComponent<Animator>(out var animator) || animator.runtimeAnimatorController == null)
            return;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            AddTarget(clip);
    }

    void AddTarget(AnimationClip clip)
    {
        if (clip == null || !_targetClipSet.Add(clip))
            return;
        _targetClips.Add(clip);
        _previewChanges.Clear();
    }

    void RemoveTarget(AnimationClip clip)
    {
        _targetClips.Remove(clip);
        _targetClipSet.Remove(clip);
        _previewChanges.Clear();
    }

    void ClearTargets()
    {
        _targetClips.Clear();
        _targetClipSet.Clear();
        _previewChanges.Clear();
        _status = string.Empty;
    }
}
#endif
