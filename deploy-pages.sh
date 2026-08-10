#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────
# NaManMoo WebGL 을 GitHub Pages 저장소로 배포한다.
#
#   ./deploy-pages.sh           이미 구운 Build/WebGL 을 복사해 PR 을 만든다
#   ./deploy-pages.sh --build   유니티를 배치 모드로 돌려 빌드한 뒤 배포한다
#
# Pages 저장소 위치는 하드코딩하지 않는다 — 이 프로젝트의 상위 폴더에서
# Namanmoo.github.io 라는 하위 폴더를 찾아 그곳에 배포한다.
# 배포는 main 에 직접 커밋하지 않고 deploy/<날짜> 브랜치로 PR 을 만든다.
# ─────────────────────────────────────────────────────────────
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$ROOT/Build/WebGL"

DO_BUILD=0
for arg in "$@"; do
  case "$arg" in
    --build) DO_BUILD=1 ;;
    -h|--help) awk 'NR > 1 { if (!/^#/) exit; print }' "$0"; exit 0 ;;
    *) echo "알 수 없는 옵션: $arg" >&2; exit 2 ;;
  esac
done

# ── Pages 저장소 찾기 ─────────────────────────────────────────
PAGES_DIR=""
for d in "$ROOT"/../*/; do
  if [ "$(basename "$d")" = "Namanmoo.github.io" ]; then
    PAGES_DIR="$(cd "$d" && pwd)"
    break
  fi
done
if [ -z "$PAGES_DIR" ]; then
  echo "상위 폴더에서 Namanmoo.github.io 를 찾지 못했습니다: $ROOT/.." >&2
  exit 1
fi
if [ ! -d "$PAGES_DIR/.git" ]; then
  echo "Pages 폴더가 git 저장소가 아닙니다: $PAGES_DIR" >&2
  exit 1
fi

# ── 필요하면 빌드 ────────────────────────────────────────────
if [ "$DO_BUILD" -eq 1 ]; then
  UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/$(awk '/^m_EditorVersion:/ {print $2}' "$ROOT/ProjectSettings/ProjectVersion.txt")/Unity.app/Contents/MacOS/Unity}"
  if [ ! -x "$UNITY" ]; then
    echo "유니티 실행 파일을 찾을 수 없습니다: $UNITY" >&2
    echo "UNITY_BIN 환경변수로 직접 지정하세요." >&2
    exit 1
  fi
  mkdir -p "$ROOT/Logs"
  LOG="$ROOT/Logs/webgl-build.log"
  echo "WebGL 빌드 시작 (몇 분 걸립니다) — 로그: $LOG"
  "$UNITY" -quit -batchmode -nographics \
    -projectPath "$ROOT" \
    -buildTarget WebGL \
    -executeMethod WebGLBuilder.Build \
    -logFile "$LOG" \
    || { echo "빌드 실패 — $LOG 확인" >&2; exit 1; }
  echo "빌드 완료: $BUILD_DIR"
fi

if [ ! -f "$BUILD_DIR/index.html" ]; then
  echo "빌드가 없습니다: $BUILD_DIR" >&2
  echo "./deploy-pages.sh --build 로 빌드부터 하세요." >&2
  exit 1
fi

# ── Pages 저장소에 복사하고 PR ────────────────────────────────
cd "$PAGES_DIR"

if [ -n "$(git status --porcelain | grep -v '^?? \.DS_Store$' || true)" ]; then
  echo "Pages 저장소에 커밋되지 않은 변경이 있습니다 — 정리 후 다시 실행하세요." >&2
  git status --short >&2
  exit 1
fi

git checkout main
git pull --ff-only origin main

BRANCH="deploy/$(date +%Y%m%d-%H%M%S)"
git checkout -b "$BRANCH"

# .git 과 README 는 남기고 빌드 산출물만 통째로 갈아끼운다
rsync -a --delete \
  --exclude .git \
  --exclude README.md \
  --exclude .DS_Store \
  "$BUILD_DIR"/ "$PAGES_DIR"/

git add -A
if git diff --cached --quiet; then
  echo "빌드 결과가 배포본과 동일합니다 — 만들 PR 이 없습니다."
  git checkout main
  git branch -D "$BRANCH"
  exit 0
fi

CLIENT_COMMIT="$(git -C "$ROOT" rev-parse --short HEAD)"
git commit -m "deploy: WebGL 빌드 갱신 (client @$CLIENT_COMMIT)"
git push -u origin "$BRANCH"
gh pr create \
  --title "deploy: WebGL 빌드 갱신 (client @$CLIENT_COMMIT)" \
  --body "Namanmoo_client @$CLIENT_COMMIT 의 Build/WebGL 산출물로 교체합니다."

git checkout main
echo "배포 PR 생성 완료 — 머지하면 github.io 에 반영됩니다."
