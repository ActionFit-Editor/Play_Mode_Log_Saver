# Play Mode Log Saver (com.actionfit.playmodelogsaver)

플레이 모드 동안의 로그를 수집해 **플레이 종료 시 txt로 저장**하는 Unity 에디터 툴입니다.

## 설치 (manifest.json, Git URL)

```json
{
  "dependencies": {
    "com.actionfit.playmodelogsaver": "https://github.com/ActionFit-Editor/Play_Mode_Log_Saver.git#1.0.4"
  }
}
```

## Unity Menu

- Package root: `Tools > Package > Play Mode Log Saver`.
- README: `Tools > Package > Play Mode Log Saver > README`.
- Setting SO: `Tools > Package > Play Mode Log Saver > Setting SO`.
- Package commands stay under the same package root and appear above the separated README/Setting SO entries when those entries exist.

## 구성

- **Editor** (`com.actionfit.playmodelogsaver.Editor`):
  - `PlayModeLogSaver` — 로그 수집·저장 (InitializeOnLoad 자동)
  - `PlayModeLogSaverWindow` — 메뉴 `Tools > Package > Play Mode Log Saver > Open Window`
  - `PlayModeLogSettings` — 설정 SO

## 설정·출력

- 설정 SO 기본 위치: `Assets/Editor/PlayModeLogSaver/PlayModeLogSettings.asset` (없으면 자동 생성, 타입 기반 자동 탐색).
- 로그 출력 폴더 기본값: `Assets/Logs` (설정 SO의 Log Folder Path로 변경 가능).
- 패키지 자체에는 설정/로그를 저장하지 않습니다.
