# 타격음 파일명 규칙

무기가 적에게 닿는 순간 나는 소리. `Assets/Audio/Weapon/`의 휘두르는 소리와는 **규칙이 다르다.**

휘두르는 소리는 공기를 가르는 것이라 대상과 무관하다. 반면 타격음은 **부딪히는 두 쪽이
만나서** 정해진다. 같은 금속이라도 슬라임을 치면 철푸덕하고 돌을 치면 챙 하고 튕긴다.

```
hit_{무기재질}_{무기무게}_{대상재질}.ogg

hit_metal_heavy_shell.ogg    무거운 금속으로 갑각을 침 — 쿵
hit_metal_light_shell.ogg    가벼운 금속으로 갑각을 침 — 챙
hit_any_any_slime.ogg        무엇으로 치든 슬라임은 철푸덕
hit_any_any_any.ogg          최소한의 타격음
```

무기 쪽은 축이 둘, 대상 쪽은 하나다. **때리는 쪽은 무엇으로 얼마나 세게 치느냐가 모두
소리를 바꾸지만, 맞는 쪽은 무엇이냐만 중요하다.**

## 무게가 왜 필요한가

같은 금속이라도 무게에 따라 부딪히는 소리가 달라진다.

| | 소리 |
|---|---|
| 무거운 금속 | 둔탁하게 **쿵** — 저역이 실리고 여운이 길다 |
| 가벼운 금속 | 날카롭게 **챙** — 고역이 서고 짧게 끊긴다 |

재질만으로 가르면 손도끼와 대형 망치가 같은 소리를 낸다. 무기가 플레이어의 그림으로
만들어져 스탯 편차가 큰 이 게임에서는 그 차이가 눈에 띈다.

무게는 무기 스탯에서 계산한다. 규칙은 `Assets/Audio/Weapon/NAMING.md`의
"`weight`는 스탯에서 계산한다"와 **완전히 같다.**

```
무게값 = damage × attackInterval    →  3 미만 light / 10 미만 medium / 10 이상 heavy
```

## 폴백 순서

위에서부터 훑어 처음 있는 것을 쓴다.

```
1. hit_{재질}_{무게}_{대상}     hit_metal_heavy_shell
2. hit_{재질}_any_{대상}        hit_metal_any_shell
3. hit_any_{무게}_{대상}        hit_any_heavy_shell
4. hit_any_any_{대상}           hit_any_any_shell      ← 대부분 여기서 끝난다
5. hit_{재질}_{무게}_any        hit_metal_heavy_any
6. hit_{재질}_any_any           hit_metal_any_any
7. hit_any_{무게}_any           hit_any_heavy_any
8. hit_any_any_any
9. default
```

**대상을 가장 오래 붙들고 있다가 마지막에 버린다.** 대상 재질이 이 소리를 지배하는
축이기 때문이다. 무기 쪽에서는 무게를 먼저 버리고 재질을 나중에 버린다 —
재질이 무게보다 소리를 더 특징짓는다.

## 대상 재질이 지배한다

**슬라임은 무엇으로 쳐도 철푸덕한다.** 무기 쪽 축이 소리를 실제로 바꾸는 경우는
생각보다 드물다. 그래서 대부분 4번(`hit_any_any_{대상}`)에서 끝나고, 파일도
**대상 재질 수만큼만** 있으면 굴러간다.

무기 재질·무게까지 따지는 건 **정말로 다르게 들리는 조합만**이다.

- 금속이 돌을 때리면 불꽃이 튀는 소리가 난다 → `hit_metal_any_stone`
- 무거운 것이 갑각을 부수는 소리는 가벼운 것과 다르다 → `hit_any_heavy_shell`

`hit_metal_any_any.ogg`처럼 **대상을 `any`로 둔 파일은 웬만하면 만들지 마라.**
폴백 6번에서 걸려 대상을 무시하는데, 대상이야말로 이 소리를 지배하는 축이다.

## 어휘

### 무기 재질 (첫째 칸)

`Assets/Scripts/Items/WeaponMaterial.cs`와 같다.

```
metal  wood  stone  organic  liquid  magic  cloth
```

### 무기 무게 (둘째 칸)

```
light  medium  heavy
```

### 대상 재질 (셋째 칸)

**무기 어휘와 별개로 관리한다.** 파일명에서 자리가 정해져 있으므로 섞일 일이 없고,
각자 필요한 값을 자유롭게 늘릴 수 있다. 무기에 `shell`, 적에 `cloth` 같은 어색한 값을
억지로 끼워 넣지 않아도 된다.

지금 게임에 있는 적은 둘뿐이다.

| 적 | 재질 |
|---|---|
| 게 (`KrabEnemy`) | `shell` |
| 로봇 보스 (`BossRobotController`) | `metal` |

앞으로 늘어날 만한 것: `flesh` `slime` `stone` `plant` `bone` `wood` `ghost`

## 채우는 순서

전 조합은 `7 × 3 × 대상수`지만 다 채울 일은 없다.

| 순서 | 파일 | 개수 | 효과 |
|---|---|---|---|
| 1 | `hit_any_any_any` | 1 | 타격이 있었다는 것만 알림 |
| 2 | `hit_any_any_{대상}` | 적 재질 수만큼 | **적별로 구분됨 — 체감이 가장 크다** |
| 3 | `hit_any_heavy_{대상}` / `hit_any_light_{대상}` | 필요한 것만 | 묵직함이 드러남 |
| 4 | 특별히 다른 조합 (`hit_metal_any_stone` 등) | 필요할 때 | 완성도 |

2단계까지가 실질적으로 전부다. 적이 늘어날 때마다 `hit_any_any_{새재질}` 하나씩 추가하면 된다.

## 그 밖의 규칙

`Assets/Audio/Weapon/NAMING.md`와 같다.

- `any` 와일드카드 — 같은 소리를 두 이름으로 복사하지 않는다
- 같은 조합에 여러 개를 두려면 뒤에 숫자 (`hit_any_any_shell_2.ogg`)
- 임포트 설정 (Force To Mono, 22050Hz, Decompress On Load, Vorbis)
- 원본은 `Assets/Audio/unfixed~/Impact/`에, 출처는 `Assets/Audio/LICENSES.md`에

## 아직 코드가 없다

타격음은 **때린 쪽과 맞은 쪽을 한자리에서 알아야** 재생할 수 있다.

지금 `EnemyHealth.TakeDamage(int amount)`는 누가 때렸는지 모르고, 적에게는 재질 필드가
아예 없다. 무기 재질과 무게를 넘기려면 호출하는 곳이 전부 바뀐다 —
`PlayerWeaponController`, `WeaponProjectile`, `AxeSwing`, `BossBullet`과 그 테스트들.

휘두르는 소리는 무기 하나만 보면 되므로 파급이 없다. **그쪽을 먼저 붙이는 편이 낫다.**

파일은 코드보다 먼저 넣어도 된다. 규칙만 지켜 두면 재생 코드가 붙는 날 그대로 잡힌다.
