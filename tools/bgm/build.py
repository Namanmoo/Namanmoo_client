"""4곡을 MIDI와 OGG로 내보낸다.

    python3 tools/bgm/build.py

산출물 (설계 문서 11절):
    Assets/Audio/Bgm/midi~/*.mid  악보 원본. 나중에 좋은 음원으로 다시 렌더할 수 있다
    Assets/Audio/Bgm/wav~/*.wav   무압축 음원. vorbis 손실 없는 원본
    Assets/Audio/Bgm/*.ogg        지금 바로 게임에 붙는 버전
    Assets/Audio/Bgm/*.ogg.meta   임포트 설정. 기본값으로 들어가면 사고가 난다

폴더 이름이 `midi~` `wav~`인 이유: 유니티는 `~`로 끝나는 폴더를 통째로 무시한다.
MIDI는 유니티에 임포터가 없어 에셋 목록만 더럽히고, WAV는 40MB가 넘어 그냥 두면
같은 음악이 빌드에 두 번 실린다. `Assets/Audio/unfixed~/`와 같은 규칙이다.

루프 이음매: 루프 길이를 넘어간 소리(리버브 꼬리, 마디를 넘긴 지속음)를
곡 앞머리에 **되접어 더한다.** 그래서 끝과 처음이 끊기지 않는다.
"""

import os
import subprocess
import struct
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import midiout                                    # noqa: E402
import songs                                      # noqa: E402
import synth                                      # noqa: E402
from synth import SR                              # noqa: E402

OUT_OGG = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Audio", "Bgm"))
OUT_MIDI = os.path.join(OUT_OGG, "midi~")

# 무압축 WAV도 같이 남긴다. `.ogg`를 만들기 직전의 그 렌더 그대로다 —
# 즉 vorbis 손실이 없는 원본이다. 들어보거나 DAW에 넣을 때 쓴다.
#
# 폴더가 `wav~`인 이유: 4곡 합쳐 40MB가 넘는다. 유니티가 이걸 에셋으로 잡으면
# 같은 음악이 빌드에 두 번 실린다. `~`로 끝나는 폴더는 유니티가 통째로 무시한다.
OUT_WAV = os.path.join(OUT_OGG, "wav~")

TAIL_S = 8.0          # 루프 뒤로 흘러넘칠 소리를 담아둘 여유

# 4곡의 체감 음량을 맞춘다. 씬이 바뀔 때 볼륨이 튀거나 꺼지면
# 곡이 아무리 좋아도 "소리가 이상한 게임"이 된다. 피크가 아니라 RMS로 맞춘다.
# BGM은 효과음 밑에 깔린다. 꽉 채우면 안 된다.
# 피크 한도가 0.80인 이유: Vorbis는 표본 **사이**에서 튀어서, WAV에서 0.94였던
# 것이 디코드하면 1.0을 넘어 클리핑됐다. 인코딩 오버슛만큼 미리 비워 둔다.
TARGET_RMS_DB = -14.5
PEAK_CEILING = 0.80


def _render(song):
    """편곡 → (2, N) 스테레오. 되접기까지 끝난 루프 한 바퀴."""
    num, den = song.get("meter", (6, 8))
    eighths_per_beat = 3 if den == 8 else 2       # 6/8은 점4분, 4/4는 4분이 한 박
    eighths_per_bar = num * 8 // den
    sec_per_eighth = 60.0 / (song["bpm"] * eighths_per_beat)
    loop_n = int(round(song["bars"] * eighths_per_bar * sec_per_eighth * SR))
    work_n = loop_n + int(TAIL_S * SR)

    dry = np.zeros((2, work_n))
    send = np.zeros((2, work_n))

    for pi, part in enumerate(song["parts"]):
        voice = synth.VOICES[part["inst"]]
        pan = part["pan"]
        gl, gr = np.sqrt((1 - pan) / 2), np.sqrt((1 + pan) / 2)
        sendamt = part["send"]

        for ni, (start, dur, key, vel) in enumerate(part["notes"]):
            pos = int(round(start * sec_per_eighth * SR))
            if pos >= work_n:
                continue
            amp = (vel / 100.0) ** 1.35
            seed = pi * 7919 + ni
            if part["inst"] == "drums":
                y = voice(key, amp=amp, seed=seed)
            else:
                y = voice(key, dur * sec_per_eighth, amp=amp, seed=seed)
            end = min(work_n, pos + len(y))
            y = y[:end - pos]
            dry[0, pos:end] += y * gl
            dry[1, pos:end] += y * gr
            send[0, pos:end] += y * gl * sendamt
            send[1, pos:end] += y * gr * sendamt

    tau, mix = song["reverb"]
    wet = synth.reverb(send, tau=tau, mix=1.0, seed=hash(song["name"]) % 9973)
    total = np.zeros((2, max(work_n, wet.shape[1])))
    total[:, :work_n] += dry
    total[:, :wet.shape[1]] += wet * mix

    if song.get("loop", True):
        # 되접기 — 루프를 넘어간 소리를 앞머리로 돌려보낸다
        out = np.zeros((2, loop_n))
        i = 0
        while i < total.shape[1]:
            seg = total[:, i:i + loop_n]
            out[:, :seg.shape[1]] += seg
            i += loop_n
    else:
        # 전주 같은 한 번짜리 — 되접지 않고 리버브 꼬리를 그대로 남긴다.
        # 꼬리 2.5초를 두고 마지막 0.3초만 살짝 접어 무음에 앉힌다.
        keep = loop_n + int(2.5 * SR)
        out = total[:, :keep].copy()
        fade = int(0.3 * SR)
        out[:, -fade:] *= np.linspace(1, 0, fade)

    target = 10 ** (TARGET_RMS_DB / 20.0)
    for _ in range(3):                              # 리미터가 깎는 만큼 되돌린다
        rms = np.sqrt(np.mean(out ** 2))
        if rms <= 0:
            break
        out = np.tanh(out * (target / rms) * 0.95) / np.tanh(0.95)
    peak = np.max(np.abs(out))
    if peak > PEAK_CEILING:
        out *= PEAK_CEILING / peak
    return out


