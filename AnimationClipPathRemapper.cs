#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AnimationClip 바인딩 경로(path) 일괄 치환 핵심 API.
/// EditorWindow와 Agent(execute_code)에서 공유한다.
/// </summary>
public static class AnimationClipPathRemapper
{
    public enum MatchMode
    {
        /// <summary>path == old 이거나 path가 old/ 로 시작할 때 접두 치환.</summary>
        PathPrefix = 0,

        /// <summary>path == old 인 바인딩만 치환.</summary>
        ExactPath = 1,

        /// <summary>'/'로 나눈 세그먼트 중 old와 같은 이름만 new로 교체 (부모 이름 변경에 적합).</summary>
        PathSegment = 2,

        /// <summary>문자열 Contains 기준 단순 치환 (부분 이름 충돌에 주의).</summary>
        Substring = 3,
    }

    public readonly struct BindingChange
    {
        public readonly AnimationClip Clip;
        public readonly string OldPath;
        public readonly string NewPath;
        public readonly string PropertyName;
        public readonly bool IsObjectReference;

        public BindingChange(
            AnimationClip clip,
            string oldPath,
            string newPath,
            string propertyName,
            bool isObjectReference)
        {
            Clip = clip;
            OldPath = oldPath;
            NewPath = newPath;
            PropertyName = propertyName;
            IsObjectReference = isObjectReference;
        }
    }

    public readonly struct RemapResult
    {
        public readonly int ClipCount;
        public readonly int BindingChangeCount;
        public readonly IReadOnlyList<BindingChange> Changes;

        public RemapResult(int clipCount, IReadOnlyList<BindingChange> changes)
        {
            ClipCount = clipCount;
            Changes = changes;
            BindingChangeCount = changes.Count;
        }
    }

    public static bool TryRemapPath(string path, string oldPath, string newPath, MatchMode mode, out string remappedPath)
    {
        remappedPath = path;

        if (string.IsNullOrEmpty(oldPath))
            return false;

        path ??= string.Empty;
        newPath ??= string.Empty;

        switch (mode)
        {
            case MatchMode.ExactPath:
                if (path != oldPath)
                    return false;
                remappedPath = newPath;
                return remappedPath != path;

            case MatchMode.PathPrefix:
            {
                var normalizedOld = oldPath.TrimEnd('/');
                if (path == normalizedOld || path == oldPath)
                {
                    remappedPath = newPath ?? string.Empty;
                    return remappedPath != path;
                }

                var boundaryPrefix = normalizedOld + "/";
                if (!path.StartsWith(boundaryPrefix, StringComparison.Ordinal))
                    return false;

                var suffix = path.Substring(normalizedOld.Length); // begins with '/'
                if (string.IsNullOrEmpty(newPath))
                    remappedPath = suffix.Length > 1 ? suffix.Substring(1) : string.Empty;
                else
                    remappedPath = newPath.TrimEnd('/') + suffix;

                return remappedPath != path;
            }

            case MatchMode.PathSegment:
            {
                if (oldPath.IndexOf('/') >= 0)
                {
                    Debug.LogWarning(
                        "[AnimationClipPathRemapper] PathSegment mode expects a single transform name without '/'.");
                    return false;
                }

                var segments = path.Split('/');
                var changed = false;
                for (var i = 0; i < segments.Length; i++)
                {
                    if (segments[i] != oldPath)
                        continue;
                    segments[i] = newPath;
                    changed = true;
                }

                if (!changed)
                    return false;

                remappedPath = string.Join("/", segments);
                return remappedPath != path;
            }

            case MatchMode.Substring:
                if (path.IndexOf(oldPath, StringComparison.Ordinal) < 0)
                    return false;
                remappedPath = path.Replace(oldPath, newPath);
                return remappedPath != path;

            default:
                return false;
        }
    }

    public static List<BindingChange> CollectChanges(
        IEnumerable<AnimationClip> clips,
        string oldPath,
        string newPath,
        MatchMode mode)
    {
        var changes = new List<BindingChange>();
        if (clips == null)
            return changes;

        foreach (var clip in clips)
        {
            if (clip == null)
                continue;

            CollectCurveChanges(clip, oldPath, newPath, mode, objectReference: false, changes);
            CollectCurveChanges(clip, oldPath, newPath, mode, objectReference: true, changes);
        }

        return changes;
    }

