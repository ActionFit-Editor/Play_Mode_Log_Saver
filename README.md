# Play Mode Log Saver (com.actionfit.playmodelogsaver)

플레이 모드 동안의 로그를 수집해 **플레이 종료 시 txt로 저장**하는 Unity 에디터 툴입니다.

## 설치 (manifest.json, Git URL)

```json
{
  "dependencies": {
    "com.actionfit.playmodelogsaver": "https://github.com/ActionFit-Editor/Play_Mode_Log_Saver.git#1.0.6"
  }
}
```

## Unity 메뉴

- Package root: `Tools > Package > Play Mode Log Saver`.
- README: `Tools > Package > Play Mode Log Saver > README`.
- Setting SO: `Tools > Package > Play Mode Log Saver > Setting SO`.
- 패키지 명령은 같은 package root 아래에 유지하며 README/Setting SO 항목이 있으면 분리된 해당 항목보다 위에 표시합니다.

## 구성

- **Editor** (`com.actionfit.playmodelogsaver.Editor`):
  - `PlayModeLogSaver` — 로그 수집·저장 (InitializeOnLoad 자동)
  - `PlayModeLogSaverWindow` — 메뉴 `Tools > Package > Play Mode Log Saver > Open Window`
  - `PlayModeLogSettings` — 설정 SO

## 설정·출력

- 설정 SO 기본 위치: `Assets/Editor/PlayModeLogSaver/PlayModeLogSettings.asset` (없으면 자동 생성, 타입 기반 자동 탐색).
- 로그 출력 폴더 기본값: `Assets/Logs` (설정 SO의 Log Folder Path로 변경 가능).
- 패키지 자체에는 설정/로그를 저장하지 않습니다.

## Agent Skill 안내

schema v2 `Skills~/manifest.json`이 Codex와 Claude에 다음 read-only 스킬을 제공합니다.

- `$playmode-logs-help`: 설정, 저장 형식, 필터와 개인정보 보호 경계를 설명합니다.
- `$playmode-logs-latest`: 가장 최근 `*_PlayLog.txt`의 카운트와 주요 오류를 민감정보 없이 요약합니다.

요약은 Play Mode 진입, 설정 생성, Console 저장, 로그 복사·업로드·삭제를 실행하지 않으며 전체 로그나 스택 트레이스를 그대로 출력하지 않습니다.
