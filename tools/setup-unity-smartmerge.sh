#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────
# Unity Smart Merge(UnityYAMLMerge)를 git 머지 드라이버로 등록한다.
# .gitattributes 가 *.unity / *.prefab / *.asset 을 이 드라이버로 보낸다.
#
# 유니티 설치 경로가 머신마다 달라 저장소에 담을 수 없다 —
# 클론(워크트리 아님, 저장소당)마다 한 번 실행한다:
#   ./tools/setup-unity-smartmerge.sh
#
# 버전은 ProjectSettings/ProjectVersion.txt 에서 읽는다.
# 다른 경로에 설치했다면 UNITY_YAML_MERGE=/path/to/UnityYAMLMerge 로 지정.
# ─────────────────────────────────────────────────────────────
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

find_tool() {
  if [ -n "${UNITY_YAML_MERGE:-}" ]; then
    echo "$UNITY_YAML_MERGE"
    return
  fi
  local version
  version="$(awk '/^m_EditorVersion:/ {print $2}' "$ROOT/ProjectSettings/ProjectVersion.txt")"
  echo "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/Helpers/UnityYAMLMerge"
}

TOOL="$(find_tool)"
if [ ! -x "$TOOL" ]; then
  echo "UnityYAMLMerge 를 찾을 수 없습니다: $TOOL" >&2
  echo "UNITY_YAML_MERGE 환경변수로 직접 지정하세요." >&2
  exit 1
fi

# -h 헤드리스(GUI 폴백 금지) / -p 프리머지 / --fallback none 외부 도구 호출 금지.
# %O=공통 조상, %B=상대편, %A=우리편(결과를 여기 남겨야 한다).
git -C "$ROOT" config merge.unityyamlmerge.name "Unity SmartMerge"
git -C "$ROOT" config merge.unityyamlmerge.driver \
  "'$TOOL' merge -h -p --force --fallback none %O %B %A %A"

echo "등록 완료: $TOOL"
echo "이제 *.unity / *.prefab / *.asset 머지는 오브젝트 단위로 자동 병합됩니다."