    public static RemapResult Preview(
        IEnumerable<AnimationClip> clips,
        string oldPath,
        string newPath,
        MatchMode mode)
    {
        var clipSet = DistinctClips(clips);
        var changes = CollectChanges(clipSet, oldPath, newPath, mode);
        return new RemapResult(clipSet.Count, changes);
    }

    /// <summary>
    /// 바인딩 경로를 치환한다. dryRun이면 변경하지 않고 결과만 반환한다.
    /// </summary>
    public static RemapResult Remap(
        IEnumerable<AnimationClip> clips,
        string oldPath,
        string newPath,
        MatchMode mode,
        bool dryRun = false)
    {
        var clipSet = DistinctClips(clips);
        var changes = CollectChanges(clipSet, oldPath, newPath, mode);
        if (dryRun || changes.Count == 0)
            return new RemapResult(clipSet.Count, changes);

        var dirtyClips = new HashSet<AnimationClip>();
        foreach (var change in changes)
        {
            if (!dirtyClips.Add(change.Clip))
                continue;

            Undo.RecordObject(change.Clip, "Remap AnimationClip Paths");
            ApplyToClip(change.Clip, oldPath, newPath, mode);
            EditorUtility.SetDirty(change.Clip);
        }

        AssetDatabase.SaveAssets();
        return new RemapResult(dirtyClips.Count, changes);
    }

    public static List<AnimationClip> FindClipsUnderFolder(string folderAssetPath)
    {
        var result = new List<AnimationClip>();
        if (string.IsNullOrEmpty(folderAssetPath))
            return result;

        folderAssetPath = folderAssetPath.Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folderAssetPath))
            return result;

        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderAssetPath });
        var seen = new HashSet<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!seen.Add(path))
                continue;

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
                result.Add(clip);
        }

        return result;
    }

    static void CollectCurveChanges(
        AnimationClip clip,
        string oldPath,
        string newPath,
        MatchMode mode,
        bool objectReference,
        List<BindingChange> changes)
    {
        var bindings = objectReference
            ? AnimationUtility.GetObjectReferenceCurveBindings(clip)
            : AnimationUtility.GetCurveBindings(clip);

        foreach (var binding in bindings)
        {
            if (!TryRemapPath(binding.path, oldPath, newPath, mode, out var remapped))
                continue;

            changes.Add(new BindingChange(
                clip,
                binding.path,
                remapped,
                binding.propertyName,
                objectReference));
        }
    }

    static void ApplyToClip(AnimationClip clip, string oldPath, string newPath, MatchMode mode)
    {
        RemapBindings(clip, oldPath, newPath, mode, objectReference: false);
        RemapBindings(clip, oldPath, newPath, mode, objectReference: true);
    }

    static void RemapBindings(
        AnimationClip clip,
        string oldPath,
        string newPath,
        MatchMode mode,
        bool objectReference)
    {
        var bindings = objectReference
            ? AnimationUtility.GetObjectReferenceCurveBindings(clip)
            : AnimationUtility.GetCurveBindings(clip);

        foreach (var binding in bindings)
        {
            if (!TryRemapPath(binding.path, oldPath, newPath, mode, out var remapped))
                continue;

            if (objectReference)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                var newBinding = binding;
                newBinding.path = remapped;
                AnimationUtility.SetObjectReferenceCurve(clip, newBinding, keys);
            }
            else
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, null);
                var newBinding = binding;
                newBinding.path = remapped;
                AnimationUtility.SetEditorCurve(clip, newBinding, curve);
            }
        }
    }

    static List<AnimationClip> DistinctClips(IEnumerable<AnimationClip> clips)
    {
        var list = new List<AnimationClip>();
        var set = new HashSet<AnimationClip>();
        if (clips == null)
            return list;

        foreach (var clip in clips)
        {
            if (clip == null || !set.Add(clip))
                continue;
            list.Add(clip);
        }

        return list;
    }
}
#endif
