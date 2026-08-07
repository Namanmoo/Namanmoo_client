"""자작 신디사이저. numpy만 쓴다.

이 컴퓨터에는 fluidsynth·timidity·사운드폰트가 없다. 그래서 직접 합성한다.
설계 문서 9절의 팔레트 6종은 **이 제약을 전제로 고른 것**이다 —
가산합성으로 정직하게 좋은 소리가 나는 것들만 골랐다.

모든 악기는 가산합성(부분음 합)이다. 필터를 안 쓰므로 에일리어싱이 없고,
파이썬 루프도 없어서 빠르다.
"""

import numpy as np

SR = 44100


def _tim(dur_s):
    return np.arange(max(1, int(dur_s * SR))) / SR


def _hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12.0)


def _noise(n, seed):
    return np.random.default_rng(seed).standard_normal(n)


# --- 1. 뮤직박스 / 벨 ------------------------------------------------------
# 노스탤지어의 핵. 빗살을 튕기는 금속 tine 소리 — 비조화 부분음 + 빠른 감쇠.
# 악보 길이와 무관하게 울린다. 실제 뮤직박스가 그렇다.

_BELL = [(1.00, 1.00, 1.0), (2.00, 0.42, 1.8), (3.01, 0.20, 2.7),
         (4.17, 0.11, 3.6), (5.43, 0.06, 4.8), (6.79, 0.035, 6.0)]


def musicbox(midi, dur_s, amp=1.0, seed=0):
    f = _hz(midi)
    tau = 1.25 * (440.0 / f) ** 0.35        # 높은 음일수록 빨리 사라진다
    t = _tim(min(3.0, max(dur_s, 2.0 * tau)))
    y = np.zeros_like(t)
    for ratio, a, dmul in _BELL:
        if f * ratio > SR * 0.45:
            break
        y += a * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t * dmul / tau)
    y *= 1 - np.exp(-t / 0.0015)            # 아주 짧은 어택
    tick = _noise(min(len(t), int(0.004 * SR)), seed) * np.exp(
        -np.arange(min(len(t), int(0.004 * SR))) / (0.0007 * SR))
    y[:len(tick)] += tick * 0.14            # 빗살에 걸리는 '틱'
    return y * amp * 0.30


# --- 2. 뜯는 현 (하프·기타) ------------------------------------------------
# 「동행」. 내가 그린 것. 고배음일수록 빨리 죽는 현의 물리를 그대로 쓴다.

def pluck(midi, dur_s, amp=1.0, seed=0, bright=1.0):
    f = _hz(midi)
    tau1 = 1.7 * (440.0 / f) ** 0.25
    t = _tim(min(3.2, max(dur_s + 0.4, 1.2)))
    y = np.zeros_like(t)
    n = 1
    while f * n < SR * 0.45 and n <= 26:
        a = bright / n ** 1.25
        y += a * np.sin(2 * np.pi * f * n * t + (n * 1.7) % 6.283) * \
            np.exp(-t * n ** 0.72 / tau1)
        n += 1
    y *= 1 - np.exp(-t / 0.002)
    k = min(len(t), int(0.006 * SR))
    y[:k] += _noise(k, seed + 991) * np.exp(-np.arange(k) / (0.0012 * SR)) * 0.10
    return y * amp * 0.26


# --- 3. 부드러운 패드 ------------------------------------------------------
# 공간, 세계. 살짝 어긋나게 겹친 3성. 느리게 들어오고 느리게 나간다.

def pad(midi, dur_s, amp=1.0, seed=0):
    f = _hz(midi)
    atk, rel = 0.35, 0.55
    t = _tim(dur_s + rel)
    y = np.zeros_like(t)
    for det in (-0.006, 0.0, 0.007):
        fd = f * (1 + det)
        n = 1
        while fd * n < SR * 0.42 and n <= 14:
            y += (1.0 / n ** 1.65) * np.sin(
                2 * np.pi * fd * n * t + (n * det * 900) % 6.283)
            n += 1
    env = np.minimum(1.0, t / atk)
    tail = np.clip((t - dur_s) / rel, 0, 1)
    env *= (1 - tail) ** 1.6
    lfo = 1 + 0.035 * np.sin(2 * np.pi * 0.19 * t + seed)
    return y * env * lfo * amp * 0.075


