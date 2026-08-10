# Fancy Title Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `title.kra`의 이야기를 유지하면서 무기 상상 효과와 야외 배경을 강화한 1920×1080 타이틀 화면을 제작한다.

**Architecture:** 내장 이미지 생성으로 글자 없는 전체 장면을 먼저 만들고 16:9 규격으로 정리한다. 정확한 영어 제목, 한국어 말풍선과 `Game Start` 버튼 글자는 결정론적으로 별도 렌더링해 오탈자를 방지한다.

**Tech Stack:** Built-in image generation, Krita KRA merged preview, PowerShell `System.Drawing`, Unity-compatible PNG

## Global Constraints

- 최종 결과는 정확히 1920 x 1080px, RGB/Alpha 8비트 PNG, sRGB다.
- 제목은 `MAKE YOUR` / `OWN WEAPON` 두 줄이며 `OWN WEAPON`이 약 20% 크다.
- 말풍선 문구는 정확히 `나 그림왕이 될 거야!`다.
- 버튼 문구는 정확히 `Game Start`이며 다른 버튼은 추가하지 않는다.
- 왼쪽 55%는 제목과 버튼, 오른쪽 45%는 캐릭터·책상·나무 영역이다.
- 캐릭터는 둥근 흰 머리, 큰 검은 눈, W자형 입, 밀짚모자, 빨간 조끼와 파란 허리띠를 유지한다.
- 최소 네 종류의 무기 낙서가 캐릭터의 종이에서 튀어나와야 한다.
- 매끈한 벡터, 3D, 사실적 금속과 정교한 판타지 문양을 사용하지 않는다.
- 기존 `Assets/UI/Title.png`와 Unity 씬은 수정하지 않는다.
- 프로젝트 `AGENTS.md` 규칙에 따라 사용자가 요청하지 않은 Git 커밋은 하지 않는다.

---

### Task 1: 글자 없는 상상 폭발 장면 생성

**Files:**
- Reference: `C:/Users/dksco/Naman/Namanmoo_client/tmp/kra-preview/title/mergedimage.png`
- Create intermediate: `tmp/imagegen/Title_Fancy_Scene_Source.png`

**Interfaces:**
- Consumes: `title.kra`의 캐릭터, 책상, 말풍선, 나무와 좌우 구도
- Produces: 글자 안전 영역이 확보된 색연필 장면

- [ ] **Step 1: 기준 이미지를 시각적으로 확인한다**

Run built-in `view_image` on `tmp/kra-preview/title/mergedimage.png`.

Expected: 왼쪽 제목·버튼 공간, 오른쪽 캐릭터·책상, 상단 말풍선과 오른쪽 나무를 확인한다.

- [ ] **Step 2: 내장 이미지 생성으로 16:9 전체 장면을 만든다**

Use this exact prompt with the merged KRA image as a composition and style reference:

```text
Use case: ui-mockup
Asset type: full-screen illustrated title background for a 2D Unity game
Primary request: Reimagine the reference as a richer, more spectacular 16:9 title screen while preserving its story and layout. On the right, the same proud child-doodle hero sits behind a drawing desk, wearing a straw hat with a red band, red vest, and blue sash, holding a large blue drawing pen in the raised right hand. The hero has a huge round white head, two oversized black eyes, a tiny W-shaped mouth, and a confident excited expression. A large crayon tree frames the far right edge. On the left, preserve a large clean open area for a two-line game title and a separate clean area near the lower left for one button.
Action: From the paper on the desk, at least five colorful child-drawn weapons burst into imagination: sword, axe, bow, magic wand, and toy-like blaster. Arrange them in a curved motion from the lower right toward the upper center, with stars, lightning bolts, paper scraps, crayon dust, and short impact marks. Keep every weapon and decoration out of the left title and button safe zones.
Speech bubble: draw one large empty white speech bubble above the hero, with its tail pointing at the hero. Leave the inside completely blank for later Korean text.
Background: warm off-white sketchbook paper, loosely colored pale blue sky, bright green grass, small flowers, rounded distant hills, white paper gaps, sunny playful mood.
Style/medium: deliberately rough elementary-school colored-pencil and crayon drawing, uneven black line pressure, visible scribble grain, imperfect proportions, energetic hand-drawn composition. Match the reference rather than polishing it.
Composition: exact 16:9 landscape. Left 55 percent is a quiet title-safe area. Right 45 percent contains the hero, desk, tree, and speech bubble. Keep the hero fully inside frame.
Color palette: strong red, blue, green, yellow, black, warm brown, pale sky blue, and paper white.
Constraints: absolutely no text, no letters, no fake words, no logos, no button labels, no watermark. The title-safe area, speech bubble interior, and button-safe area must remain clear.
Avoid: polished vector art, commercial preschool illustration, anime rendering, glossy 3D, realistic lighting, realistic metal, detailed fantasy ornament, photographic texture.
```

