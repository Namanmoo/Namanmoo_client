using System.Collections.Generic;
using UnityEngine;

namespace NaManMoo.Audio
{
    /// <summary>
    /// 씬 하나에 하나 있는 효과음 재생기. 씬 빌더가 오디오 폴더를 훑어 클립을 구워 두면
    /// (<c>파일명이 곧 등록</c> — Assets/Audio/README.md), 전투 코드는
    /// <see cref="Instance"/>로 찾아와 이름 후보를 넘긴다.
    ///
    /// <b>소리가 없어도 게임은 진행된다.</b> 재생기가 씬에 없거나 후보가 전부 비어도
    /// 조용히 넘어간다 — 호출하는 쪽은 <c>SfxPlayer.Instance?.Play(...)</c> 한 줄이면 된다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        /// <summary>기본 이름 → 변형들. <c>swing_any_metal_2</c>는 <c>swing_any_metal</c>로 묶인다.</summary>
        private Dictionary<string, List<AudioClip>> library;
        private AudioSource source;

        /// <summary>씬 빌더가 에디터에서 클립을 구워 넣을 때 쓴다.</summary>
        public void Configure(AudioClip[] bakedClips, float sfxVolume = 1f)
        {
            clips = bakedClips;
            volume = Mathf.Clamp01(sfxVolume);
            library = null;
        }

        /// <summary>
        /// 후보를 위에서부터 훑어 처음 있는 이름을 튼다. 같은 이름에 변형이 여럿이면
        /// 그중 하나를 무작위로 고른다. 틀었으면 true.
        /// </summary>
        public bool Play(IReadOnlyList<string> candidates)
        {
            if (candidates == null || source == null)
            {
                return false;
            }

            EnsureLibrary();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!library.TryGetValue(candidates[i], out List<AudioClip> variants))
                {
                    continue;
                }

                source.PlayOneShot(variants[Random.Range(0, variants.Count)], volume);
                return true;
            }

            return false;
        }

        private void Awake()
        {
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            // 2D로 고정한다. 3D면 카메라가 움직일 때 음량과 좌우가 흔들린다.
            source.spatialBlend = 0f;
            EnsureLibrary();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureLibrary()
        {
            if (library != null)
            {
                return;
            }

            library = new Dictionary<string, List<AudioClip>>();
            if (clips == null)
            {
                return;
            }

            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                string key = BaseNameOf(clip.name);
                if (!library.TryGetValue(key, out List<AudioClip> variants))
                {
                    variants = new List<AudioClip>();
                    library[key] = variants;
                }

                variants.Add(clip);
            }
        }

        /// <summary>끝의 변형 번호를 뗀다 — 어휘에 숫자 단어가 없으므로 안전하다.</summary>
        public static string BaseNameOf(string clipName)
        {
            int cut = clipName.LastIndexOf('_');
            if (cut <= 0 || cut == clipName.Length - 1)
            {
                return clipName;
            }

            for (int i = cut + 1; i < clipName.Length; i++)
            {
                if (!char.IsDigit(clipName[i]))
                {
                    return clipName;
                }
            }

            return clipName.Substring(0, cut);
        }
    }
}
