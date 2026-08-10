# Make Your Own Weapon Logo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 공책 낙서 타이틀과 어울리는 `나만의 무기 만들기 / MAKE YOUR OWN WEAPON` 대표 로고를 투명 PNG로 제작한다.

**Architecture:** 장식 그림과 정확한 제목 글자를 분리한다. 내장 이미지 생성으로 글자가 없는 낙서 장식층을 만들고 크로마키를 제거한 뒤, 정확한 한글·영어 글자를 결정론적으로 렌더링해 하나의 RGBA PNG로 합성한다.

**Tech Stack:** Built-in image generation, imagegen chroma-key removal helper, PowerShell `System.Drawing`, Unity-compatible PNG

## Global Constraints

- 주제목은 정확히 `나만의 무기 만들기`여야 한다.
- 부제는 정확히 `MAKE YOUR OWN WEAPON`이어야 한다.
- 한글 주제목이 중심이고 영어 부제는 작게 보조한다.
- 캔버스는 1536 x 1024px, RGBA 8비트, sRGB다.
- 배경과 네 모서리는 완전히 투명해야 한다.
- 색상은 빨강, 파랑, 초록, 노랑, 검정, 흰색으로 제한한다.
- 3D 효과, 사실적인 그림자, 금속 렌더링, 정교한 판타지 문양은 사용하지 않는다.
- 기존 `Assets/UI/Title.png`와 다른 UI 파일은 수정하거나 덮어쓰지 않는다.
- 프로젝트 `AGENTS.md` 규칙에 따라 사용자가 별도로 요청하지 않은 Git 커밋은 하지 않는다.

---

### Task 1: 글자 없는 낙서 장식층 제작

**Files:**
- Reference: `Assets/UI/Title.png`
- Create intermediate: `tmp/imagegen/Make_Your_Own_Weapon_Logo_Decoration_Chroma.png`
- Create intermediate: `tmp/imagegen/Make_Your_Own_Weapon_Logo_Decoration_Alpha.png`

**Interfaces:**
- Consumes: 기존 타이틀 화면의 공책 낙서 스타일
- Produces: 중앙 제목 영역이 비어 있는 투명 RGBA 장식층

- [ ] **Step 1: 기존 타이틀 그림을 시각 참조로 다시 확인한다**

Run: built-in `view_image` on `Assets/UI/Title.png`

Expected: 빨강·파랑·초록 크레용 선, 별과 번개, 공책 낙서 질감을 확인한다.

- [ ] **Step 2: 내장 이미지 생성으로 장식층을 만든다**

Use this exact prompt:

```text
Use case: logo-brand
Asset type: transparent title-logo decoration layer for a 2D Unity game
Primary request: Create a wide child-drawn doodle explosion frame for the game title "Make Your Own Weapon". Do not draw any letters or words. Leave a large clean horizontal empty area in the center for title text. Behind that empty area, cross a handmade pencil transforming into a toy-like sword with a simple crafting hammer. Add a few stars, lightning bolts, short impact lines, and scribble sparks around the outer perimeter.
Input image: Image 1 is a style reference only; match its rough notebook crayon drawing, uneven black outlines, visible colored-pencil grain, and intentionally elementary-school doodle quality.
Composition: wide centered emblem, approximately 3:2, generous padding, central text-safe area unobstructed.
Color palette: red, blue, green, yellow, black, and white only.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for removal, with no shadows, gradients, texture, floor, or lighting variation.
Constraints: no text, no letters, no Korean glyphs, no logo wording, no watermark; do not use #ff00ff in the artwork; no cast shadow or reflection.
Avoid: polished vector art, glossy 3D, realistic metal, ornate fantasy crest, professional calligraphy.
```

Expected: 장식이 중앙 글자 영역을 침범하지 않고, 전체가 평평한 `#ff00ff` 배경 위에 분리되어 있다.

- [ ] **Step 3: 생성 결과를 중간 경로로 복사하고 크로마키를 제거한다**

Run the installed helper with:

```text
--auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill --edge-contract 1
```

Expected: `Make_Your_Own_Weapon_Logo_Decoration_Alpha.png`가 RGBA이며 네 모서리 알파값이 0이다.

