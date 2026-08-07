using System.Collections;
using UnityEngine;

namespace NaManMoo.Audio
{
    /// <summary>
    /// 씬 배경음을 튼다. 기본은 한 곡 무한 반복이고, 두 가지를 더 할 수 있다.
    ///
    /// <b>전주.</b> 인트로 클립을 주면 한 번만 틀고 본곡 루프로 넘어간다. 전환은
    /// <see cref="AudioSource.PlayScheduled"/>로 샘플 단위로 잇는다 — 프레임 단위로
    /// 이으면 이음매에서 박자가 민다.
    ///
    /// <b>곡 교체.</b> <see cref="CrossfadeTo"/>가 두 번째 AudioSource로 다음 곡을
    /// 겹쳐 올리며 페이드한다. 보스방 진입, 보스 페이즈 전환이 쓴다.
    ///
    /// BGM 파일은 이음매가 물리도록 만들어져 있어(<c>Assets/Audio/Bgm/MUSIC_DESIGN.md</c>)
    /// 루프 자체에는 페이드를 걸지 않는다 — 걸면 루프마다 음량이 출렁인다.
    ///
    /// <b>소리가 없어도 게임은 진행된다.</b> 클립이 비어 있으면 경고만 남기고 넘어간다.
    /// 음악 때문에 판을 막지 않는다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BgmPlayer : MonoBehaviour
    {
        /// <summary>
        /// BGM은 효과음 밑에 깔린다. 파일 자체를 -14.5dBFS로 맞춰 뒀지만
        /// 실제로 들어 보니 0.55는 앞으로 나왔다. 효과음이 붙을 자리를 미리 비워 둔다.
        /// </summary>
        public const float DefaultVolume = 0.45f;

        /// <summary>
        /// 곡 교체 페이드 길이. 형제 곡끼리 넘어가는 자리라 길게 끌지 않는다.
        /// </summary>
        public const float DefaultCrossfadeSeconds = 1.5f;

        /// <summary>
        /// 예약 재생 여유. 이보다 촉박하게 예약하면 첫 버퍼를 놓쳐 시작이 잘린다.
        /// </summary>
        private const double ScheduleLeadSeconds = 0.1;

        [SerializeField] private AudioClip introClip;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = DefaultVolume;

        // 두 소스를 번갈아 쓴다. active가 본곡, standby가 전주 또는 페이드로 물러나는 곡.
        private AudioSource activeSource;
        private AudioSource standbySource;
        private Coroutine crossfade;
        private Coroutine introSchedule;

        /// <summary>지금 목표로 삼은 본곡. 곡 교체 요청이 겹칠 때 중복을 거른다.</summary>
        public AudioClip LoopClip => clip;

        /// <summary>씬 빌더가 에디터에서 곡을 꽂을 때 쓴다.</summary>
        public void Configure(AudioClip bgm, float bgmVolume = DefaultVolume)
        {
            Configure(bgm, null, bgmVolume);
        }

        /// <summary>전주 한 번 → 본곡 루프. 전주가 없으면 null을 준다.</summary>
        public void Configure(AudioClip bgm, AudioClip intro, float bgmVolume = DefaultVolume)
        {
            clip = bgm;
            introClip = intro;
            volume = Mathf.Clamp01(bgmVolume);
            Apply();
        }

        /// <summary>
        /// 지금 곡을 줄이며 다음 곡을 처음부터 겹쳐 올린다. 이미 그 곡이면 아무것도 하지
        /// 않으므로 방을 오갈 때마다 불러도 된다. 전주가 울리는 중이면 전주를 줄이고
        /// 전주 뒤에 예약된 본곡은 취소한다 — 새 곡이 그 자리를 차지한다.
        /// </summary>
        public void CrossfadeTo(AudioClip nextLoop, float seconds = DefaultCrossfadeSeconds)
        {
            if (nextLoop == null || nextLoop == clip)
            {
                return;
            }

            clip = nextLoop;

            // 아직 소리가 나기 전(Start 이전·에디터)이면 목표 곡만 바꿔 둔다.
            if (activeSource == null || standbySource == null || !isActiveAndEnabled)
            {
                Apply();
                return;
            }

            if (crossfade != null)
            {
                StopCoroutine(crossfade);
            }

            // 아직 예약 전인 전주가 있으면 취소한다 — 새 곡 위에 뒤늦게 전주가 얹히면 안 된다.
            if (introSchedule != null)
            {
                StopCoroutine(introSchedule);
                introSchedule = null;
                introClip = null;
            }

            AudioSource from = activeSource;
            AudioSource to = standbySource;
            if (introClip != null && standbySource.isPlaying && standbySource.clip == introClip)
            {
                // 전주 중이다. 본곡 소스는 예약만 걸려 있으므로 세우고 새 곡에 내준다.
                // 전주는 여기서 소비된 것으로 친다 — 페이드아웃 중인 전주를 다음 교체가
                // 또 전주로 오인하면 방금 올린 곡을 세워 버린다.
                from = standbySource;
                to = activeSource;
                to.Stop();
                introClip = null;
            }

            to.clip = nextLoop;
            to.loop = true;
            to.volume = 0f;
            to.Play();

            activeSource = to;
            standbySource = from;
            crossfade = StartCoroutine(Crossfade(from, to, Mathf.Max(0.01f, seconds)));
        }

        private void Awake()
        {
            Apply();
        }

        private void Start()
        {
            if (clip == null)
            {
                Debug.LogWarning($"{name}: BGM 클립이 없어 재생하지 않습니다.", this);
                return;
            }

            if (introClip == null)
            {
                activeSource.Play();
                return;
            }

            introSchedule = StartCoroutine(ScheduleIntroThenLoop());
        }

        /// <summary>
        /// 브라우저(WebGL)는 첫 사용자 입력 전까지 오디오 시계를 잠근다. 잠긴 시계로
        /// 절대 시각 예약을 걸면 깨어났을 때 전주 구간이 이미 지나 있어 한동안 무음이
        /// 된다 — 시계가 실제로 흐르기 시작한 뒤에 예약한다. 데스크톱에서는 첫 프레임에
        /// 바로 통과하므로 체감 차이가 없다.
        /// </summary>
        private IEnumerator ScheduleIntroThenLoop()
        {
            double frozen = AudioSettings.dspTime;
            while (AudioSettings.dspTime <= frozen)
            {
                yield return null;
            }

            // 전주 길이는 samples/frequency로 센다 — AudioClip.length는 float라 오차가 있다.
            double startTime = AudioSettings.dspTime + ScheduleLeadSeconds;
            double introLength = (double)introClip.samples / introClip.frequency;
            standbySource.clip = introClip;
            standbySource.loop = false;
            standbySource.PlayScheduled(startTime);
            activeSource.PlayScheduled(startTime + introLength);
            introSchedule = null;
        }

        private IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                // 같은 음량 두 곡이 겹치는 구간이므로 등파워 곡선을 쓴다.
                // 직선으로 섞으면 한가운데에서 3dB 꺼진다.
                float t = Mathf.Clamp01(elapsed / seconds) * Mathf.PI * 0.5f;
                from.volume = Mathf.Cos(t) * volume;
                to.volume = Mathf.Sin(t) * volume;
                yield return null;
            }

            from.Stop();
            from.volume = volume;
            crossfade = null;
        }

        private void Apply()
        {
            if (activeSource == null)
            {
                activeSource = GetComponent<AudioSource>();
            }

            // 두 번째 소스는 실행 중에만 만든다. 씬 파일에는 소스 하나만 저장된다.
            if (standbySource == null && Application.isPlaying)
            {
                standbySource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureSource(activeSource);
            activeSource.clip = clip;
            activeSource.loop = true;
            activeSource.volume = volume;

            if (standbySource != null)
            {
                ConfigureSource(standbySource);
                standbySource.volume = volume;
            }
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            // 2D로 고정한다. 3D면 카메라가 움직일 때 음량과 좌우가 흔들린다.
            source.spatialBlend = 0f;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
        }
    }
}