# --- 4. 칩튠 리드 (구형파) -------------------------------------------------
# 전투 에너지. 옛날 게임기의 정체성. 홀수 배음만 — 대역제한 구형파.

def lead(midi, dur_s, amp=1.0, seed=0, duty_harm=17):
    f = _hz(midi)
    t = _tim(dur_s + 0.09)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.6 * t) * np.clip((t - 0.12) / 0.15, 0, 1)
    ph = 2 * np.pi * f * np.cumsum(vib) / SR
    y = np.zeros_like(t)
    for n in range(1, duty_harm * 2, 2):
        if f * n > SR * 0.45:
            break
        y += np.sin(n * ph) / n
    env = np.minimum(1.0, t / 0.004)
    env *= np.where(t < dur_s, 1.0 - 0.18 * np.clip(t / 0.35, 0, 1),
                    np.clip(1 - (t - dur_s) / 0.09, 0, 1) * 0.82)
    return y * env * amp * 0.17


# --- 5. 베이스 -------------------------------------------------------------

def bass(midi, dur_s, amp=1.0, seed=0):
    f = _hz(midi)
    t = _tim(dur_s + 0.06)
    y = np.sin(2 * np.pi * f * t) * 1.0
    for n in range(2, 11):
        if f * n > SR * 0.45:
            break
        y += np.sin(2 * np.pi * f * n * t) / n ** 1.75
    env = np.minimum(1.0, t / 0.005)
    env *= np.where(t < dur_s, np.exp(-t * 0.55),
                    np.clip(1 - (t - dur_s) / 0.06, 0, 1) * np.exp(-t * 0.55))
    return y * env * amp * 0.30


# --- 7. 일렉 기타 (드라이브) ----------------------------------------------
# 보스 2페이즈 전용. 디스토션은 **파형을 찌그러뜨리는 것**이므로 코드로 만들 수 있다.
#
# 순서가 중요하다: 엔벨로프를 **먼저** 씌우고 그 다음에 찌그러뜨린다.
# 그래야 실제 앰프처럼 서스테인이 압축되어 길게 늘어진다. 반대로 하면
# 소리가 커졌다 작아지기만 하고 기타가 안 된다.

def _drive(x, gain):
    return np.tanh(x * gain) / np.tanh(gain)


def _cabinet(y, taps=5):
    """캐비닛 흉내. 고역을 깎아 쏘는 소리를 죽인다 — 이게 없으면 톱밥 소리가 난다."""
    return np.convolve(y, np.ones(taps) / taps, mode="same")


def guitar(midi, dur_s, amp=1.0, seed=0, gain=7.0):
    f = _hz(midi)
    t = _tim(dur_s + 0.14)
    y = np.zeros_like(t)
    for det, ph in ((-0.0016, 0.0), (0.0019, 1.1)):   # 줄 두 개의 미세한 음정 차이
        fd = f * (1 + det)
        n = 1
        while fd * n < SR * 0.45 and n <= 12:
            y += np.sin(2 * np.pi * fd * n * t + ph + n * 0.31) / n
            n += 1
    env = np.minimum(1.0, t / 0.004)
    env *= np.where(t < dur_s, np.exp(-t * 0.45),
                    np.clip(1 - (t - dur_s) / 0.14, 0, 1) * np.exp(-t * 0.45))
    return _cabinet(_drive(y * env, gain)) * amp * 0.13


def ebass(midi, dur_s, amp=1.0, seed=0):
    """오버드라이브 베이스. 기타보다 약하게 물려 저역이 뭉개지지 않게 한다."""
    f = _hz(midi)
    t = _tim(dur_s + 0.07)
    y = np.sin(2 * np.pi * f * t)
    for n in range(2, 9):
        if f * n > SR * 0.45:
            break
        y += np.sin(2 * np.pi * f * n * t) / n ** 1.3
    env = np.minimum(1.0, t / 0.004)
    env *= np.where(t < dur_s, np.exp(-t * 0.5),
                    np.clip(1 - (t - dur_s) / 0.07, 0, 1) * np.exp(-t * 0.5))
    return _drive(y * env, 2.6) * amp * 0.26


