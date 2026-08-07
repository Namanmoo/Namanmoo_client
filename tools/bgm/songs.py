"""4곡의 편곡. `Assets/Audio/Bgm/MUSIC_DESIGN.md` 3·4·10절을 코드로 옮긴 것이다.

곡은 4개지만 **노래는 하나**다. 멜로디는 전부 theme.py에서 오고,
여기서 바뀌는 것은 템포·조성·편성·밀도뿐이다 (설계 문서 5절 허용 목록).

악기 배분 자체가 서사다:
    title    뮤직박스 혼자          → 혼자였다
    stage1   뜯는 현이 넘겨받는다    → 동행이 생겼다
    dungeon  뮤직박스가 조각난다     → 멀어졌다
    boss     칩튠이 빼앗아간다       → 빼앗겼다
"""

from theme import (
    BAR, pitch, ORBIT, CLIMB, CLIMB_DRIVE, LAND, THEME, LAND_OPEN,
    P1_ORBIT, P1_CLIMB, P1_THEME, P1_DRIVE,
    P2_ORBIT, P2_CLIMB, P2_THEME, P2_DRIVE,
    ORBIT_FRAGMENT, ORBIT_RETRO,
    to_minor, voice, bass_pitch, chord,
    PROG_MAJOR, PROG_RELATIVE, PROG_RELATIVE_RECALL, PROG_PARALLEL,
)


# --- 배치 도우미 -----------------------------------------------------------

