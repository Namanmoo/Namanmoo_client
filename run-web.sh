#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────
# NaManMoo WebGL 로컬 실행.
#
#   ./run-web.sh            이미 구운 Build/WebGL 을 서빙만 한다
#   ./run-web.sh --build    유니티를 배치 모드로 돌려 빌드한 뒤 서빙한다
#   ./run-web.sh --build --development   개발 빌드(스택트레이스 포함)
#   ./run-web.sh --no-open  브라우저를 자동으로 열지 않는다
#
# 유니티 실행 파일은 ProjectSettings/ProjectVersion.txt 의 버전으로 찾는다.
# 다른 경로에 설치했다면 UNITY_BIN=/path/to/Unity ./run-web.sh --build
# 포트 변경: PORT=8080 ./run-web.sh
# ─────────────────────────────────────────────────────────────
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$ROOT/Build/WebGL"
PORT="${PORT:-5173}"

DO_BUILD=0
DEVELOPMENT=""
NO_OPEN=""
for arg in "$@"; do
  case "$arg" in
    --build) DO_BUILD=1 ;;
    --development|--dev) DEVELOPMENT="--development" ;;
    --no-open) NO_OPEN="--no-open" ;;
    -h|--help) awk 'NR > 1 { if (!/^#/) exit; print }' "$0"; exit 0 ;;
    *) echo "알 수 없는 옵션: $arg" >&2; exit 2 ;;
  esac
done

find_unity() {
  if [ -n "${UNITY_BIN:-}" ]; then
    echo "$UNITY_BIN"
    return
  fi
  local version
  version="$(awk '/^m_EditorVersion:/ {print $2}' "$ROOT/ProjectSettings/ProjectVersion.txt")"
  echo "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity"
}

if [ "$DO_BUILD" -eq 1 ]; then
  UNITY="$(find_unity)"
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
    -executeMethod WebGLBuilder.BuildFromCommandLine \
    -logFile "$LOG" ${DEVELOPMENT:+$DEVELOPMENT} \
    || { echo "빌드 실패 — $LOG 확인" >&2; exit 1; }
  echo "빌드 완료: $BUILD_DIR"
fi

if [ ! -f "$BUILD_DIR/index.html" ]; then
  echo "빌드가 없습니다: $BUILD_DIR" >&2
  echo "유니티 메뉴 Tools → NaManMoo → Build WebGL 또는 ./run-web.sh --build 를 실행하세요." >&2
  exit 1
fi

exec python3 "$ROOT/tools/serve-webgl.py" --dir "$BUILD_DIR" --port "$PORT" ${NO_OPEN:+$NO_OPEN}
