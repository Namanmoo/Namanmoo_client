using System.Collections.Generic;
using NaManMoo.Audio;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 효과음 폴더를 훑어 씬에 <see cref="SfxPlayer"/> 하나를 굽는다.
/// "파일명이 곧 등록"(Assets/Audio/README.md) — 폴더에 파일을 넣고 씬을 다시 지으면
/// 잡히고, 빼면 사라진다. 어떤 목록에도 손으로 적지 않는다.
/// </summary>
public static class SfxSceneBuilder
{
    private static readonly string[] Folders =
    {
        "Assets/Audio/Weapon",
        "Assets/Audio/Impact",
        "Assets/Audio/Enemy",
        "Assets/Audio/Effect"
    };

    public static void Create()
    {
        var clips = new List<AudioClip>();
        foreach (string folder in Folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                continue;
            }

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:AudioClip", new[] { folder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
        }

        if (clips.Count == 0)
        {
            // 소리가 없어도 게임은 돌아간다. 씬 만들기를 막지 않는다.
            Debug.LogWarning("효과음 클립이 하나도 없습니다. 재생기 없이 만듭니다.");
            return;
        }

        var sfxObject = new GameObject("SFX");
        sfxObject.AddComponent<SfxPlayer>().Configure(clips.ToArray());
    }
}
