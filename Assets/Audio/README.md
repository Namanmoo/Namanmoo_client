# 오디오

```
Assets/Audio/
├── Weapon/     무기를 휘두르는 소리         → NAMING.md
├── Impact/     무기가 적에게 닿는 소리       → NAMING.md
├── Effect/     상태이상·장판 (걸려 있는 동안) → NAMING.md
├── Enemy/      적이 스스로 내는 소리
├── UI/         버튼·화면 전환
├── Bgm/        배경음악
├── unfixed~/   손대기 전 원본 (빌드에 안 들어감) → README.md
└── LICENSES.md 출처와 라이선스 기록
```

## 공통 규칙

**파일명이 곧 등록이다.** 어떤 목록에도 손으로 적지 않는다. 폴더에 넣으면 잡히고 빼면 사라진다.

- 소문자 영어와 밑줄만 쓴다. 공백·한글·대문자를 넣지 않는다
- 확장자는 `.ogg` (BGM 포함)
- 축을 따지지 않을 때는 `any`
- 같은 자리에 여러 개를 두려면 뒤에 숫자 — `swing_any_metal_2.ogg`
- 각 폴더에 `default.ogg`를 하나 둔다. 아무것도 안 맞을 때의 안전망이다
- 어휘 밖 단어를 쓰지 마라. 오타는 조용히 폴백으로 떨어져 알아채기 어렵다

## 폴더별 이름 규칙

### Weapon — `{motion}_{weight}_{material}.ogg`

무기를 휘두를 때. 자세한 것은 `Weapon/NAMING.md`.

### Impact — `hit_{무기재질}_{무기무게}_{대상재질}.ogg`

무기가 적에게 닿을 때. 때리는 쪽은 재질과 무게 둘 다 보고, 맞는 쪽은 재질만 본다.
자세한 것은 `Impact/NAMING.md`.

### Effect — `{출처}_{효과}_{단계}.ogg`

감속·화상·장판처럼 걸려 있는 동안 계속되는 것. `zone_burn_loop.ogg`, `any_slow_start.ogg`.

**단계(`start`/`loop`/`end`)는 폴백하지 않는다** — `loop`가 없다고 `start`를 반복하면
최악으로 들린다. 없는 단계는 그냥 소리가 안 난다. 자세한 것은 `Effect/NAMING.md`.

### Enemy — `{event}_{재질}.ogg`

적이 스스로 내는 소리. 맞는 소리(`Impact/`)와 다르다 — 이쪽은 적의 목소리와 움직임이다.

```
hurt_shell.ogg      맞고 내는 소리
die_metal.ogg       죽을 때
attack_shell.ogg    공격 동작
spot_any.ogg        플레이어를 발견
```

| `event` | |
|---|---|
| `hurt` | 피격 반응 |
| `die` | 사망 |
| `attack` | 공격 |
| `spot` | 발견·경계 |

재질 어휘는 `Impact/NAMING.md`의 대상 재질과 같다 (`shell` `metal` 등).

### UI — `{대상}_{동작}.ogg`

```
button_click.ogg
button_hover.ogg
slot_select.ogg     핫바 칸 전환
forge_success.ogg   무기 만들기 성공
forge_fail.ogg
```

조합 규칙이 없다. 화면에 있는 것을 그대로 이름으로 쓴다. UI는 종류가 적고 늘어나는
속도도 느려서 축으로 쪼갤 이유가 없다.

### Bgm — `{장면}.ogg`

```
title.ogg
forge.ogg
dungeon.ogg
boss.ogg
```

## 임포트 설정 — 효과음과 BGM이 정반대다

같은 설정을 일괄 적용하면 사고가 난다. **BGM에 `Decompress On Load`가 걸리면 수십 MB가
메모리에 올라간다.**

| | 효과음 (Weapon/Impact/Enemy/UI) | BGM |
|---|---|---|
| Load Type | Decompress On Load | **Streaming** |
| Force To Mono | 켠다 | 끈다 |
| Sample Rate | Override → 22050Hz | 44100Hz |
| Compression | Vorbis | Vorbis |

효과음은 짧고 많다. 개수보다 설정이 용량을 좌우한다 — 300개 기준으로 26MB와 1.2MB로 갈린다.

## 파일을 넣을 때

1. 받아온 원본을 `unfixed~/{폴더}/`에 둔다 (내보낼 이름 + `_unfixed`)
2. `LICENSES.md`에 **그 자리에서** 한 줄 적는다. 나중에 몰아서 하면 출처를 못 찾는다
3. 노멀라이징·트리밍을 거쳐 `.ogg`로 내보낸다 (기준은 `unfixed~/README.md`)
4. `.ogg.meta`가 같이 커밋되는지 확인한다. 여러 개를 한 번에 넣으면 빠뜨리기 쉽다

## 아직 재생 코드가 없다

지금은 규칙과 폴더만 있다. 파일은 코드보다 먼저 넣어도 된다 — **규칙만 지켜 두면
재생 코드가 붙는 날 그대로 잡힌다.**

CC-BY 소리를 쓴다면 게임 안에 크레딧 화면이 있어야 배포가 적법하다.
`docs/크레딧-화면.md` 참고.