# --- 6. 타악 ---------------------------------------------------------------
# 노이즈 + 사인. 하이패스는 1차 차분으로 대신한다.

def _hp(x):
    return np.diff(x, prepend=0.0)


def drum(kind, dur_s=0.0, amp=1.0, seed=0):
    if kind == "kick":
        t = _tim(0.34)
        f = 48 + 92 * np.exp(-t / 0.028)
        y = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.115)
        k = int(0.003 * SR)
        y[:k] += _hp(_noise(k, seed)) * 0.25 * np.exp(-np.arange(k) / (0.0008 * SR))
        return y * amp * 0.62
    if kind == "snare":
        t = _tim(0.20)
        n = _hp(_noise(len(t), seed + 7)) * np.exp(-t / 0.055)
        tone = (np.sin(2 * np.pi * 190 * t) + 0.6 * np.sin(2 * np.pi * 288 * t)) \
            * np.exp(-t / 0.032)
        return (n * 0.75 + tone * 0.42) * amp * 0.34
    if kind == "hat":
        t = _tim(0.055)
        y = _hp(_hp(_noise(len(t), seed + 13))) * np.exp(-t / 0.013)
        return y * amp * 0.10
    if kind == "shaker":
        t = _tim(0.075)
        env = np.minimum(1.0, t / 0.008) * np.exp(-t / 0.021)
        return _hp(_hp(_noise(len(t), seed + 29))) * env * amp * 0.075
    if kind == "ride":
        # 재즈의 뼈대. 짧게 끊지 않고 길게 울려야 스윙이 산다.
        t = _tim(0.9)
        y = _hp(_noise(len(t), seed + 41)) * np.exp(-t / 0.26)
        for fr in (523.0, 741.0, 1043.0, 1487.0):      # 금속 부분음
            y += np.sin(2 * np.pi * fr * t) * np.exp(-t / 0.33) * 0.11
        return y * amp * 0.055
    if kind == "crash":
        t = _tim(1.6)
        y = _hp(_hp(_noise(len(t), seed + 53))) * np.exp(-t / 0.52)
        return y * amp * 0.085
    if kind == "ohat":
        t = _tim(0.28)
        y = _hp(_hp(_noise(len(t), seed + 17))) * np.exp(-t / 0.085)
        return y * amp * 0.070
    if kind == "rim":
        t = _tim(0.045)
        y = _hp(_noise(len(t), seed + 31)) * np.exp(-t / 0.006)
        y += np.sin(2 * np.pi * 840 * t) * np.exp(-t / 0.011) * 0.55
        return y * amp * 0.17
    raise KeyError(kind)


VOICES = {
    "musicbox": musicbox, "pluck": pluck, "pad": pad,
    "lead": lead, "bass": bass, "drums": drum,
    "guitar": guitar, "ebass": ebass,
}


# --- 리버브 ----------------------------------------------------------------
# FFT 컨볼루션. 좌우를 다른 난수로 만들어 넓힌다.

def _ir(tau, seed, predelay=0.012, lo=0.5):
    n = int(tau * 3.2 * SR)
    t = np.arange(n) / SR
    h = _noise(n, seed) * np.exp(-t / tau)
    k = np.ones(9) / 9.0                    # 아주 순한 저역 통과 (거칠음 제거)
    h = np.convolve(h, k, mode="same") * lo + h * (1 - lo)
    return np.concatenate([np.zeros(int(predelay * SR)), h]) / np.sqrt(n)


def _fftconv(x, h):
    n = len(x) + len(h) - 1
    N = 1 << int(np.ceil(np.log2(n)))
    return np.fft.irfft(np.fft.rfft(x, N) * np.fft.rfft(h, N), N)[:n]


def reverb(stereo, tau=1.6, mix=0.25, seed=1234):
    """(2, N) 스테레오에 리버브를 건다. 반환 길이는 꼬리만큼 길어진다."""
    left = _fftconv(stereo[0], _ir(tau, seed))
    right = _fftconv(stereo[1], _ir(tau, seed + 555))
    wet = np.stack([left, right])
    out = np.zeros_like(wet)
    out[:, :stereo.shape[1]] = stereo * (1 - mix)
    return out + wet * mix
