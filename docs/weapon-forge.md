# 무기 만들기 (Weapon Forge)

`게임 시작 → 무기 만들기 → Stage1` 흐름의 가운데 화면. 플레이어가 무기를 직접 그리고,
"추가 설정" 텍스트를 붙이면 AI가 둘을 보고 **스탯**을 정한다. 무기 그림은 **슬라이더로
AI 개입 단계를 골라** 만든다.

| 단계 | 그림 | 만드는 곳 |
| --- | --- | --- |
| 0. 그대로 | 그린 그림 그대로 | 클라이언트 (이미지 호출 없음) |
| 1. 조금 멋있게 | 형태 유지, 선·색만 정리 | 백엔드 → Gemini 이미지 모델 |
| 2. 완전 멋있게 | 컨셉만 살린 새 무기 아트 | 백엔드 → Gemini 이미지 모델 |

**고른 단계만 생성한다** — 이미지 API 호출은 최대 1회, 0단계는 0회다. 생성 후 결과
확인 화면에서 "이걸로 하기" 또는 "다시 그리기"를 고른다.

확정한 무기는 인벤토리 **3번 칸**에 들어간다(검·도끼는 그대로). 숫자 3을 눌러 장착하면
그 그림이 발사체가 되고, AI가 정한 공격력·연사·탄속·사거리가 적용된다.

## 그리기 도구

목업 도구바의 연필·크레용·지우개·되돌리기·다시하기와 4색(검·빨·파·초), 그리고 도구바
아래에 **확장 팔레트 12색**을 더 얹었다(AIGame 그리기 도구와 같은 구성). 지금 고른 도구와
색은 아이콘 아래 **주황 밑줄**로 표시된다 — 목업 그림을 덮지 않기 위해 밑줄을 골랐다.

색 목록은 [WeaponForgeController.PaletteColors](../Assets/Scripts/Forge/WeaponForgeController.cs)
한 곳에만 있고 씬 빌더가 그 배열로 버튼을 만든다. 색을 늘리려면 거기만 고치면 된다.

## 실행

```bash
# 1) 백엔드 (다른 저장소)
cd ../Namanmoo_Backend && ./run.sh       # http://127.0.0.1:8790

# 2) 게임
#    에디터: Assets/Scenes/Title.unity 에서 플레이
#    브라우저: ./run-web.sh --build
```

`GEMINI_API_KEY` 없이 백엔드를 띄우면 **목 모드**로 동작한다. 원본 그림을 실제로 가공해
버전별로 다른 이미지를 돌려주므로 키 없이도 3버전 선택까지 전부 확인할 수 있다.

**백엔드가 꺼져 있어도 게임은 진행된다.** 연결에 실패하면 "그대로" 한 장만 있는
선택지를 띄우고, 스탯은 기본 검과 같은 값을 쓴다.

## 화면 만들기

배경은 목업 그림 `Assets/UI/WeaponForge.png`를 그대로 깔고, 그 위에 투명한 버튼과
그리기 캔버스를 얹는 방식이다(타이틀 화면과 같은 패턴).

```
Tools → NaManMoo → Build Weapon Forge
```

좌표는 배경 그림을 실제로 측정해 넣은 **정규화 값**이다
([WeaponForgeSceneBuilder.cs](../Assets/Editor/WeaponForgeSceneBuilder.cs) 상단).
배경 그림을 다시 그리면 그 값들도 다시 재야 한다.

배경과 상호작용 요소는 같은 `AspectRatioFitter` 프레임 안에 있다. 창 비율이 16:9가
아닐 때 배경만 레터박스되면 버튼이 그림과 어긋나기 때문이다.

## 구성

| 파일 | 역할 |
| --- | --- |
| [DrawingBrush.cs](../Assets/Scripts/Forge/DrawingBrush.cs) | 픽셀에 직접 찍는 브러시 (연필·크레용·지우개). 순수 로직 |
| [DrawingHistory.cs](../Assets/Scripts/Forge/DrawingHistory.cs) | undo/redo 스냅샷 20단계. 순수 로직 |
| [DrawingCanvas.cs](../Assets/Scripts/Forge/DrawingCanvas.cs) | Texture2D 페인팅 + 포인터 입력 |
| [WhiteBackgroundKey.cs](../Assets/Scripts/Forge/WhiteBackgroundKey.cs) | 생성 이미지의 흰 배경 제거. 순수 로직 |
| [ForgeClient.cs](../Assets/Scripts/Forge/ForgeClient.cs) | `POST /forge` 업로드 (그림 + 메모 + stage) |
| [ForgeDto.cs](../Assets/Scripts/Forge/ForgeDto.cs) | 응답 DTO + `WeaponStats` 클램프 |
| [ForgedWeapon.cs](../Assets/Scripts/Forge/ForgedWeapon.cs) | 씬을 넘어 무기를 들고 가는 static 자리 |
| [WeaponForgeController.cs](../Assets/Scripts/Forge/WeaponForgeController.cs) | 그리기 → 생성 → 결과 확인 상태 기계, 팔레트 정의 |

## 알아둘 것

- **그리기 텍스처는 투명 배경**이다. 목업의 예시 그림을 가리는 흰 바탕은 뒤에 깔린 별도
  `Image`다. 이렇게 해야 내보낸 PNG에 알파가 남아 스프라이트로 바로 쓸 수 있다.
- **한글 폰트**: WebGL은 OS 폰트를 쓸 수 없어 한글이 깨진다. `Assets/Fonts/Gaegu-Regular.ttf`
  (OFL, AIGame에서 가져옴)를 프로젝트에 넣고 UI Text가 직접 참조한다.
- **스탯 검증이 두 곳에 있다.** 백엔드 `app/forge/clamp.py`와 클라이언트 `WeaponStats`가
  같은 범위를 갖는다. 범위를 바꾸면 **양쪽을 함께** 고쳐야 한다.
- **생성 이미지 배경 제거**는 가장자리에서 시작하는 플러드 필이다. 무기 안쪽의 흰
  하이라이트는 남기고 바깥과 이어진 흰색만 지운다. 음영이 강한 아트에서는 거칠 수 있고,
  그러면 서버에 제대로 된 누끼(rembg)를 붙이는 편이 낫다.
- `PlayerSwordShooter`는 이제 슬롯 번호가 아니라 **장착된 아이템**으로 발사 여부를
  판단한다(`IsProjectileWeapon`). 검은 인스펙터 값을, 만든 무기는 `ItemData.Stats`를 쓴다.