### Task 2: 정확한 제목 글자 렌더링과 최종 합성

**Files:**
- Consume: `tmp/imagegen/Make_Your_Own_Weapon_Logo_Decoration_Alpha.png`
- Create: `Assets/UI/Logo/Make_Your_Own_Weapon_Logo_01.png`

**Interfaces:**
- Consumes: Task 1의 투명 장식층
- Produces: 정확한 한글과 영어가 포함된 1536 x 1024 RGBA 로고

- [ ] **Step 1: 1536 x 1024 투명 캔버스를 만든다**

Use `System.Drawing.Bitmap(1536, 1024, Format32bppArgb)` and `CompositingMode.SourceCopy` for the transparent base.

Expected: 캔버스 네 모서리 알파값이 0이다.

- [ ] **Step 2: 장식층을 비율 유지해 중앙에 배치한다**

Place the decoration inside the rectangle `(60, 80, 1416, 820)` without stretching its aspect ratio.

Expected: 검과 망치가 중앙 글자 영역 뒤에 있고 캔버스 밖으로 잘리지 않는다.

- [ ] **Step 3: 한글 주제목을 정확히 렌더링한다**

Use installed `NanumGothicExtraBold.ttf` as the readable base. Render `나만의` centered near `Y=235` at approximately 105px and render `무기 만들기` centered near `Y=360` at approximately 185px. Build each line as a `GraphicsPath`, draw two slightly offset rough black outline passes, then fill with crayon-like red/blue/green/yellow color blocks while keeping every glyph readable.

Expected: 표기가 정확히 `나만의 무기 만들기`이고 `무기 만들기`가 가장 먼저 읽힌다.

- [ ] **Step 4: 영어 부제 종이띠와 문구를 렌더링한다**

Draw a slightly irregular white paper strip centered near `Y=665`, with a rough black outline. Render `MAKE YOUR OWN WEAPON` in uppercase black at approximately 55px.

Expected: 부제가 한 줄에 들어가고 어떤 장식과도 겹치지 않는다.

- [ ] **Step 5: 최종 PNG를 저장한다**

Save as `Assets/UI/Logo/Make_Your_Own_Weapon_Logo_01.png` using PNG format.

Expected: 1536 x 1024, `Format32bppArgb`, transparent background.

### Task 3: 규격·가독성·타이틀 조화 검증

**Files:**
- Verify: `Assets/UI/Logo/Make_Your_Own_Weapon_Logo_01.png`
- Reference: `Assets/UI/Title.png`
- Create temporary preview: `tmp/imagegen/Make_Your_Own_Weapon_Logo_Title_Preview.png`

**Interfaces:**
- Consumes: Task 2의 최종 로고
- Produces: 검증을 통과한 Unity용 대표 로고와 시각 확인용 미리보기

- [ ] **Step 1: 기계적으로 PNG 속성을 검사한다**

Check with `System.Drawing`:

```text
Width = 1536
Height = 1024
PixelFormat = Format32bppArgb
Corner alpha = 0,0,0,0
```

Expected: 네 조건이 모두 일치한다.

- [ ] **Step 2: 원본 타이틀 위에 축소 합성한 미리보기를 만든다**

Place the final logo near the top-center of a copy of `Assets/UI/Title.png`, preserving the original title image.

Expected: 공책 낙서 스타일과 이질감이 없고 기존 버튼 영역을 침범하지 않는다.

- [ ] **Step 3: 최종 이미지와 미리보기를 시각 검사한다**

Run built-in `view_image` on both files.

Expected: `무기 만들기`가 축소 상태에서도 우선 읽히고, 글자 오탈자·크로마 번짐·잘린 장식·불필요한 배경 픽셀이 없다.

- [ ] **Step 4: 문제가 있으면 한 가지 원인만 수정하고 다시 검사한다**

Allowed targeted retries: 장식 크기, 글자 크기, 글자 색 대비, 종이띠 위치, 크로마 가장자리 중 하나만 변경한다.

Expected: 수정 후 Task 3의 모든 검증 기준을 다시 통과한다.