# BGM 임포트 설정. 효과음과 **정반대**라 기본값으로 들어가면 사고가 난다
# (`Assets/Audio/README.md`). Decompress On Load가 걸리면 수십 MB가 메모리에 올라간다.
#   loadType 2        = Streaming
#   compressionFormat 1 = Vorbis
#   sampleRateSetting 0 = 원본 유지 (44100Hz)
#   forceToMono 0     = 스테레오 유지
#   normalize 0       = 유니티가 다시 정규화하지 못하게. 4곡 음량은 이미 맞춰 놨다
_META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 2
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.7
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 0
  preloadAudioData: 0
  loadInBackground: 1
  ambisonic: 0
  3D: 0
  userData:{sp}
  assetBundleName:{sp}
  assetBundleVariant:{sp}
"""


def _write_meta(ogg_path):
    """.ogg.meta 를 만든다. 이미 있으면 GUID를 지켜 준다 — 씬 참조가 끊긴다."""
    path = ogg_path + ".meta"
    guid = None
    if os.path.exists(path):
        with open(path, encoding="utf-8") as f:
            for line in f:
                if line.startswith("guid:"):
                    guid = line.split(":", 1)[1].strip()
                    break
    if not guid:
        import hashlib
        guid = hashlib.md5(
            ("namanmoo/bgm/" + os.path.basename(ogg_path)).encode()).hexdigest()
    with open(path, "w", encoding="utf-8") as f:
        f.write(_META.format(guid=guid, sp=" "))


def _write_wav(path, stereo):
    pcm = np.clip(stereo.T, -1, 1)
    data = (pcm * 32767.0).astype("<i2").tobytes()
    with open(path, "wb") as f:
        f.write(b"RIFF" + struct.pack("<I", 36 + len(data)) + b"WAVE")
        f.write(b"fmt " + struct.pack("<IHHIIHH", 16, 1, 2, SR, SR * 4, 4, 16))
        f.write(b"data" + struct.pack("<I", len(data)) + data)


def main():
    os.makedirs(OUT_MIDI, exist_ok=True)
    os.makedirs(OUT_WAV, exist_ok=True)

    for build in songs.ALL:
        song = build()
        name = song["name"]

        midi_parts = [{"inst": p["inst"], "notes": p["notes"]} for p in song["parts"]]
        midi_path = os.path.join(OUT_MIDI, name + ".mid")
        midiout.write(midi_path, song["bpm"], midi_parts, name=name,
                      meter=song.get("meter", (6, 8)))

        audio = _render(song)
        wav_path = os.path.join(OUT_WAV, name + ".wav")
        _write_wav(wav_path, audio)
        ogg_path = os.path.join(OUT_OGG, name + ".ogg")
        subprocess.run(
            ["ffmpeg", "-y", "-loglevel", "error", "-i", wav_path,
             "-c:a", "libvorbis", "-q:a", "5", "-ar", "44100", "-ac", "2", ogg_path],
            check=True)
        _write_meta(ogg_path)

        n_notes = sum(len(p["notes"]) for p in song["parts"])
        print(f"{name:9s} {audio.shape[1] / SR:6.2f}s  "
              f"{song['bars']:3d}마디  점4분={song['bpm']:3d}  "
              f"{n_notes:4d}음  → {name}.ogg(+meta), wav~/{name}.wav, "
              f"midi~/{name}.mid")


if __name__ == "__main__":
    main()
