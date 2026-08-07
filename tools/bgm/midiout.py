"""최소 MIDI 파일 라이터. 외부 의존성 없음.

Format 1, 480 ticks/quarter. 트랙당 악기 하나 (설계 문서 11절).
시간 단위는 theme.py와 같은 **8분음표**이고, 여기서 tick으로 바꾼다.
"""

import struct

TPQ = 480              # ticks per quarter
TICKS_PER_EIGHTH = TPQ // 2

# 팔레트 → General MIDI 음색. 자작 신스가 진짜 소리를 내고,
# 이 번호는 DAW에서 MIDI를 열었을 때 얼추 맞게 들리라고 넣는 것이다.
GM_PROGRAM = {
    "musicbox": 10,   # Music Box
    "pluck": 46,      # Orchestral Harp
    "pad": 89,        # Pad 2 (warm)
    "lead": 80,       # Lead 1 (square)
    "bass": 38,       # Synth Bass 1
    "guitar": 30,     # Distortion Guitar
    "ebass": 34,      # Electric Bass (finger)
}
DRUM_CHANNEL = 9

# 자작 타악 이름 → GM 드럼 노트
GM_DRUM = {"kick": 36, "snare": 38, "hat": 42, "rim": 37, "shaker": 82,
           "ride": 51, "crash": 49, "ohat": 46}


def _varlen(n):
    out = bytearray([n & 0x7F])
    n >>= 7
    while n:
        out.insert(0, (n & 0x7F) | 0x80)
        n >>= 7
    return bytes(out)


def _chunk(tag, body):
    return tag + struct.pack(">I", len(body)) + body


def _track(events):
    """events: [(absolute_tick, bytes), ...] → MTrk 청크"""
    events = sorted(events, key=lambda e: (e[0], e[1][0] & 0xF0 != 0x80))
    body, prev = bytearray(), 0
    for tick, data in events:
        body += _varlen(tick - prev) + data
        prev = tick
    body += _varlen(0) + b"\xff\x2f\x00"  # end of track
    return _chunk(b"MTrk", bytes(body))


def write(path, bpm, parts, name="theme", meter=(6, 8)):
    """parts: [{'inst': str, 'notes': [(start_eighth, dur_eighth, pitch, vel)]}]

    bpm 은 **한 박** 기준이다 — 6/8이면 점4분음표(8분음표 3개),
    4/4면 4분음표(8분음표 2개). MIDI 템포는 4분음표 기준이라 환산해서 넣는다.
    """
    num, den = meter
    eighths_per_beat = 3 if den == 8 else 2
    us_per_quarter = int(60_000_000 * 2 / (bpm * eighths_per_beat))

    #             박자표          클릭 = 한 박
    timesig = bytes([num, den.bit_length() - 1, 36 if den == 8 else 24, 8])
    meta = [
        (0, b"\xff\x03" + _varlen(len(name)) + name.encode()),
        (0, b"\xff\x58\x04" + timesig),
        (0, b"\xff\x51\x03" + us_per_quarter.to_bytes(3, "big")),
    ]
    tracks = [_track(meta)]

    channel = 0
    for part in parts:
        inst = part["inst"]
        drums = inst == "drums"
        if drums:
            ch = DRUM_CHANNEL
            events = []
        else:
            ch = channel
            channel += 1
            if channel == DRUM_CHANNEL:
                channel += 1
            events = [(0, bytes([0xC0 | ch, GM_PROGRAM[inst]]))]
        events.append((0, b"\xff\x03" + _varlen(len(inst)) + inst.encode()))

        for note in part["notes"]:
            start, dur, p, vel = note
            if drums:
                p = GM_DRUM[p]
            on = int(round(start * TICKS_PER_EIGHTH))
            off = on + max(1, int(round(dur * TICKS_PER_EIGHTH)))
            events.append((on, bytes([0x90 | ch, p & 0x7F, vel & 0x7F])))
            events.append((off, bytes([0x80 | ch, p & 0x7F, 0])))
        tracks.append(_track(events))

    header = _chunk(b"MThd", struct.pack(">HHH", 1, len(tracks), TPQ))
    with open(path, "wb") as f:
        f.write(header + b"".join(tracks))
