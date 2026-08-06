#!/usr/bin/env python3
"""무기 카탈로그를 백엔드에서 클라이언트로 복사한다.

원본은 Namanmoo_Backend의 app/forge/weapon-catalog.json 하나뿐이다. 게임은 같은 파일을
Assets/Resources/에 두고 읽는다 — 저장소가 갈라져 있어 심볼릭 링크를 쓸 수 없다.

  python3 tools/sync-catalog.py           # 복사
  python3 tools/sync-catalog.py --check   # 두 파일이 같은지만 검사 (다르면 종료코드 1)

효과를 덜어낼 때는 백엔드 원본만 고치고 이 스크립트를 다시 돌린다.
"""

from __future__ import annotations

import argparse
import hashlib
import sys
from pathlib import Path

CLIENT_ROOT = Path(__file__).resolve().parent.parent
DEST = CLIENT_ROOT / "Assets" / "Resources" / "weapon-catalog.json"

# 백엔드는 형제 디렉터리에 있다 (run-web.sh도 같은 가정을 쓴다).
# 워크트리에서 작업할 때를 위해 이름 후보를 몇 개 둔다.
SOURCE_CANDIDATES = [
    CLIENT_ROOT.parent / "Namanmoo_Backend" / "app" / "forge" / "weapon-catalog.json",
    CLIENT_ROOT.parent / "wt-weapon-effects-backend" / "app" / "forge" / "weapon-catalog.json",
]


def find_source(explicit: str | None) -> Path:
    if explicit:
        path = Path(explicit).expanduser().resolve()
        if not path.is_file():
            sys.exit(f"카탈로그 원본이 없습니다: {path}")
        return path

    for candidate in SOURCE_CANDIDATES:
        if candidate.is_file():
            return candidate

    tried = "\n  ".join(str(c) for c in SOURCE_CANDIDATES)
    sys.exit(f"카탈로그 원본을 찾지 못했습니다. --source로 알려 주세요.\n  {tried}")


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="복사하지 않고 일치 여부만 검사")
    parser.add_argument("--source", help="카탈로그 원본 경로 (기본: 형제 백엔드 저장소)")
    args = parser.parse_args()

    source = find_source(args.source)

    if args.check:
        if not DEST.is_file():
            print(f"클라이언트 사본이 없습니다: {DEST}", file=sys.stderr)
            return 1
        if digest(source) != digest(DEST):
            print(
                "카탈로그가 어긋났습니다 — `python3 tools/sync-catalog.py`로 다시 맞추세요.\n"
                f"  원본: {source}\n  사본: {DEST}",
                file=sys.stderr,
            )
            return 1
        print(f"카탈로그 일치 ({digest(source)[:12]})")
        return 0

    DEST.parent.mkdir(parents=True, exist_ok=True)
    DEST.write_bytes(source.read_bytes())
    print(f"{source}\n  → {DEST}\n  sha256 {digest(DEST)[:12]}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
