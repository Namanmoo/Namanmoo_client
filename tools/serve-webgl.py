#!/usr/bin/env python3
"""Unity WebGL 빌드 로컬 정적 서버.

file:// 로는 WebGL 빌드가 실행되지 않는다(fetch/wasm 스트리밍 제한).
표준 http.server는 .wasm/.data MIME과 Brotli·gzip 헤더를 모르기 때문에
그 두 가지만 보강한 얇은 래퍼다.

    python3 tools/serve-webgl.py [--dir Build/WebGL] [--port 5173] [--no-open]
"""

from __future__ import annotations

import argparse
import functools
import http.server
import mimetypes
import os
import socket
import socketserver
import sys
import webbrowser

# Unity가 굽는 확장자들 — 브라우저가 올바른 타입으로 받아야 로더가 동작한다.
mimetypes.add_type("application/wasm", ".wasm")
mimetypes.add_type("application/javascript", ".js")
mimetypes.add_type("application/octet-stream", ".data")

# 압축 빌드(.wasm.br, .data.gz 등)를 그대로 올려도 되게 Content-Encoding을 붙인다.
ENCODINGS = {".br": "br", ".gz": "gzip"}


class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        base, ext = os.path.splitext(path)
        if ext in ENCODINGS:
            # unity.wasm.br → unity.wasm 기준으로 타입을 정한다.
            return super().guess_type(base)
        return super().guess_type(path)

    def end_headers(self):
        ext = os.path.splitext(self.translate_path(self.path))[1]
        encoding = ENCODINGS.get(ext)
        if encoding:
            self.send_header("Content-Encoding", encoding)
        # 개발 중에는 항상 최신 빌드를 보게 한다.
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, fmt, *args):
        # 에셋 하나마다 한 줄씩 찍히면 시끄러우므로 오류만 남긴다.
        status = args[1] if len(args) > 1 else ""
        if str(status).startswith(("4", "5")):
            super().log_message(fmt, *args)


class Server(socketserver.ThreadingTCPServer):
    # 병렬 요청(.data + .wasm 동시 로드)을 막지 않도록 스레딩 + 즉시 재바인딩.
    daemon_threads = True
    allow_reuse_address = True


def port_taken(port: int) -> bool:
    """이미 누가 듣고 있는 포트인지 확인.

    127.0.0.1 바인딩은 다른 프로세스가 [::1]만 잡고 있으면 성공해 버린다.
    그러면 브라우저의 localhost가 그쪽으로 가서 엉뚱한 서버를 보게 되므로
    양쪽 스택 모두 붙여 본다.
    """
    for family, address in ((socket.AF_INET, "127.0.0.1"), (socket.AF_INET6, "::1")):
        try:
            with socket.socket(family, socket.SOCK_STREAM) as probe:
                probe.settimeout(0.3)
                if probe.connect_ex((address, port)) == 0:
                    return True
        except OSError:
            continue
    return False


def main() -> int:
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    parser = argparse.ArgumentParser(description="Unity WebGL 빌드 로컬 서버")
    parser.add_argument("--dir", default=os.path.join(root, "Build", "WebGL"))
    parser.add_argument("--port", type=int, default=5173)
    parser.add_argument("--no-open", action="store_true", help="브라우저를 열지 않는다")
    args = parser.parse_args()

    directory = os.path.abspath(args.dir)
    if not os.path.isfile(os.path.join(directory, "index.html")):
        print(
            f"빌드를 찾을 수 없습니다: {directory}\n"
            "Unity 메뉴 Tools → NaManMoo → Build WebGL 로 먼저 빌드하거나 "
            "./run-web.sh --build 를 실행하세요.",
            file=sys.stderr,
        )
        return 1

    if port_taken(args.port):
        print(
            f"포트 {args.port}을(를) 다른 프로세스가 이미 쓰고 있습니다.\n"
            f"  확인: lsof -iTCP:{args.port} -sTCP:LISTEN -n -P\n"
            f"  다른 포트로: PORT=8080 ./run-web.sh",
            file=sys.stderr,
        )
        return 1

    handler = functools.partial(UnityWebGLHandler, directory=directory)
    url = f"http://127.0.0.1:{args.port}"
    with Server(("127.0.0.1", args.port), handler) as httpd:
        print(f"{directory} 서빙 중 → {url}  (Ctrl+C 로 종료)")
        if not args.no_open:
            webbrowser.open(url)
        try:
            httpd.serve_forever()
        except KeyboardInterrupt:
            print("\n종료합니다.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