Expected: 참조 구도가 유지되고 가짜 글자가 없으며 왼쪽 안전 영역과 말풍선 내부가 비어 있다.

- [ ] **Step 3: 결과를 중간 파일로 복사한다**

Copy the selected built-in output to `tmp/imagegen/Title_Fancy_Scene_Source.png` without deleting the generated original.

Expected: 프로젝트 내부 중간 파일이 존재한다.

### Task 2: 1920×1080 장면 정규화와 정확한 문구 합성

**Files:**
- Consume: `tmp/imagegen/Title_Fancy_Scene_Source.png`
- Create intermediate: `tmp/imagegen/Title_Fancy_Scene_1920x1080.png`
- Create final: `Assets/UI/Title_Fancy_01.png`

**Interfaces:**
- Consumes: Task 1의 글자 없는 장면
- Produces: 정확한 문구와 단일 버튼이 포함된 Unity용 타이틀 PNG

- [ ] **Step 1: 생성 장면을 1920×1080으로 정규화한다**

Use high-quality bicubic `System.Drawing` resizing. Preserve the 16:9 composition; when source ratio differs, crop only the smallest equal amount from the top and bottom rather than cutting the left title area or right character.

Expected: `Title_Fancy_Scene_1920x1080.png`가 정확히 1920×1080이다.

- [ ] **Step 2: 영어 제목을 왼쪽 안전 영역에 렌더링한다**

Render `MAKE YOUR` near `(130, 170)` and `OWN WEAPON` near `(95, 345)` using a heavy handwritten English base, rough black double outlines, and visible crayon fill. Use blue/green for the first line and red/yellow/blue color blocks for the second. Keep all glyphs readable and inside `X=80..1060`.

Expected: `OWN WEAPON`이 더 크고 두 줄 모두 축소 상태에서도 읽힌다.

- [ ] **Step 3: 한국어 말풍선 문구를 렌더링한다**

Use installed `NanumGothicExtraBold.ttf` with a slightly irregular black crayon texture. Fit the exact text `나 그림왕이 될 거야!` inside the generated empty speech bubble without touching its outline.

Expected: 오탈자가 없고 말풍선 꼬리가 캐릭터를 향한다.

- [ ] **Step 4: 단일 Game Start 버튼을 합성한다**

Draw one irregular white paper button with rough black outline in the lower-left safe area near `X=220..760`, `Y=785..930`. Render exact text `Game Start` in blue crayon with a small red hand-drawn arrow and two yellow stars. Do not add Settings or Exit.

Expected: 버튼이 다른 낙서와 겹치지 않고 한눈에 클릭 요소로 보인다.

- [ ] **Step 5: 최종 파일을 저장한다**

Save the composed image as `Assets/UI/Title_Fancy_01.png` using PNG format.

Expected: 1920×1080 RGBA PNG이며 기존 `Assets/UI/Title.png`는 변경되지 않는다.

### Task 3: 규격·구도·문구 검증

**Files:**
- Verify: `Assets/UI/Title_Fancy_01.png`
- Reference: `tmp/kra-preview/title/mergedimage.png`

**Interfaces:**
- Consumes: Task 2의 최종 타이틀 화면
- Produces: 사용자에게 제시할 검증된 이미지

- [ ] **Step 1: 최종 PNG 속성을 검사한다**

Check width, height, pixel format, and file readability.

```text
Width = 1920
Height = 1080
Mode = RGBA or RGB
File opens without decode errors
```

Expected: 네 조건이 모두 일치한다.

- [ ] **Step 2: 문구와 기능 범위를 시각 검사한다**

Run built-in `view_image` and verify the exact visible strings:

```text
MAKE YOUR
OWN WEAPON
나 그림왕이 될 거야!
Game Start
```

Expected: 다른 버튼이나 가짜 글자가 없고 네 문구만 정확히 보인다.

- [ ] **Step 3: 장면 구성 기준을 시각 검사한다**

Verify the hero, desk, tree and speech bubble are on the right, while the title and button are on the left. Count at least four visibly distinct weapon doodles emerging from the desk paper.

Expected: 참조 KRA의 구도가 인식되며 제목·버튼과 무기 장식이 겹치지 않는다.

- [ ] **Step 4: 기존 타이틀 파일의 미변경을 검사한다**

Run `git diff --quiet -- Assets/UI/Title.png`.

Expected: exit code 0.

- [ ] **Step 5: 한 가지 문제가 있으면 한 원인만 수정하고 재검증한다**

Allowed single-variable corrections are composition crop, title size, speech text fit, button location, or decorative density. Re-run all Task 3 checks after the correction.

Expected: 모든 검증 조건이 통과한다.
