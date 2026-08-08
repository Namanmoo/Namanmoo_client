using UnityEditor;
using UnityEngine;

/// <summary>
/// 한쪽 방향 클립에 찍은 "Weapon Hand" 커브를 반대쪽 클립에 좌우 반전해 복사한다.
///
/// 몸 그림의 Left/Right 프레임은 서로 거울상이므로 손 위치도 x만 반대고,
/// 칼끝 방향(z 회전)도 반대로 돈다. 한쪽만 공들여 찍으면 반대쪽은 이걸로 끝 —
/// 그림이 완전한 거울상이 아니라면 복사 후 어긋난 프레임만 손보면 된다.
/// </summary>
public static class WeaponHandCurveMirror
{
    private const string AnimationRoot = "Assets/Player/Animation";
    private const string HandPath = "Weapon Hand";

    [MenuItem("Tools/NaManMoo/Mirror Weapon Hand Curves/Left → Right")]
    public static void MirrorLeftToRight()
    {
        MirrorAll("Left", "Right");
    }

    [MenuItem("Tools/NaManMoo/Mirror Weapon Hand Curves/Right → Left")]
    public static void MirrorRightToLeft()
    {
        MirrorAll("Right", "Left");
    }

    private static void MirrorAll(string fromSide, string toSide)
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:AnimationClip", new[] { AnimationRoot });
        int mirrored = 0;
        foreach (string guid in guids)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
            if (!sourcePath.Contains(fromSide))
            {
                continue;
            }

            // 폴더와 파일 이름의 방향 표기를 통째로 바꾸면 반대쪽 클립 경로다
            string targetPath = sourcePath.Replace(fromSide, toSide);
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
            var target = AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath);
            if (source == null || target == null || !MirrorClip(source, target))
            {
                continue;
            }

            mirrored++;
            Debug.Log($"무기 손 커브 반전 복사: {sourcePath} → {targetPath}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log(mirrored > 0
            ? $"클립 {mirrored}개에 반전 복사 완료"
            : $"복사할 Weapon Hand 커브가 있는 {fromSide} 클립이 없습니다");
    }

    /// <summary>커브가 하나라도 복사됐으면 true.</summary>
    private static bool MirrorClip(AnimationClip source, AnimationClip target)
    {
        EditorCurveBinding[] sourceBindings = AnimationUtility.GetCurveBindings(source);
        bool any = false;
        foreach (EditorCurveBinding binding in sourceBindings)
        {
            if (binding.path == HandPath)
            {
                any = true;
                break;
            }
        }

        if (!any)
        {
            return false;
        }

        // 대상의 기존 Weapon Hand 커브를 전부 지운다 — 예전에 찍다 만
        // 속성이 남아 섞이면 이상한 중간 자세가 나온다
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(target))
        {
            if (binding.path == HandPath)
            {
                AnimationUtility.SetEditorCurve(target, binding, null);
            }
        }

        foreach (EditorCurveBinding binding in sourceBindings)
        {
            if (binding.path != HandPath)
            {
                continue;
            }

            AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
            if (NeedsNegation(binding.propertyName))
            {
                curve = Negate(curve);
            }

            AnimationUtility.SetEditorCurve(target, binding, curve);
        }

        EditorUtility.SetDirty(target);
        return true;
    }

    private static bool NeedsNegation(string propertyName)
    {
        // 거울상: x 위치는 반대쪽, z 회전(칼끝 방향)은 반대로 돈다.
        // 회전은 녹화 방식에 따라 쿼터니언 또는 오일러 커브로 저장된다 —
        // z축 회전만 쓰는 2D라 어느 쪽이든 z 성분 부호만 뒤집으면 된다.
        return propertyName == "m_LocalPosition.x"
            || propertyName == "m_LocalRotation.z"
            || propertyName == "localEulerAnglesRaw.z";
    }

    private static AnimationCurve Negate(AnimationCurve curve)
    {
        Keyframe[] keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].value = -keys[i].value;
            keys[i].inTangent = -keys[i].inTangent;
            keys[i].outTangent = -keys[i].outTangent;
        }

        return new AnimationCurve(keys)
        {
            preWrapMode = curve.preWrapMode,
            postWrapMode = curve.postWrapMode,
        };
    }
}