def place(notes, at_bar, octaves=0, minor=False, bar=BAR):
    """악구를 지정한 마디에 놓는다."""
    if minor:
        notes = to_minor(notes)
    base = (min(s for s, _, _ in notes) // bar) * bar
    d = 12 * octaves
    return [(s - base + at_bar * bar, dur, p + d) for (s, dur, p) in notes]


def sung(notes, vel=88, accent=10, bar=BAR):
    """멜로디에 벨로시티를 입힌다. 마디 안 위치로 강세를 준다.

    박 자리는 0과 bar//2 — 6/8은 0·3, 4/4는 0·4.
    """
    out = []
    for (s, d, p) in notes:
        pos = s % bar
        v = vel + (accent if pos == 0 else accent // 2 if pos == bar // 2 else -4)
        out.append((s, d, p, max(1, min(127, int(v)))))
    return out


def arc(segments, accent=10, bar=BAR):
    """[(악구, 벨로시티)] → 다이내믹 커브가 입혀진 하나의 멜로디.

    설계 문서 10절: A → A' → B(정점) → A''. 커브는 여기서 만들어진다.
    """
    out = []
    for notes, vel in segments:
        out.extend(sung(notes, vel, accent, bar))
    return out


def spread(prog, bar0, bars):
    """진행을 마디 수만큼 늘린다."""
    return [(bar0 + i, prog[i % len(prog)]) for i in range(bars)]


# --- 반주 도우미 -----------------------------------------------------------

def stabs(chords, vel, low, high, offsets=(1, 2, 4, 5), dur=1, bar=BAR):
    """박 사이를 채우는 반주. 6/8은 0·3을 비운다 — 흔들림이 여기서 나온다."""
    out = []
    for i, (b, name) in enumerate(chords):
        v = voice(name, low, high)
        for j, off in enumerate(offsets):
            out.append((b * bar + off, dur, v[(j + i) % len(v)], vel))
    return out


def roll(chords, vel, low, high):
    """구르는 아르페지오. 8분음표 6개를 다 채운다."""
    seq = (0, 1, 2, 1, 2, 1)
    out = []
    for bar, name in chords:
        v = voice(name, low, high)
        for off, idx in enumerate(seq):
            out.append((bar * BAR + off, 1, v[idx % len(v)],
                        vel + (8 if off in (0, 3) else 0)))
    return out


def sustain(chords, vel, low, high, bar=BAR):
    """마디 전체를 덮는 지속음 (패드)."""
    out = []
    for b, name in chords:
        for p in voice(name, low, high):
            out.append((b * bar, bar, p, vel))
    return out


def walk(chords, vel, low=36, high=47, offsets=(0, 3), bar=BAR):
    out = []
    for b, name in chords:
        p = bass_pitch(name, low, high)
        for off in offsets:
            out.append((b * bar + off, 3, p, vel + (6 if off == 0 else 0)))
    return out


def beat(bar0, bars, pattern, bar=BAR):
    """pattern: [(8분음표 위치, 타악 이름, 벨로시티)]"""
    out = []
    for b in range(bar0, bar0 + bars):
        for off, kind, vel in pattern:
            out.append((b * bar + off, 1, kind, vel))
    return out


# --- 재즈·락 도우미 (보스 2페이즈 전용) ------------------------------------
#
# 여기 있는 것들은 설계 문서 8·9절의 원칙을 **의도적으로 연다**.
# 3화음 원칙 → 7화음, 고정 팔레트 6종 → 일렉 기타·베이스.
# 보스전 한정이고, 문서에도 그렇게 적어 두었다.

SCALE_EB_MINOR = {3, 5, 6, 8, 10, 11, 1}     # 미♭ 파 솔♭ 라♭ 시♭ 도♭ 레♭
SCALE_EB_MAJOR = {3, 5, 7, 8, 10, 0, 2}      # 미♭ 파 솔 라♭ 시♭ 도 레


def _at(pc, low, high):
    p = low + (pc - low) % 12
    return p - 12 if p > high else p


def seventh(name, scale, low=55, high=72):
    """3화음 + 그 조의 7음.

    문서 6절은 3화음을 원칙으로 하고 7화음을 아껴 쓰라고 한다.
    **재즈판에서만** 그 문을 연다 — 7음 없이는 재즈가 안 된다.
    """
    _, tones = chord(name)
    sev = (tones[0] + 10) % 12
    if sev not in scale:
        sev = (tones[0] + 11) % 12
    return sorted(_at(pc, low, high) for pc in list(tones) + [sev])


def power(name, low=40, high=51):
    """파워코드 — 근음·5음·옥타브. **3음을 뺀다.**

    디스토션은 배음을 잔뜩 만들어 내는데, 3음이 끼면 그 배음들이 서로 부딪혀
    탁해진다. 파워코드가 존재하는 이유가 그것이다.
    """
    r = _at(chord(name)[1][0], low, high)
    return [r, r + 7, r + 12]


def walking(chords, vel, low=33, high=45):
    """재즈 워킹 베이스.

    박마다 한 음씩 걸어간다 — 근음에서 출발해 화음 안을 지나고, 마디 끝에서
    **다음 마디 근음의 반음 아래**에 붙는다. 그 반음이 다음 마디를 끌어당긴다.
    베이스가 쉬지 않고 걷는 것이 재즈 리듬감의 뼈대다.
    """
    out = []
    for i, (bar, name) in enumerate(chords):
        _, tones = chord(name)
        nxt = chord(chords[(i + 1) % len(chords)][1])[1][0]
        seq = ((0.0, tones[0], vel + 8),
               (2.0, tones[2] if i % 2 else tones[1], vel - 3),
               (3.0, tones[1] if i % 2 else tones[2], vel + 3),
               (5.0, (nxt - 1) % 12, vel - 7))
        for off, pc, v in seq:
            out.append((bar * BAR + off, 1.5, _at(pc, low, high), v))
    return out


def comp_jazz(chords, vel, scale, low=55, high=72):
    """컴핑 — 뒷박에 짧게 찌르는 코드.

    **마디당 한 번뿐이다.** 처음엔 두 번씩 찔렀는데 멜로디·라이드·베이스와
    겹쳐 정신 사나웠다. 컴핑은 양념이다 — 밥이 아니다.
    박(0·3)에는 절대 놓지 않는다. 박은 베이스와 라이드가 이미 잡고 있다.
    """
    spots = (4, 2, 5, 4)
    out = []
    for i, (bar, name) in enumerate(chords):
        off = spots[i % len(spots)]
        for p in seventh(name, scale, low, high):
            out.append((bar * BAR + off + 0.07, 1.2, p, vel))
    return out


def ride_swing(bar0, bars, vel):
    """재즈 라이드 — 박(0·3)과 그 박의 **마지막** 8분음표(2·5).

    6/8은 한 박이 셋으로 나뉘므로 가운데(1·4)를 비우면 그 자체로 스윙이 된다.
    스윙을 따로 흉내 낼 필요가 없다 — 박자가 이미 셋잇단이기 때문이다.
    """
    out = []
    for b in range(bar0, bar0 + bars):
        for off, v in ((0, vel + 10), (2, vel - 10), (3, vel + 4), (5, vel - 10)):
            out.append((b * BAR + off, 1, "ride", v))
    return out


def laid_back(notes, amount=0.05):
    """뒷박을 아주 조금 늦춘다. 이게 없으면 재즈가 기계처럼 들린다."""
    return [(s + (amount if s % BAR in (1, 2, 4, 5) else 0.0), d, p, v)
            for (s, d, p, v) in notes]


def _merge(*groups):
    out = []
    for g in groups:
        out.extend(g)
    return out


def _part(inst, notes, pan=0.0, send=0.25):
    return {"inst": inst, "notes": sorted(notes), "pan": pan, "send": send}


# =========================================================================
# 1. title — E♭ 장조, 점4분 = 60, 16마디 (32초)
#
#    16마디 중 12마디는 악기가 **하나뿐**이다. 외롭게 시작해야 한다.
#    13마디째에 뜯는 현이 들어오는 것 — 그게 이 게임의 논지 전체다.
# =========================================================================

def title():
    prog = ["Eb", "Gm", "Ab", "Bb"] * 2 + ["Gm", "Cm", "Db", "Eb"] + \
           ["Eb", "Gm", "Db", "Eb"]
    ch = [(i, prog[i]) for i in range(16)]

    mel = arc([
        (place(ORBIT, 0), 72),                              # A   외롭게
        (place(ORBIT, 4), 84),                              # A'  낮은 대구가 붙는다
        (place(CLIMB, 8) + place(LAND, 10), 106),           # B   상행 — 정점
        (place(ORBIT[:10], 12) + place(LAND, 14), 88),      # A'' 뜯는 현 합류
    ], accent=12)

    # 낮은 뮤직박스 대구. 2마디째부터 아주 조용히 스며든다.
    low = _merge(
        [(b * BAR, 3, bass_pitch(n, 48, 59), 40) for b, n in ch[2:4]],
        roll(ch[4:8], 34, 53, 64),
        roll(ch[8:12], 56, 53, 67),
        roll(ch[12:16], 40, 53, 64),
    )

    # 뜯는 현 — 마지막 4마디에서만. 무언가 곁에 선다.
    companion = _merge(
        stabs(ch[12:16], 52, 60, 76, offsets=(0, 2, 3, 5), dur=2),
        [(12 * BAR, 3, bass_pitch("Eb", 41, 52), 46)],
    )

    return {
        "name": "title", "bpm": 60, "bars": 16, "reverb": (2.4, 0.34),
        "parts": [
            _part("musicbox", mel, pan=-0.05, send=0.42),
            _part("musicbox", low, pan=0.10, send=0.38),
            _part("pluck", companion, pan=0.18, send=0.34),
        ],
    }


# =========================================================================
# 2. stage1 — E♭ 장조, 점4분 = 84, 48마디 (69초)
#
#    타이틀에서 곁에 서기만 했던 뜯는 현이 여기선 앞장선다.
#    멜로디 주인이 바뀌는 것이 이 곡의 핵심이다.
# =========================================================================

def stage1():
    ch = spread(PROG_MAJOR, 0, 48)
    st = [ch[i * 8:(i + 1) * 8] for i in range(6)]

    # A(0-7) A'(8-15) B(16-31) A''(32-47).  정점은 B다 — 문서 10절.
    lead_mel = arc([(place(THEME, i * 8), v)
                    for i, v in enumerate([72, 82, 96, 106, 92, 80])])

    # 뮤직박스는 장식으로 물러난다. 4번째 진술부터 한 옥타브 위에서 겹친다.
    deco = _merge(
        [(s, d, p, 52) for (s, d, p) in place(ORBIT, 16)],
        [(s, d, p, 58) for (s, d, p) in place(THEME, 32, octaves=1)],
        [(s, d, p, 50) for (s, d, p) in place(CLIMB, 44)],
    )

    # 편성도 멜로디와 같은 커브를 그린다. B(16-31)가 가장 두껍고 A''(32-)는 걷힌다.
    pads = _merge(sustain(st[0], 34, 60, 74), sustain(st[1], 46, 60, 74),
                  sustain(st[2], 56, 60, 74), sustain(st[3], 62, 60, 74),
                  sustain(st[4], 48, 60, 74), sustain(st[5], 36, 60, 74))
    low = _merge(walk(st[0], 46, offsets=(0,)), walk(st[1], 58),
                 walk(st[2], 68), walk(st[3], 76),
                 walk(st[4], 62), walk(st[5][:4], 50))

    drums = _merge(
        beat(8, 8, [(o, "shaker", 34 if o in (0, 3) else 22) for o in range(6)]),
        beat(16, 8, [(o, "shaker", 44 if o in (0, 3) else 30) for o in range(6)] +
             [(0, "kick", 66)]),
        beat(24, 8, [(o, "shaker", 50 if o in (0, 3) else 34) for o in range(6)] +
             [(0, "kick", 78), (3, "kick", 62)]),
        beat(32, 8, [(o, "shaker", 40 if o in (0, 3) else 26) for o in range(6)] +
             [(0, "kick", 64)]),
        beat(40, 4, [(o, "shaker", 34) for o in (0, 3)] + [(0, "kick", 56)]),
    )

    return {
        "name": "stage1", "bpm": 84, "bars": 48, "reverb": (1.9, 0.26),
        "parts": [
            _part("pluck", lead_mel, pan=-0.12, send=0.30),
            _part("musicbox", deco, pan=0.22, send=0.36),
            _part("pad", pads, pan=0.0, send=0.30),
            _part("bass", low, pan=0.0, send=0.10),
            _part("drums", drums, pan=0.06, send=0.14),
        ],
    }


# =========================================================================
# 3. dungeon — C 단조(나란한조), 점4분 = 80, 64마디 (96초)
#
#    조표가 E♭ 장조와 **같다.** 그래서 플레이어는 어두워진 것을 의식하지 못한 채
#    불안해진다. 그리고 상행은 루프 후반까지 오지 않는다 —
#    미뤄진 정점이 던전의 긴장 그 자체다.
#
#    **네 곡 중 가장 느리다.** stage1(84)보다도 느려서, 던전에 들어가면 걸음이
#    무거워지는 느낌이 난다. 긴장을 속도로 만들지 않고 무게로 만든다.
#
#    반복을 견디게 하는 것이 이 곡의 최대 과제다. 되뇜 하나로 96초를 버텨야 하므로
#    **진행·모양·악기·음역·정적을 진술마다 전부 바꾼다.** 벨로시티만 바꾸면 지겹다.
# =========================================================================

def dungeon(major=False):
    # 진행을 네 가지로 돌린다. 같은 8마디가 여덟 번 반복되면 그것만으로 지겹다.
    #
    # 나란한조라 **멜로디는 장조·단조가 같다.** 조성을 가르는 건 화음뿐이다 —
    # 같은 음들 위에서 무게중심을 도(C)에 두느냐 미♭(E♭)에 두느냐의 차이다.
    #
    # 그래서 장조판은 화음만 바꿔서는 **같은 곡으로 들린다** (실측: 멜로디·드럼
    # 100% 동일, 파형 상관 0.674). 장조판은 진행뿐 아니라 **편곡을 통째로
    # 갈아입는다** — 멜로디를 밝은 악기가 제 옥타브에서 부르고, 단화음을 걷어내고,
    # 타악을 가볍게. 같은 노래의 낮과 밤이다.
    if major:
        P1 = ["Eb", "Ab", "Eb", "Bb", "Eb", "Ab", "Db", "Eb"]   # 장화음 위주
        P2 = ["Ab", "Eb", "Bb", "Eb", "Ab", "Bb", "Db", "Eb"]
        P3 = ["Eb", "Bb", "Ab", "Bb", "Eb", "Ab", "Db", "Eb"]
        PR = ["Eb", "Gm", "Ab", "Bb", "Eb", "Ab", "Db", "Eb"]
    else:
        P1 = PROG_RELATIVE                                   # 기본
        P2 = ["Cm", "Eb", "Ab", "Bb", "Gm", "Eb", "Fm", "Cm"]  # 장조 쪽으로 흔들림
        P3 = ["Gm", "Cm", "Fm", "Gm", "Cm", "Ab", "Fm", "Cm"]  # 더 가라앉는다
        PR = PROG_RELATIVE_RECALL                            # E♭를 스치는 판
    plan = [P1, P2, P1, P3, P2, P3, PR, P1]
    ch = [(i, plan[i // 8][i % 8]) for i in range(64)]
    st = [ch[i * 8:(i + 1) * 8] for i in range(8)]

    # 단조판: 낮은 옥타브의 칩튠 펄스 (어둡다)
    # 장조판: 제 옥타브의 뜯는 현 (밝다) — 악기와 음역이 조성보다 크게 들린다
    lo = dict(octaves=0 if major else -1)

    # 펄스 — 되뇜의 뼈대. 다만 **혼자 다 끌지 않는다.**
    # 3·6번 진술에서는 아예 빠져서 다른 악기에 자리를 내준다.
    pulse = arc([
        (place(ORBIT, 0, **lo), 60),                                    # 0-3, 4-7은 정적
        (place(ORBIT, 8, **lo) + place(ORBIT_RETRO, 12, **lo)
         + place(ORBIT_RETRO, 14, **lo), 66),                           # 역행이 일찍 낀다
        # 16-23 : 뮤직박스에게 넘긴다
        (place(ORBIT_RETRO, 24, **lo) + place(ORBIT_RETRO, 26, **lo), 72),  # 28-31 정적
        (place(ORBIT, 32, **lo) + place(ORBIT_RETRO, 36, **lo)
         + place(ORBIT_RETRO, 38, **lo), 76),
        (place(ORBIT_RETRO, 40, **lo) + place(ORBIT_RETRO, 42, **lo)
         + place(ORBIT, 44, **lo), 82),                                 # 조여든다
        # 48-55 : 상행이 나오는 동안 펄스는 물러난다
        (place(ORBIT_FRAGMENT, 56, **lo) + place(ORBIT_FRAGMENT, 58, **lo), 54),
    ], accent=8)

    # 뮤직박스 — 3번째 진술에서는 **멜로디를 통째로 가져간다**(한 옥타브 위).
    # 나머지 자리에서는 조각만 흘린다. 동행이 가까워졌다 멀어진다.
    box = _merge(
        [(s, d, p, 58) for (s, d, p) in place(ORBIT, 16, octaves=1)],
        [(s, d, p, 50) for (s, d, p) in place(ORBIT_FRAGMENT, 21, octaves=1)],
        [(s, d, p, 44) for (s, d, p) in place(ORBIT_FRAGMENT, 29, octaves=1)],
        [(s, d, p, 46) for (s, d, p) in place(ORBIT_FRAGMENT, 34)],
        [(s, d, p, 42) for (s, d, p) in place(ORBIT_FRAGMENT, 46, octaves=1)],
        [(s, d, p, 40) for (s, d, p) in place(ORBIT_FRAGMENT, 61, octaves=1)],
    )

    # 뜯는 현 — 6번째 진술의 역행, 그리고 루프 후반의 단 한 번뿐인 상행.
    # 그 마디에서 E♭ 장조를 스친다 — 기억이 스치듯.
    strings = _merge(
        sung(place(ORBIT_RETRO, 40) + place(ORBIT_RETRO, 44), 62, 8),
        arc([(place(ORBIT, 48), 84),
             (place(CLIMB, 52) + place(LAND, 54), 100)], accent=12),
    )

    # 첫 마디부터 소리가 있어야 한다. 여긴 정지 화면이 아니라 싸우는 곳이다.
    # 다만 4·28-31마디처럼 **멜로디가 쉬는 자리**를 만들어 귀를 쉬게 한다.
    # 장조판은 패드도 한 층 위에서 깐다 — 음역이 밝기의 절반이다.
    plo, phi = (62, 76) if major else (57, 71)
    pads = _merge(sustain(st[0], 52, plo, phi), sustain(st[1], 54, plo, phi),
                  sustain(st[2], 50, plo, phi), sustain(st[3], 56, plo, phi),
                  sustain(st[4], 58, plo, phi), sustain(st[5], 58, plo, phi),
                  sustain(st[6], 60, plo, phi), sustain(st[7], 48, plo, phi))
    low = _merge(walk(st[0], 68, offsets=(0,)), walk(st[1], 72, offsets=(0, 3)),
                 walk(st[2], 66, offsets=(0,)), walk(st[3], 74, offsets=(0, 3)),
                 walk(st[4], 76, offsets=(0, 3)), walk(st[5], 78, offsets=(0, 2, 3)),
                 walk(st[6], 78, offsets=(0, 3)), walk(st[7], 64, offsets=(0,)))

    # 타악도 진술마다 무늬를 바꾼다. 세 번째와 네 번째 후반은 아예 뺀다 — 정적이 무기다.
    if major:
        # 장조판 — 킥·림을 걷어내고 셰이커가 흔든다. stage1의 걸음에 가깝다
        drums = _merge(
            beat(0, 4, [(0, "shaker", 40), (3, "shaker", 30)]),
            beat(4, 4, [(3, "shaker", 26)]),
            beat(8, 8, [(o, "shaker", 40 if o in (0, 3) else 24) for o in range(6)]),
            beat(16, 8, [(0, "kick", 48)] +
                 [(o, "shaker", 36 if o in (0, 3) else 22) for o in range(6)]),
            beat(24, 4, [(0, "kick", 54), (3, "shaker", 34), (5, "shaker", 26)]),
            # 28-31 : 무음
            beat(32, 8, [(0, "kick", 56)] +
                 [(o, "shaker", 40 if o in (0, 3) else 26) for o in range(6)]),
            beat(40, 8, [(0, "kick", 60), (3, "kick", 44)] +
                 [(o, "shaker", 42 if o in (0, 3) else 28) for o in range(6)]),
            beat(48, 8, [(0, "kick", 64), (3, "kick", 48), (5, "hat", 24)] +
                 [(o, "shaker", 44 if o in (0, 3) else 30) for o in range(6)]),
            beat(56, 6, [(0, "shaker", 36), (3, "shaker", 28)]),
        )
    else:
        drums = _merge(
            beat(0, 4, [(0, "kick", 58), (3, "rim", 46)]),
            beat(4, 4, [(3, "rim", 40)]),                   # 멜로디가 쉬는 자리
            beat(8, 8, [(0, "kick", 64), (3, "rim", 50), (5, "shaker", 28)]),
            beat(16, 8, [(0, "kick", 56), (2, "shaker", 26), (4, "shaker", 24)]),
            beat(24, 4, [(0, "kick", 72), (3, "rim", 54), (5, "hat", 26)]),
            # 28-31 : 무음. 되뇜도 타악도 없다
            beat(32, 8, [(0, "kick", 74), (3, "rim", 56), (5, "hat", 30)]),
            beat(40, 8, [(0, "kick", 76), (3, "rim", 58), (2, "hat", 26), (5, "hat", 30)]),
            beat(48, 8, [(0, "kick", 78), (3, "rim", 60), (2, "hat", 28), (5, "hat", 32)]),
            beat(56, 6, [(0, "kick", 62), (3, "rim", 46)]),
        )

    return {
        "name": "dungeon_major" if major else "dungeon",
        "bpm": 80, "bars": 64, "reverb": (2.0, 0.28) if major else (2.4, 0.34),
        "parts": [
            # 장조판은 되뇜을 뜯는 현이, 단조판은 낮은 칩튠이 맡는다
            _part("pluck" if major else "lead", pulse,
                  pan=-0.10, send=0.30 if major else 0.22),
            _part("musicbox", box, pan=0.26, send=0.40 if major else 0.46),
            _part("pluck", strings, pan=-0.04, send=0.32),
            _part("pad", pads, pan=0.0, send=0.30 if major else 0.34),
            _part("bass", low, pan=0.0, send=0.10),
            _part("drums", drums, pan=0.04, send=0.16),
        ],
    }


# =========================================================================
# 4. boss — E♭ 단조(평행단조), 점4분 = 138, 64마디 (56초)
#
#    시작음이 같아서 같은 노래임이 **즉시** 들린다. 배신당한 자장가다.
#    상행은 한 번으로 안 끝나고, 아치는 닫히지 않는다.
#    해결은 음악이 주지 않는다 — 보스를 죽여야 온다.
# =========================================================================

def boss(major=False):
    # 장조판은 `to_minor`를 끄고 진행만 E♭ 장조로 바꾼 것이다. 편곡은 손대지 않는다.
    #
    # 매듭(35마디) 화음이 장조·단조에서 다른 이유: 하행이 솔을 지나는데,
    # 단조에서는 솔♭이라 D♭의 4음으로 맞지만 장조에서는 솔♮이라 D♭ 위에서 증4도가 된다.
    # 그래서 장조판은 B♭(V) → E♭(I) 정격종지로 닫는다.
    if major:
        base = PROG_MAJOR
        intro = ["Eb", "Eb", "Cm", "Db"]
        drive = ["Gm", "Cm"] * 3 + ["Bb", "Eb"]
        tail = ["Cm", "Cm", "Bb", "Bb"]
    else:
        base = PROG_PARALLEL
        intro = ["Ebm", "Ebm", "Bbm", "Db"]
        drive = ["Gb", "Abm"] * 3 + ["Db", "Ebm"]  # 상행3 + 매듭1
        tail = ["Bbm", "Bbm", "Db", "Db"]
    prog = intro + base * 3 + drive + base + base * 2 + tail
    ch = [(i, prog[i]) for i in range(64)]

    m = dict(minor=not major)

    # 칩튠 리드가 멜로디를 빼앗아 간다
    chip = arc([
        (place(CLIMB[:2], 3, **m), 76),                 # 도입 끝 — 상행 조각 예고
        (place(THEME, 4, **m), 88),                     # A
        (place(THEME, 12, **m), 96),                    # A'
        (place(THEME, 20, **m), 100),
        (place(CLIMB_DRIVE, 28, **m), 112),             # B — 상행 3번 + 마무리 1번
        (place(THEME, 36, **m), 104),
        # A'' — 아치가 닫히지 않는다. 집으로 내려와야 할 자리에서 계속 위로 밀린다
        (place(ORBIT, 44, **m) + place(CLIMB, 48, **m)
         + place(LAND_OPEN, 50, **m), 106),
        (place(ORBIT, 52, **m) + place(CLIMB, 56, **m)
         + place(LAND_OPEN, 58, **m), 110),
    ], accent=10)

    # 뮤직박스는 거의 사라졌다. 도입과 꼬리에 유령처럼 남는다.
    ghost = _merge(
        [(s, d, p, 38) for (s, d, p) in place(ORBIT_FRAGMENT, 0, octaves=1, **m)],
        [(s, d, p, 34) for (s, d, p) in place(ORBIT_FRAGMENT, 60, octaves=1, **m)],
    )

    pads = sustain(ch[4:60], 46, 53, 67)
    low = _merge(
        walk(ch[0:4], 78, offsets=(0, 3)),
        walk(ch[4:60], 84, offsets=(0, 2, 3, 5)),
        walk(ch[60:64], 80, offsets=(0, 3)),
    )

    drive = [(0, "kick", 96), (3, "snare", 84)] + \
            [(o, "hat", 40 if o in (0, 3) else 28) for o in range(6)]
    hammer = drive + [(2, "kick", 66), (5, "snare", 62)]   # 상행을 두들기는 구간
    drums = _merge(
        beat(0, 4, [(0, "kick", 88), (3, "kick", 72), (5, "hat", 34)]),
        beat(4, 24, drive),
        beat(28, 8, hammer),
        beat(36, 24, drive),
        beat(60, 4, drive + [(2, "snare", 58), (5, "snare", 66)]),
    )

    return {
        "name": "boss_major" if major else "boss",
        "bpm": 138, "bars": 64, "reverb": (1.3, 0.20),
        "parts": [
            _part("lead", chip, pan=-0.08, send=0.18),
            _part("musicbox", ghost, pan=0.28, send=0.52),
            _part("pad", pads, pan=0.0, send=0.26),
            _part("bass", low, pan=0.0, send=0.08),
            _part("drums", drums, pan=0.05, send=0.12),
        ],
    }


# =========================================================================
# 4-2. boss_p1 — 1페이즈 · 재즈 (E♭ 장조, 점4분 = 116, 64마디 · 66초)
#
#    감독 지시: 2페이즈 보스. 소스는 **장조판(boss_major)**이다.
#    1페이즈는 리듬감 — 재즈의 문법을 빌린다. 단 **양념은 적게**:
#    첫 판은 컴핑 2회/마디 + 멜로디 늦춤까지 겹쳐 정신 사나웠다.
#    지금은 라이드 스윙 + 워킹 베이스가 뼈대이고, 컴핑은 마디당 1번,
#    멜로디는 정직하게 박을 지킨다. 늦추는 것은 컴핑뿐이다.
# =========================================================================

def boss_p1():
    base = PROG_MAJOR
    intro = ["Eb", "Eb", "Cm", "Db"]
    drv = ["Gm", "Cm"] * 3 + ["Bb", "Eb"]
    tail = ["Cm", "Cm", "Bb", "Bb"]
    prog = intro + base * 3 + drv + base + base * 2 + tail
    ch = [(i, prog[i]) for i in range(64)]

    # 멜로디는 **대체 선율** — 리듬은 boss_major의 THEME 그대로, 음만 새로 썼다.
    # 시♭을 축으로 맴돌고 9음을 스치는, 정점 낮은(미♭5) 재즈 선율.
    # 뜯는 현이 부른다. 1페이즈는 아직 노래를 빼앗기기 전이다.
    mel = arc([
        (place(P1_CLIMB[:2], 3), 72),
        (place(P1_THEME, 4), 84),
        (place(P1_THEME, 12), 88),
        (place(P1_THEME, 20), 92),
        (place(P1_DRIVE, 28), 102),
        (place(P1_THEME, 36), 94),
        (place(P1_ORBIT, 44) + place(P1_CLIMB, 48) + place(LAND_OPEN, 50), 96),
        (place(P1_ORBIT, 52) + place(P1_CLIMB, 56) + place(LAND_OPEN, 58), 100),
    ], accent=8)

    # 컴핑 — 마디당 1번, 늦춰서. 유일한 재즈 양념.
    comping = laid_back(comp_jazz(ch[8:60], 40, SCALE_EB_MAJOR, 58, 72))

    walkbass = walking(ch, 70)

    drums = _merge(
        ride_swing(0, 64, 44),
        beat(4, 56, [(0, "kick", 50)]),
        [(b * BAR + 3, 1, "snare", 28 + (b * 7) % 10) for b in range(8, 60, 8)],
        [(62 * BAR, 1, "crash", 56)],
    )

    return {
        "name": "boss_p1", "bpm": 116, "bars": 64, "reverb": (1.6, 0.24),
        "parts": [
            _part("pluck", mel, pan=-0.08, send=0.24),
            _part("pluck", comping, pan=0.20, send=0.28),
            _part("ebass", walkbass, pan=0.0, send=0.08),
            _part("drums", drums, pan=0.05, send=0.14),
        ],
    }


# =========================================================================
# 4-3. boss_p2 — 2페이즈 · 락 (E♭ 단조, 점4분 = 138, 64마디 · 56초)
#
#    소스는 **장조판**. 2페이즈는 1페이즈 위에 일렉을 얹는다 — 같은 노래가 무장한다.
#    멜로디를 칩튠이 빼앗아 간다. 1페이즈에서 뜯는 현이 부르던 그 노래다.
#    드라이브 기타(파워코드), 오버드라이브 베이스, 오픈햇 백비트.
#    파워코드에 3음이 없는 이유: 디스토션 배음끼리 부딪혀 탁해진다.
#    재즈의 늦춤·컴핑은 걷어낸다 — 락은 박을 정면으로 친다.
# =========================================================================

def boss_p2():
    base = PROG_MAJOR
    intro = ["Eb", "Eb", "Cm", "Db"]
    drv = ["Gm", "Cm"] * 3 + ["Bb", "Eb"]
    tail = ["Cm", "Cm", "Bb", "Bb"]
    prog = intro + base * 3 + drv + base + base * 2 + tail
    ch = [(i, prog[i]) for i in range(64)]
    m = dict(minor=False)

    # 멜로디는 **대체 선율** — 리듬은 boss_major의 THEME 그대로, 음만 새로 썼다.
    # 5음(시♭)을 반복해 두들기고 4·5도로 크게 뛰는 락 선율. 정점은 원곡처럼 솔5.
    # 칩튠이 빼앗아 최고 강도로 외친다. 끝은 올라가며 닫는다 — 주먹을 든 채.
    chip = arc([
        (place(P2_CLIMB[:2], 3, **m), 80),
        (place(P2_THEME, 4, **m), 96),
        (place(P2_THEME, 12, **m), 102),
        (place(P2_THEME, 20, **m), 106),
        (place(P2_DRIVE, 28, **m), 118),
        (place(P2_THEME, 36, **m), 108),
        (place(P2_ORBIT, 44, **m) + place(P2_CLIMB, 48, **m)
         + place(LAND_OPEN, 50, **m), 110),
        (place(P2_ORBIT, 52, **m) + place(P2_CLIMB, 56, **m)
         + place(LAND_OPEN, 58, **m), 114),
    ], accent=10)

    # 드라이브 기타 — 파워코드를 박마다 내리찍는다. B에서는 8분음표 전부.
    def chugs(chords, vel, offsets):
        out = []
        for bar, name in chords:
            for p in power(name, 45, 56):
                for off in offsets:
                    out.append((bar * BAR + off, 1.4, p, vel))
        return out

    gtr = _merge(
        chugs(ch[4:28], 78, (0, 3)),
        chugs(ch[28:36], 88, (0, 1, 2, 3, 4, 5)),        # B — 전부 찍는다
        chugs(ch[36:60], 82, (0, 2, 3, 5)),
        chugs(ch[60:64], 74, (0, 3)),
    )

    ghost = _merge(
        [(s, d, p, 36) for (s, d, p) in place(ORBIT_FRAGMENT, 0, octaves=1, **m)],
        [(s, d, p, 32) for (s, d, p) in place(ORBIT_FRAGMENT, 60, octaves=1, **m)],
    )

    # 오버드라이브 베이스 — 근음을 8분음표로 민다
    low = []
    for bar, name in ch:
        r = bass_pitch(name, 33, 44)
        for off in range(6):
            low.append((bar * BAR + off, 1, r, 84 if off in (0, 3) else 70))

    # 백비트 — 킥 1박, 스네어 2박, 오픈햇이 뒷박을 연다
    rock = [(0, "kick", 100), (2, "kick", 62), (3, "snare", 92),
            (1, "hat", 34), (4, "hat", 34), (5, "ohat", 46)]
    drums = _merge(
        beat(0, 4, [(0, "kick", 92), (3, "kick", 76), (5, "ohat", 44)]),
        [(3 * BAR + 5, 1, "crash", 72)],
        beat(4, 24, rock),
        beat(28, 8, rock + [(1, "kick", 58), (4, "kick", 58)]),   # B — 더블킥 흉내
        [(28 * BAR, 1, "crash", 78)],
        beat(36, 24, rock),
        [(36 * BAR, 1, "crash", 70)],
        beat(60, 4, rock + [(2, "snare", 64), (5, "snare", 72)]),
    )

    return {
        "name": "boss_p2", "bpm": 138, "bars": 64, "reverb": (1.2, 0.18),
        "parts": [
            _part("lead", chip, pan=-0.08, send=0.16),
            _part("guitar", gtr, pan=0.14, send=0.14),
            _part("musicbox", ghost, pan=0.28, send=0.50),
            _part("ebass", low, pan=0.0, send=0.06),
            _part("drums", drums, pan=0.05, send=0.10),
        ],
    }


# =========================================================================
# 4-4. field — G 단조, **4/4**, 4분 = 126, 48마디 (91초)
#
#    감독이 로직으로 쓴 플루트 스케치가 원본이다 (midi~/field_sketch.mid).
#    「여백」 밖의 첫 멜로디 — 설계 문서 머리말이 "그 외 층"을 처음부터
#    범위 밖으로 두었고, 부록에 등록했다. 4/4라서 이 곡만 meter 필드를 쓴다.
#
#    초원에서 몬스터를 사냥하는 곡이다. 필드 BGM의 관례대로 한 바퀴를
#    벌스-후렴 두 회전으로 짠다 — 인트로 4 · A 8 · A' 8 · B 8 · A'' 8 ·
#    B' 8 · 꼬리 4. 벌스(A)는 스케치 그대로 — 단조 아르페지오 모티프
#    (1·♭7·1·♭3·5)가 Dm → Cm → B♭m으로 온음씩 내려앉고 G에 앉는다.
#    후렴(B)은 그 모티프를 **장조로 뒤집는다**(1·7·1·3·5) — 같은 손모양이
#    B♭ 장조 I-V-vi-IV 위를 올라간다. 내려앉던 노래가 후렴에서 일어선다.
#    벌스는 뜯는 현이, 후렴은 칩튠이 분다 — 경계하다, 달려든다.
# =========================================================================

BAR8 = 8  # 4/4 한 마디 = 8분음표 8개


def _f(spec):
    return [(s, d, pitch(n)) for (n, s, d) in spec]


# 스케치 전문 (8분음표 단위, 0.5 = 16분음표)
FIELD_MEL = _f([
    # 마디 1-2 · Dm → Cm. 모티프와 그 온음 아래 답
    ("D6", 0, 1), ("C6", 1, 1), ("D6", 2, 0.5), ("F6", 2.5, 0.5), ("A6", 3, 3),
    ("C6", 8, 1), ("Bb5", 9, 1), ("C6", 10, 0.5), ("Eb6", 10.5, 0.5), ("G6", 11, 2.5),
    # 마디 3-4 · B♭m까지 내려앉고, 하행선이 G에 닿는다
    ("Bb5", 16, 1), ("Ab5", 17, 1), ("Bb5", 18, 0.5), ("Db6", 18.5, 0.5),
    ("F6", 19, 1), ("F6", 21, 1), ("Eb6", 22, 1),
    ("D6", 24, 1), ("Bb5", 25, 1), ("G5", 27, 5),
    # 마디 5-6 · 종지구 — 도를 맴돌다 올라섰다가 걸어 내려온다
    ("C6", 32, 1), ("B5", 33, 1), ("C6", 34, 1), ("D6", 35, 1),
    ("Eb6", 36, 2), ("Eb6", 38, 2),
    ("F6", 40, 1), ("Eb6", 41, 1), ("D6", 44, 2), ("Bb5", 46, 2),
    ("G5", 48, 3),                                 # 착지 — 스케치의 1박을 3으로
])

# 진술 꼬리(8마디째)에서 뮤직박스가 첫 모티프를 되받는다 — 다음 바퀴의 예고
FIELD_ECHO = _f([
    ("D6", 56, 1), ("C6", 57, 1), ("D6", 58, 0.5), ("F6", 58.5, 0.5), ("A6", 59, 3),
])

# 후렴 (8마디) — 벌스 모티프의 **장조 반전.** 같은 손모양(축–아래이웃–축–3도–3도)이
# 단조에서 1·♭7·1·♭3·5로 내려앉았다면, 여기서는 1·7·1·3·5로 일어선다.
# B♭ 장조 I–V–vi–IV 위에서 마디마다 그 화음의 음만 짚는다. 4마디째만
# 아르페지오 대신 음계로 걸어 올라간다 — 뛰기 전에 한 번 딛는 것.
FIELD_LIFT = _f([
    ("Bb5", 0, 1), ("A5", 1, 1), ("Bb5", 2, 0.5), ("D6", 2.5, 0.5), ("F6", 3, 3),
    ("A5", 8, 1), ("G5", 9, 1), ("A5", 10, 0.5), ("C6", 10.5, 0.5), ("F6", 11, 3),
    ("Bb5", 16, 1), ("A5", 17, 1), ("Bb5", 18, 0.5), ("D6", 18.5, 0.5), ("G6", 19, 3),
    ("Bb5", 24, 1), ("C6", 25, 1), ("D6", 26, 1), ("Eb6", 27, 1),
    ("D6", 28, 1), ("Bb5", 29, 1), ("G5", 30, 2),
    ("Bb5", 32, 1), ("A5", 33, 1), ("Bb5", 34, 0.5), ("D6", 34.5, 0.5), ("F6", 35, 3),
    ("A5", 40, 1), ("G5", 41, 1), ("A5", 42, 0.5), ("C6", 42.5, 0.5), ("F6", 43, 3),
    ("Bb5", 48, 1), ("A5", 49, 1), ("Bb5", 50, 0.5), ("D6", 50.5, 0.5), ("G6", 51, 3),
    ("F6", 56, 1), ("D6", 57, 1), ("C6", 58, 1), ("A5", 59, 3),   # 내려와 손을 넘긴다
])


def field():
    # 화음은 멜로디가 정한다 (문서 6절) — 벌스는 각 마디의 정점음이 그 화음의
    # 5음이고, 후렴은 모티프가 그 화음의 아르페지오다.
    # 벌스 8마디째 Dm은 꼬리 되받기(레–도–레–파–라)가 그대로 화음이 된 것.
    verse = ["Dm", "Cm", "Bbm", "Gm", "Cm", "Bb", "Gm", "Dm"]
    lift = ["Bb", "F", "Gm", "Eb", "Bb", "F", "Gm", "F"]        # I V vi IV — 사냥의 추진력
    prog = (["Gm", "Bb", "F", "Gm"] + verse + verse + lift + verse + lift
            + ["Gm", "Bb", "F", "F"])                            # 꼬리 F(V) → 루프 Gm
    ch = [(i, prog[i]) for i in range(48)]

    # 인트로4 · A8 · A'8 · B8 · A''8 · B'8 · 꼬리4.
    # 벌스는 뜯는 현이 한 옥타브 아래에서 분다 — 스케치의 플루트 음역(G5-A6)은
    # 관악 흉내가 되어 9절이 막는다. 후렴에서는 짧은 코드 스탭으로 물러난다.
    mel = arc([
        (place(FIELD_MEL, 4, octaves=-1, bar=BAR8), 82),
        (place(FIELD_MEL, 12, octaves=-1, bar=BAR8), 90),
        (place(FIELD_MEL, 28, octaves=-1, bar=BAR8), 96),
    ], accent=10, bar=BAR8)
    comp = _merge(stabs(ch[20:28], 52, 62, 76, offsets=(2, 4.5, 7), bar=BAR8),
                  stabs(ch[36:44], 58, 62, 76, offsets=(2, 4.5, 7), bar=BAR8))

    # 후렴은 칩튠이 분다 — 경계하던 노래가 달려드는 노래가 된다.
    chip = arc([
        (place(FIELD_LIFT, 20, octaves=-1, bar=BAR8), 92),
        (place(FIELD_LIFT, 36, octaves=-1, bar=BAR8), 102),
    ], accent=10, bar=BAR8)

    # 뮤직박스 — 인트로에서 모티프를 예고하고, A''는 스케치 원음역으로 유니즌,
    # B'는 후렴 위에서 한 옥타브 위 반짝임. 꼬리 되받기가 루프를 넘긴다.
    box = _merge(
        [(s, d, p, 48) for (s, d, p) in place(FIELD_ECHO, 2, bar=BAR8)],
        [(s, d, p, 50) for (s, d, p) in place(FIELD_ECHO, 19, bar=BAR8)],
        [(s, d, p, 56) for (s, d, p) in place(FIELD_MEL, 28, bar=BAR8)],
        [(s, d, p, 52) for (s, d, p) in place(FIELD_ECHO, 35, bar=BAR8)],
        [(s, d, p, 54) for (s, d, p) in place(FIELD_LIFT, 36, bar=BAR8)],
        [(s, d, p, 46) for (s, d, p) in place(FIELD_ECHO, 46, bar=BAR8)],
    )

    pads = _merge(sustain(ch[0:4], 44, 57, 71, bar=BAR8),
                  sustain(ch[4:12], 42, 57, 71, bar=BAR8),
                  sustain(ch[12:20], 48, 57, 71, bar=BAR8),
                  sustain(ch[20:28], 56, 62, 76, bar=BAR8),   # 후렴은 한 층 위 — 트인 하늘
                  sustain(ch[28:36], 52, 57, 71, bar=BAR8),
                  sustain(ch[36:44], 58, 62, 76, bar=BAR8),
                  sustain(ch[44:48], 40, 57, 71, bar=BAR8))

    # 벌스는 뛰는 베이스(근·근·5·근 — 사냥 걸음), 후렴은 8분음표로 민다.
    def bounce(chords, vel):
        out = []
        for b, name in chords:
            r = bass_pitch(name, 36, 47)
            for off, p, v in ((0, r, vel + 8), (3, r, vel - 6),
                              (4, r + 7, vel), (6, r, vel - 2)):
                out.append((b * BAR8 + off, 1, p, v))
        return out

    def pump(chords, vel):
        return [(b * BAR8 + off, 1, bass_pitch(name, 36, 47),
                 vel + (10 if off in (0, 4) else 0))
                for b, name in chords for off in range(8)]

    low = _merge(bounce(ch[0:4], 54), bounce(ch[4:12], 58), bounce(ch[12:20], 64),
                 pump(ch[20:28], 68), bounce(ch[28:36], 68), pump(ch[36:44], 72),
                 walk(ch[44:48], 50, offsets=(0,), bar=BAR8))

    # 4/4 백비트 — 스네어는 2·4박(8분음표 2·6). 벌스는 림, 후렴은 스네어.
    # 섹션 경계마다 스네어 필과 크래시가 문을 연다. 꼬리는 걷어낸다.
    step = [(o, "shaker", 38 if o in (0, 4) else 26) for o in range(8)]
    trot = [(0, "kick", 78), (4, "kick", 64), (2, "rim", 56), (6, "rim", 60)]
    gallop = [(0, "kick", 88), (3, "kick", 58), (4, "kick", 72),
              (2, "snare", 80), (6, "snare", 84),
              (1, "hat", 30), (5, "hat", 30), (7, "ohat", 44)]
    fill = [(4, "snare", 58), (5, "snare", 64), (6, "snare", 72), (7, "snare", 80)]
    drums = _merge(
        beat(0, 3, trot + step, bar=BAR8),
        beat(3, 1, [(0, "kick", 78), (2, "rim", 52)] + fill, bar=BAR8),
        beat(4, 8, trot + step, bar=BAR8),
        beat(12, 7, trot + step + [(1, "hat", 26), (5, "hat", 26)], bar=BAR8),
        beat(19, 1, trot[:2] + fill, bar=BAR8),
        [(20 * BAR8, 1, "crash", 72)],
        beat(20, 8, gallop + step, bar=BAR8),
        [(28 * BAR8, 1, "crash", 66)],
        beat(28, 7, trot + step + [(1, "hat", 28), (5, "hat", 28),
                                   (7, "ohat", 40)], bar=BAR8),
        beat(35, 1, trot[:2] + fill, bar=BAR8),
        [(36 * BAR8, 1, "crash", 76)],
        beat(36, 8, gallop + step + [(5.5, "kick", 50)], bar=BAR8),
        beat(44, 4, [(0, "kick", 56)]
             + [(o, "shaker", 32 if o in (0, 4) else 20) for o in range(8)],
             bar=BAR8),
    )

    return {
        "name": "field", "bpm": 126, "bars": 48, "meter": (4, 4),
        "reverb": (1.7, 0.24),
        "parts": [
            _part("pluck", mel + comp, pan=-0.10, send=0.28),
            _part("lead", chip, pan=0.08, send=0.20),
            _part("musicbox", box, pan=0.24, send=0.36),
            _part("pad", pads, pan=0.0, send=0.28),
            _part("bass", low, pan=0.0, send=0.10),
            _part("drums", drums, pan=0.05, send=0.14),
        ],
    }


# =========================================================================
# 5. 전주 — 「여백」이 나오기 전에 깔리는 도입부
#
#    새 모티프는 없다 (5절). 재료는 전부 테마의 뼈대다:
#      1) 되뇜의 축 네 개(솔 → 시♭ → 라♭ → 도)를 슬로모션으로 하나씩 놓는다.
#         테마를 아는 귀에는 "그 노래가 오고 있다"로 들린다
#      2) 상행을 **다섯 걸음까지만** 인용하고 멈춘다. 여섯 걸음째(정점)는
#         본 곡이 완성한다 — 전주는 질문이고 곡이 대답이다
#      3) 마디 7에 ♭VII(D♭) — 그 자리는 전주에도 있다
#      4) B♭(V) 반종지로 끝난다. 그래서 본 곡의 E♭ 첫 음이 '도착'으로 들린다
# =========================================================================

def _intro_parts(minor=False, stretch=1):
    """전주 8×stretch 마디의 파트들. stretch=2면 빠른 템포용(보스)으로 늘린다."""
    z = stretch
    P = ["Eb", "Eb", "Ab", "Ab", "Eb", "Ab", "Db", "Bb"]
    if minor:
        P = ["Ebm", "Ebm", "Abm", "Abm", "Ebm", "Abm", "Db", "Bbm"]
    ch = [(b, P[b // z]) for b in range(8 * z)]

    mm = dict(minor=minor)

    # 1) 축 네 개 — 한 블록(z마디)에 하나씩, 길게
    from theme import pitch as _p
    axes = ["G4", "Bb4", "Ab4", "C5"]
    bones = [(i * z * BAR, 3 * z, _p(n)) for i, n in enumerate(axes)]
    if minor:
        bones = to_minor(bones)
    bones = [(s, d, pt, 56 + i * 4) for i, (s, d, pt) in enumerate(bones)]

    # 2) 상행 다섯 걸음 (블록 4~5) — 정점 직전에 멈춘다
    steps = ["Bb4", "C5", "D5", "Eb5", "F5"]
    quote = [(4 * z * BAR + i * 2 * z, 2 * z, _p(n)) for i, n in enumerate(steps)]
    if minor:
        quote = to_minor(quote)
    quote = [(s, d, pt, 58 + i * 5) for i, (s, d, pt) in enumerate(quote)]

    # 3) ♭VII 자리 (블록 6) — 라♭ 하나가 길게 아리다
    ache = [(6 * z * BAR, 3 * z, _p("Ab4"), 62)]

    pads = sustain(ch, 44, 60, 74) if not minor else sustain(ch, 46, 57, 71)
    low = walk(ch[2 * z:], 52 if not minor else 62, offsets=(0,))

    parts = [
        _part("musicbox", bones + ache, pan=-0.05, send=0.45),
        _part("pluck", quote, pan=0.14, send=0.34),
        _part("pad", pads, pan=0.0, send=0.34),
        _part("bass", low, pan=0.0, send=0.10),
    ]
    if minor:
        # 보스용 — 심장박동 킥이 뒤에서 다가온다
        parts.append(_part("drums", _merge(
            beat(4 * z, 2 * z, [(0, "kick", 46)]),
            beat(6 * z, 2 * z, [(0, "kick", 58), (3, "kick", 40)]),
        ), pan=0.0, send=0.10))
    return parts, 8 * z


def _offset_parts(parts, bars):
    """파트들의 모든 음을 뒤로 민다 — 전주 뒤에 본 곡을 붙일 때."""
    return [{**p, "notes": [(s + bars * BAR, d, pt, v) for (s, d, pt, v) in p["notes"]]}
            for p in parts]


def intro():
    """독립 전주. 루프하지 않는다 — 한 번 흐르고 본 곡으로 넘어가는 용도."""
    parts, bars = _intro_parts(minor=False, stretch=1)
    return {"name": "intro", "bpm": 60, "bars": bars,
            "reverb": (2.4, 0.34), "loop": False, "parts": parts}


def title_intro():
    """전주 8마디 + 타이틀 16마디. 한 파일로 들어보는 판."""
    pre, bars = _intro_parts(minor=False, stretch=1)
    t = title()
    return {"name": "title_intro", "bpm": 60, "bars": bars + t["bars"],
            "reverb": t["reverb"], "loop": False,
            "parts": pre + _offset_parts(t["parts"], bars)}


def boss_intro():
    """단조 전주 16마디(빠른 템포라 두 배로 늘림) + 보스 64마디.
    전주가 끝나면 보스의 원래 4마디 드럼 도입이 다리가 된다."""
    pre, bars = _intro_parts(minor=True, stretch=2)
    b = boss()
    return {"name": "boss_intro", "bpm": 138, "bars": bars + b["bars"],
            "reverb": b["reverb"], "loop": False,
            "parts": pre + _offset_parts(b["parts"], bars)}


ALL = [title, stage1, dungeon, boss,
       lambda: dungeon(major=True), lambda: boss(major=True),
       boss_p1, boss_p2, field,
       intro, title_intro, boss_intro]
