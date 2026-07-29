# WebGL 빌드 · 실행

브라우저에서 NaManMoo를 실행하기 위한 구성. 유니티 프로젝트 루트가 곧 레포 루트이므로
빌드 산출물은 `Build/WebGL`(gitignore 대상)에 떨어지고, 얇은 파이썬 정적 서버로 띄운다.

## 구성 요소

| 경로 | 역할 |
| --- | --- |
| [Assets/Editor/WebGLBuilder.cs](../Assets/Editor/WebGLBuilder.cs) | 빌드 진입점. 메뉴 `Tools → NaManMoo → Build WebGL`, 배치 모드 `WebGLBuilder.BuildFromCommandLine` |
| [Assets/WebGLTemplates/NaManMoo/index.html](../Assets/WebGLTemplates/NaManMoo/index.html) | 커스텀 로딩 템플릿 (진행률 바, 16:9 레터박스, 전체화면 버튼) |
| [tools/serve-webgl.py](../tools/serve-webgl.py) | 로컬 정적 서버 — `.wasm` MIME과 Brotli·gzip `Content-Encoding` 보강 |
| [run-web.sh](../run-web.sh) | 빌드 + 서빙 래퍼 |

## 실행

```bash
# 유니티 에디터에서 빌드한 뒤 서빙만
./run-web.sh

# 배치 모드로 빌드까지 (몇 분 걸림, 로그는 Logs/webgl-build.log)
./run-web.sh --build

# 개발 빌드 (예외 스택트레이스 + 스트리핑 Minimal)
./run-web.sh --build --development
```

기본 주소는 <http://localhost:5173>. 포트는 `PORT=8080 ./run-web.sh`,
유니티 경로는 `UNITY_BIN=/path/to/Unity ./run-web.sh --build` 로 바꾼다.

`file://` 로 `Build/WebGL/index.html`을 직접 열면 동작하지 않는다 —
로더가 `fetch`로 `.wasm`을 스트리밍하기 때문에 반드시 HTTP로 서빙해야 한다.

## 사전 준비

- Unity Hub → Installs → 해당 에디터 → **Add modules → WebGL Build Support** 설치.
  (모듈이 없으면 빌드 스크립트가 그 사실을 로그로 알려주고 중단한다.)
- 빌드 대상 씬은 Build Settings의 활성 씬 목록을 그대로 쓴다 — 현재 `Title` → `Stage1`.
  씬을 추가했다면 Build Profiles에서 켜두어야 빌드에 포함된다.

## 빌드 설정

`WebGLBuilder.ApplyPlayerSettings`가 매 빌드마다 아래를 강제한다.

- 템플릿: `PROJECT:NaManMoo`
- 압축: **없음** — 개발용 정적 서버가 `Content-Encoding`을 못 붙여도 로더가 깨지지 않게.
  배포 시에는 Brotli로 바꾸고 웹서버(예: Caddy/Nginx)가 헤더를 붙이도록 한다.
  `serve-webgl.py`는 그 경우에도 `.br`/`.gz`를 올바르게 서빙한다.
- 예외 처리: 릴리스는 `ExplicitlyThrownExceptionsOnly`, 개발 빌드는 `FullWithStacktrace`
- 매니지드 스트리핑: 릴리스 `High`, 개발 `Minimal`
- 데이터 캐싱(IndexedDB) 사용

## 알려진 경고

브라우저 콘솔에 아래 경고가 뜬다. 게임 동작에는 지장이 없어 그대로 두었다.

```
Shader 'Hidden/Universal Render Pipeline/Edge Adaptive Spatial Upsampling' is not supported
(in 'Blit FSR Upscaling'). PostProcessing render passes will not execute.
```

`Assets/Settings/UniversalRP.asset`의 `m_UpscalingFilter`가 `Automatic(0)`이라 URP가 FSR
업스케일 경로를 시도하는데, 그 셰이더가 WebGL(GLES3)에서 지원되지 않아 생긴다.
포스트프로세싱을 WebGL에서 쓰게 되면 업스케일 필터를 `Linear`로 고정해야 한다.

## 포트 충돌

`serve-webgl.py`는 시작 전에 IPv4/IPv6 양쪽으로 포트를 확인하고, 이미 쓰는 프로세스가
있으면 그 사실을 알리고 종료한다 (`127.0.0.1`만 비어 있고 `::1`을 남이 쓰고 있으면
바인딩은 성공하지만 브라우저의 `localhost`가 엉뚱한 서버로 가기 때문).
다른 포트를 쓰려면 `PORT=8080 ./run-web.sh`.

## 배포로 넘어갈 때

1. `PlayerSettings.WebGL.compressionFormat`을 `Brotli`로 전환.
2. 웹서버에서 `.br` 파일에 `Content-Encoding: br` + 원본 MIME을 붙인다.
3. `Cache-Control`을 해시 파일명 기반 장기 캐시로 전환
   (`PlayerSettings.WebGL.nameFilesAsHashes = true`).
