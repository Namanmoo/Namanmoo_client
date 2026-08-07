# 오디오 출처와 라이선스

받아온 파일은 **받은 그 자리에서 여기 한 줄 추가한다.** 나중에 몰아서 되짚으려 하면
어느 파일이 어디서 왔는지 알아낼 방법이 없다. 소리는 파일만 봐서는 출처를 알 수 없다.

## CC-BY를 쓸 때 반드시 적어야 하는 것

CC BY 4.0 3조 (a)(1)이 요구하는 항목이다. 하나라도 빠지면 라이선스 위반이다.

| 항목 | |
|---|---|
| 저작자 이름 | 지정된 이름 또는 가명 |
| 제목 | 원저작물 제목 |
| 출처 링크 | 원저작물 URL |
| 라이선스 표시 | 이름 + **전문 링크** (예: https://creativecommons.org/licenses/by/4.0/) |
| **변경 사실** | 수정했다면 그 사실을 밝혀야 한다 |

마지막 항목을 특히 조심해라. 이 프로젝트는 원본을 `unfixed~/`에 두고 노멀라이징·클리핑·
트리밍을 거쳐 내보내므로 **가져온 소리는 사실상 전부 "수정함"이다.** 자르기만 해도 변경이다.

Freesound 같은 곳은 "Get attribution text" 버튼으로 완성된 문구를 준다. 그걸 그대로
아래 표의 `표기 문구`에 붙여 넣고, 수정 사실만 덧붙이면 된다.

CC0 / Public Domain은 표기 의무가 없다. 그래도 표에는 적어라 — 나중에 "이건 표기해야
하나?"를 다시 확인하는 비용이 표 한 줄보다 크다.

## 기록

| 파일 | 원제 | 저작자 | 출처 | 라이선스 | 수정 | 표기 필요 |
|---|---|---|---|---|---|---|
| `Weapon/any_any_liquid.ogg` | Water, Pouring, A.wav | InspectorJ | https://freesound.org/people/InspectorJ/sounds/421184/ | CC BY 4.0 | 모노 변환, 16bit, 트리밍, 노멀라이즈 -1dBFS | ✅ |
| `Weapon/swing_any_any.ogg` | Whoosh | qubodup | https://freesound.org/people/qubodup/sounds/60013/ | CC0 1.0 | 모노 변환, 16bit, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Weapon/swing_light_any.ogg` | Fast whoosh | alanmcki | https://freesound.org/people/alanmcki/sounds/461017/ | CC BY 4.0 | 모노 변환, 96k→22050Hz, 16bit, 트리밍, 노멀라이즈 -1dBFS | ✅ |
| `Weapon/swing_heavy_any.ogg` | long wispy woosh1.wav | newagesoup | https://freesound.org/people/newagesoup/sounds/377830/ | CC BY 4.0 | 모노 변환, 48k→22050Hz, 16bit, 1초 이내로 트리밍, 노멀라이즈 -1dBFS | ✅ |
| `Weapon/swing_light_metal.ogg` | quick woosh | florianreichelt | https://freesound.org/people/florianreichelt/sounds/683101/ | CC0 1.0 | 모노 변환, 22050Hz, 16bit, 1초 이내로 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Weapon/swing_medium_any.ogg` | Whoosh_Swish_03.wav | www.bonson.ca | https://freesound.org/people/www.bonson.ca/sounds/12658/ | **CC BY 3.0** | 22050Hz, 트리밍, 노멀라이즈 -1dBFS | ✅ |
| `Weapon/thrust_any_any.ogg` | whip03.wav | snowflakes | https://freesound.org/people/snowflakes/sounds/72191/ | CC0 1.0 | 모노 변환, 22050Hz, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Weapon/thrust_medium_any.ogg` | whip02.wav | snowflakes | https://freesound.org/people/snowflakes/sounds/72190/ | CC0 1.0 | 모노 변환, 22050Hz, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Weapon/thrust_light_metal.ogg` | Nasty Knife Stab.wav | Aris621 | https://freesound.org/people/Aris621/sounds/435238/ | CC BY 4.0 | 모노 변환, 22050Hz, 16bit, 트리밍, 노멀라이즈 -1dBFS | ✅ |
| `Weapon/throw_any_any.ogg` | Throwing / Whip Effect | denao270 | https://freesound.org/people/denao270/sounds/346373/ | CC0 1.0 | 모노 변환, 22050Hz, 16bit, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Weapon/thrust_light_any.ogg` | Throw/Swipe | mrickey13 | https://freesound.org/people/mrickey13/sounds/515625/ | CC0 1.0 | 22050Hz, 32bit float→16bit, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Weapon/any_light_liquid.ogg` | Water splash | nilbul | https://freesound.org/people/nilbul/sounds/404829/ | CC0 1.0 | 모노 변환, 22050Hz, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Enemy/die_metal.ogg` | Glitching 3 | G.M_Isaac | https://freesound.org/people/G.M_Isaac/sounds/864985/ | CC0 1.0 | **GSM→PCM 디코드**, 22050Hz, 16bit, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Effect/any_shock_start.ogg` | JacobsLadderSingle2.flac | Halleck | https://freesound.org/people/Halleck/sounds/19487/ | CC BY 4.0 | 모노 변환, 48k→22050Hz, 트리밍, 끝에 페이드, 노멀라이즈 -1dBFS | ✅ |
| `Effect/zone_burn_start.ogg` | Explosion_01.wav | tommccann | https://freesound.org/people/tommccann/sounds/235968/ | CC0 1.0 | 모노 변환, 22050Hz, 1초 남짓으로 트리밍, 끝에 페이드, 노멀라이즈 -1dBFS | ❌ |
| `Effect/zone_burn_loop.ogg` | Torch.wav | DanielVega | https://freesound.org/people/DanielVega/sounds/479338/ | CC0 1.0 | 모노 변환, 22050Hz, 32bit float→16bit, 이음매 크로스페이드로 루프화, 노멀라이즈 -1dBFS | ❌ |
| `Impact/hit_any_any_any.ogg` | punch.wav | Ekokubza123 | https://freesound.org/people/Ekokubza123/sounds/104183/ | CC0 1.0 | 모노 변환, 22050Hz, 앞 무음 완전 제거, 1초 이내 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Impact/hit_metal_light_metal.ogg` | Anvil Hit 1 | michorvath | https://freesound.org/people/michorvath/sounds/270589/ | CC0 1.0 | 22050Hz, 트리밍, 노멀라이즈 -1dBFS | ❌ |
| `Bgm/title.ogg` | 여백 (Blank Margin) | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/stage1.ogg` | 여백 — 걷는 속도 | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/dungeon.ogg` | 여백 — 나란한조 | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/boss.ogg` | 여백 — 평행단조 | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/dungeon_major.ogg` `Bgm/boss_major.ogg` | 여백 — 장조판 | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/boss_p1.ogg` `Bgm/boss_p2.ogg` | 여백 — 보스 1·2페이즈 (재즈/락) | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/intro.ogg` `Bgm/title_intro.ogg` `Bgm/boss_intro.ogg` | 여백 — 전주 및 전주 결합판 | Namanmoo 프로젝트 | 자작 — `tools/bgm/` | **프로젝트 소유** | 전곡 합성 생성 | ❌ |
| `Bgm/field.ogg` | field — 필드 (감독 스케치 멜로디) | Namanmoo 프로젝트 | 자작 — 감독 멜로디 + `tools/bgm/` 편곡 | **프로젝트 소유** | 전곡 합성 생성 | ❌ |

> **BGM은 전부 받아온 소재가 없다.** 멜로디·편곡·음색 합성까지 전부 이 저장소 안에서
> 만들었고([`tools/bgm/`](../../tools/bgm/), 설계는 [`Bgm/MUSIC_DESIGN.md`](Bgm/MUSIC_DESIGN.md)),
> 샘플·사운드폰트·루프팩을 일절 쓰지 않았다. 그래서 **표기 의무가 없고, 이 4곡 때문에
> 크레딧 화면이 필요해지지도 않는다.** `Bgm/midi~/`의 MIDI가 원본이다.

**아직 내보내지 않은 것**

> **임시 상태 (2026-08-07 갱신):** `Weapon/`은 손질한 `*_fixed` 판(wav, m4a는 wav로
> 변환)으로 교체했다. `Impact/`·`Enemy/`·`Effect/`는 아직 원본을 가공 없이 복사해 둔
> 상태다 (`die_metal`만 GSM→PCM 디코드). 정식 내보내기를 하면 교체하고 이 표에서 지운다.

원본만 `unfixed~/`에 있고 게임용 파일이 아직 없는 항목이다. 내보내면 이 목록에서 지운다.
원본 파일명은 내보낼 이름 + `_unfixed`이므로 여기 이름만 보면 원본을 찾을 수 있다.

- `Weapon/any_any_liquid.ogg` — 2.83초 / 44.1kHz / 스테레오 / 24bit wav
- `Weapon/any_light_liquid.ogg` — 0.87초 / 44.1kHz / 스테레오 / 16bit wav
- `Weapon/swing_any_any.ogg` — 0.43초 / 44.1kHz / 스테레오 / 24bit flac
- `Weapon/swing_light_any.ogg` — 0.95초 / **96kHz** / 스테레오 / 24bit wav
- `Weapon/swing_heavy_any.ogg` — **1.88초** / **48kHz** / 스테레오 / 24bit wav (1초 이내로 잘라야 한다)
- `Weapon/swing_light_metal.ogg` — **2.05초** / 48kHz / 스테레오 / mp3 (1초 이내로 잘라야 한다)
- `Weapon/swing_medium_any.ogg` — 0.45초 / 44.1kHz / **모노** / 16bit wav
- `Weapon/thrust_any_any.ogg` — 0.42초 / 44.1kHz / 스테레오 / 16bit wav
- `Weapon/thrust_light_any.ogg` — 0.60초 / 44.1kHz / 모노 / **32bit float** wav
- `Weapon/thrust_medium_any.ogg` — 0.67초 / 44.1kHz / 스테레오 / 16bit wav
- `Weapon/thrust_light_metal.ogg` — 0.53초 / 44.1kHz / 스테레오 / 24bit wav
- `Weapon/throw_any_any.ogg` — 0.33초 / 44.1kHz / 스테레오 / 24bit wav
- `Impact/hit_any_any_any.ogg` — 1.19초 / 44.1kHz / 스테레오 / 16bit wav (**타격음이므로 앞 무음을 완전히 없애 트랜지언트가 0초에 오게 한다**)
- `Impact/hit_metal_light_metal.ogg` — 1.27초 / 44.1kHz / **모노 / 16bit** wav (변환 없이 리샘플·트리밍만)
- `Enemy/die_metal.ogg` — **10.94초** / 44.1kHz / 모노 / **GSM 6.10 코덱** wav (PCM으로 먼저 풀어야 한다)
- `Effect/any_shock_start.ogg` — 1.33초 / **48kHz** / 스테레오 / 16bit flac
- `Effect/zone_burn_start.ogg` — **7.80초** / 44.1kHz / 스테레오 / 16bit wav (1초 남짓으로 잘라야 한다)
- `Effect/zone_burn_loop.ogg` — **4.17초** / 44.1kHz / 스테레오 / **32bit float** wav (루프용, 1~3초로 줄이고 이음매를 물려야 한다)

## 게임 화면에 넣을 문구

CC-BY는 **이용자가 볼 수 있는 곳**에 표기해야 한다. 리포지터리의 이 파일만으로는 부족하다.
빌드된 게임 안에 크레딧 화면이 있어야 한다.

아래를 그 화면에 그대로 쓴다. 표에 줄이 늘 때마다 여기도 같이 갱신한다.

```
사운드

"Water, Pouring, A.wav" by InspectorJ
  https://freesound.org/people/InspectorJ/sounds/421184/
  CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/) — 편집함

"Fast whoosh" by alanmcki
  https://freesound.org/people/alanmcki/sounds/461017/
  CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/) — 편집함

"Nasty Knife Stab.wav" by Aris621
  https://freesound.org/people/Aris621/sounds/435238/
  CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/) — 편집함

"JacobsLadderSingle2.flac" by Halleck
  https://freesound.org/people/Halleck/sounds/19487/
  CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/) — 편집함

"long wispy woosh1.wav" by newagesoup
  https://freesound.org/people/newagesoup/sounds/377830/
  CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/) — 편집함

"Whoosh_Swish_03.wav" by www.bonson.ca
  https://freesound.org/people/www.bonson.ca/sounds/12658/
  CC BY 3.0 (https://creativecommons.org/licenses/by/3.0/) — 편집함
```

## 받기 전에 확인할 것

- **무료 계정으로 받은 파일도 상업적 배포가 되는가** — 사이트마다 다르다
- **게임 빌드에 파일을 내장해 배포해도 되는가** — WebGL은 오디오가 배포물에 그대로 실린다.
  "재배포 금지" 조항이 있는 곳은 쓸 수 없다
- **CC BY-NC / ND가 아닌가** — NC는 상업적 이용 금지, ND는 변경 금지다.
  이 프로젝트는 파일을 무조건 편집하므로 **ND는 조건을 지킬 수 없다**
