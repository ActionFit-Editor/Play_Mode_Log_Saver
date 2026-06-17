using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CustomPackage.PlayModeLogSaver
{
    /// <summary>
    /// 플레이 모드 중 발생한 로그를 수집하고, 플레이 종료 시 txt 파일로 저장합니다.
    /// InitializeOnLoad로 에디터 시작 시 자동 초기화됩니다.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeLogSaver
    {
        #region Constants

        private const string SETTINGS_PATH = "Assets/Editor/PlayModeLogSaver/PlayModeLogSettings.asset";
        private const string PLAY_LOG_PREFIX = "PlayLog";
        private const string CONSOLE_LOG_PREFIX = "ConsoleLog";
        private const string LOG_FILE_EXTENSION = ".txt";

        #endregion

        #region Fields

        // 수집된 로그 목록
        private static readonly List<LogEntry> _logs = new();

        // 설정 캐시
        private static PlayModeLogSettings _settings;

        // 플레이 모드 시작 시간
        private static DateTime _playStartTime;

        #endregion

        #region Properties

        // 설정 SO 가져오기 (없으면 생성)
        public static PlayModeLogSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = AssetDatabase.LoadAssetAtPath<PlayModeLogSettings>(SETTINGS_PATH);

                    if (_settings == null)
                    {
                        // 고정 경로에 없으면 타입으로 프로젝트 전체 탐색 (위치 무관)
                        var guids = AssetDatabase.FindAssets($"t:{nameof(PlayModeLogSettings)}");
                        if (guids.Length > 0)
                            _settings = AssetDatabase.LoadAssetAtPath<PlayModeLogSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    }

                    if (_settings == null)
                    {
                        _settings = CreateSettings();
                    }
                }
                return _settings;
            }
        }

        // 현재 수집된 로그 수
        public static int LogCount => _logs.Count;

        #endregion

        #region 초기화

        static PlayModeLogSaver()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // 설정 SO 생성
        private static PlayModeLogSettings CreateSettings()
        {
            var settings = ScriptableObject.CreateInstance<PlayModeLogSettings>();

            // 폴더 확인 및 생성
            string directory = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PlayModeLogSaver] 설정 파일 생성됨: {SETTINGS_PATH}");
            return settings;
        }

        #endregion

        #region 플레이 모드 상태 변경

        // 플레이 모드 상태 변경 시 호출
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    OnEnteredPlayMode();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    OnExitingPlayMode();
                    break;
            }
        }

        // 플레이 모드 진입 시
        private static void OnEnteredPlayMode()
        {
            if (!Settings.IsEnabled) return;

            _logs.Clear();
            _playStartTime = DateTime.Now;
            Application.logMessageReceived += OnLogMessageReceived;

            Debug.Log("[PlayModeLogSaver] 로그 수집 시작");
        }

        // 플레이 모드 종료 시
        private static void OnExitingPlayMode()
        {
            Application.logMessageReceived -= OnLogMessageReceived;

            if (!Settings.IsEnabled) return;
            if (_logs.Count == 0)
            {
                Debug.Log("[PlayModeLogSaver] 저장할 로그 없음");
                return;
            }

            SaveLogsToFile();
        }

        #endregion

        #region 로그 수집

        // 로그 메시지 수신 시
        private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            // Error/Warning만 저장 옵션 체크
            if (Settings.LogOnlyErrorsAndWarnings)
            {
                if (type != LogType.Error && type != LogType.Exception && type != LogType.Warning)
                {
                    return;
                }
            }

            var entry = new LogEntry
            {
                Time = DateTime.Now,
                Type = type,
                Message = message,
                StackTrace = stackTrace
            };

            _logs.Add(entry);
        }

        #endregion

        #region 파일 저장

        // 플레이 모드 로그를 파일로 저장
        private static void SaveLogsToFile()
        {
            string folderPath = Settings.LogFolderPath;

            // 폴더 경로 유효성 검사
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("[PlayModeLogSaver] 로그 저장 폴더가 설정되지 않음");
                return;
            }

            // 폴더 생성
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 파일명 생성: yyyy-MM-dd,HH-mm-ss_PlayLog.txt
            DateTime exitTime = DateTime.Now;
            string fileName = $"{exitTime:yyyy-MM-dd,HH-mm-ss}_{PLAY_LOG_PREFIX}{LOG_FILE_EXTENSION}";
            string filePath = Path.Combine(folderPath, fileName);

            // 로그 내용 생성
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                        Play Mode Log");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"플레이 시작: {_playStartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"플레이 종료: {exitTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"플레이 시간: {(exitTime - _playStartTime):hh\\:mm\\:ss}");
            sb.AppendLine($"총 로그 수: {_logs.Count}");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // 로그 타입별 카운트
            int logCount = 0, warningCount = 0, errorCount = 0;
            foreach (var log in _logs)
            {
                switch (log.Type)
                {
                    case LogType.Log:
                        logCount++;
                        break;
                    case LogType.Warning:
                        warningCount++;
                        break;
                    case LogType.Error:
                    case LogType.Exception:
                        errorCount++;
                        break;
                }
            }

            sb.AppendLine($"[Log: {logCount}] [Warning: {warningCount}] [Error: {errorCount}]");
            sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // 각 로그 출력
            foreach (var log in _logs)
            {
                string typePrefix = log.Type switch
                {
                    LogType.Log => "[LOG]",
                    LogType.Warning => "[WARNING]",
                    LogType.Error => "[ERROR]",
                    LogType.Exception => "[EXCEPTION]",
                    LogType.Assert => "[ASSERT]",
                    _ => "[UNKNOWN]"
                };

                sb.AppendLine($"[{log.Time:HH:mm:ss.fff}] {typePrefix}");
                sb.AppendLine(log.Message);

                if (Settings.IncludeStackTrace && !string.IsNullOrEmpty(log.StackTrace))
                {
                    sb.AppendLine("--- Stack Trace ---");
                    sb.AppendLine(log.StackTrace);
                }

                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            // 파일 저장
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[PlayModeLogSaver] 로그 저장 완료: {filePath} ({_logs.Count}개 로그)");

            // Asset Database 갱신 (Assets 폴더 내인 경우)
            if (folderPath.StartsWith("Assets"))
            {
                AssetDatabase.Refresh();
            }
        }

        #endregion

        #region 콘솔 로그 수동 저장

        /// <summary>
        /// Unity 콘솔에 현재 표시된 모든 로그를 txt 파일로 저장합니다.
        /// 컴파일 에러, 경고 등 에디터 모드에서 발생한 로그를 저장할 때 사용합니다.
        /// </summary>
        /// <returns>저장된 파일 경로, 실패 시 null</returns>
        public static string SaveConsoleLogsToFile()
        {
            string folderPath = Settings.LogFolderPath;

            // 폴더 경로 유효성 검사
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogWarning("[PlayModeLogSaver] 로그 저장 폴더가 설정되지 않음");
                return null;
            }

            // 폴더 생성
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Unity 콘솔 로그 가져오기
            var consoleLogs = GetUnityConsoleLogs();

            if (consoleLogs.Count == 0)
            {
                Debug.Log("[PlayModeLogSaver] 저장할 콘솔 로그가 없습니다.");
                return null;
            }

            // 파일명 생성: yyyy-MM-dd,HH-mm-ss_ConsoleLog.txt
            DateTime saveTime = DateTime.Now;
            string fileName = $"{saveTime:yyyy-MM-dd,HH-mm-ss}_{CONSOLE_LOG_PREFIX}{LOG_FILE_EXTENSION}";
            string filePath = Path.Combine(folderPath, fileName);

            // 로그 내용 생성
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                        Console Log (Manual Save)");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"저장 시간: {saveTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"총 로그 수: {consoleLogs.Count}");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // 로그 타입별 카운트
            int logCount = 0, warningCount = 0, errorCount = 0;
            foreach (var log in consoleLogs)
            {
                switch (log.Type)
                {
                    case LogType.Log:
                        logCount++;
                        break;
                    case LogType.Warning:
                        warningCount++;
                        break;
                    case LogType.Error:
                    case LogType.Exception:
                        errorCount++;
                        break;
                }
            }

            sb.AppendLine($"[Log: {logCount}] [Warning: {warningCount}] [Error: {errorCount}]");
            sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            // 각 로그 출력
            foreach (var log in consoleLogs)
            {
                string typePrefix = log.Type switch
                {
                    LogType.Log => "[LOG]",
                    LogType.Warning => "[WARNING]",
                    LogType.Error => "[ERROR]",
                    LogType.Exception => "[EXCEPTION]",
                    LogType.Assert => "[ASSERT]",
                    _ => "[UNKNOWN]"
                };

                sb.AppendLine($"{typePrefix}");
                sb.AppendLine(log.Message);

                if (Settings.IncludeStackTrace && !string.IsNullOrEmpty(log.StackTrace))
                {
                    sb.AppendLine("--- Stack Trace ---");
                    sb.AppendLine(log.StackTrace);
                }

                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            // 파일 저장
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[PlayModeLogSaver] 콘솔 로그 저장 완료: {filePath} ({consoleLogs.Count}개 로그)");

            // Asset Database 갱신 (Assets 폴더 내인 경우)
            if (folderPath.StartsWith("Assets"))
            {
                AssetDatabase.Refresh();
            }

            return filePath;
        }

        // Unity 콘솔의 로그를 리플렉션으로 가져오기
        private static List<LogEntry> GetUnityConsoleLogs()
        {
            var logs = new List<LogEntry>();

            try
            {
                // UnityEditor.LogEntries 클래스 가져오기 (internal 클래스)
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null)
                {
                    Debug.LogError("[PlayModeLogSaver] LogEntries 타입을 찾을 수 없습니다.");
                    return logs;
                }

                // LogEntry 구조체 타입 가져오기
                var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                if (logEntryType == null)
                {
                    Debug.LogError("[PlayModeLogSaver] LogEntry 타입을 찾을 수 없습니다.");
                    return logs;
                }

                // GetCount 메서드
                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                if (getCountMethod == null)
                {
                    Debug.LogError("[PlayModeLogSaver] GetCount 메서드를 찾을 수 없습니다.");
                    return logs;
                }

                // StartGettingEntries 메서드
                var startMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);

                // EndGettingEntries 메서드
                var endMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                // GetEntryInternal 메서드
                var getEntryMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                if (getEntryMethod == null)
                {
                    Debug.LogError("[PlayModeLogSaver] GetEntryInternal 메서드를 찾을 수 없습니다.");
                    return logs;
                }

                // LogEntry 필드들
                var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
                var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
                var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);

                // 로그 개수 가져오기
                int count = (int)getCountMethod.Invoke(null, null);

                if (count == 0) return logs;

                // 로그 가져오기 시작
                startMethod?.Invoke(null, null);

                try
                {
                    // LogEntry 인스턴스 생성
                    var logEntryInstance = Activator.CreateInstance(logEntryType);

                    for (int i = 0; i < count; i++)
                    {
                        // GetEntryInternal(int index, ref LogEntry entry) 호출
                        object[] args = { i, logEntryInstance };
                        bool result = (bool)getEntryMethod.Invoke(null, args);

                        if (result)
                        {
                            logEntryInstance = args[1]; // ref로 업데이트된 값

                            string message = messageField?.GetValue(logEntryInstance) as string ?? "";
                            int mode = modeField != null ? (int)modeField.GetValue(logEntryInstance) : 0;

                            // mode를 LogType으로 변환 (Unity LogMessageFlags 비트 정의)
                            // kError=1, kAssert=2, kLog=4, kFatal=16, kAssetImportError=32, kAssetImportWarning=64,
                            // kScriptingError=256, kScriptingWarning=512, kScriptingLog=1024,
                            // kScriptCompileError=2048, kScriptCompileWarning=4096, kScriptingException=131072
                            const int kError = 1;
                            const int kAssert = 2;
                            const int kFatal = 1 << 4;          // 16
                            const int kAssetImportError = 1 << 5;   // 32
                            const int kAssetImportWarning = 1 << 6; // 64
                            const int kScriptingError = 1 << 8;     // 256
                            const int kScriptingWarning = 1 << 9;   // 512
                            const int kScriptCompileError = 1 << 11; // 2048
                            const int kScriptCompileWarning = 1 << 12; // 4096
                            const int kScriptingException = 1 << 17; // 131072

                            LogType logType = LogType.Log;
                            if ((mode & (kError | kFatal | kAssetImportError | kScriptingError | kScriptCompileError)) != 0)
                                logType = LogType.Error;
                            else if ((mode & kScriptingException) != 0)
                                logType = LogType.Exception;
                            else if ((mode & kAssert) != 0)
                                logType = LogType.Assert;
                            else if ((mode & (kAssetImportWarning | kScriptingWarning | kScriptCompileWarning)) != 0)
                                logType = LogType.Warning;

                            // Error/Warning만 저장 옵션 체크
                            if (Settings.LogOnlyErrorsAndWarnings)
                            {
                                if (logType != LogType.Error && logType != LogType.Exception &&
                                    logType != LogType.Warning && logType != LogType.Assert)
                                {
                                    continue;
                                }
                            }

                            // 메시지와 스택 트레이스 분리
                            string stackTrace = "";
                            int newlineIndex = message.IndexOf('\n');
                            if (newlineIndex > 0)
                            {
                                stackTrace = message.Substring(newlineIndex + 1);
                                message = message.Substring(0, newlineIndex);
                            }

                            logs.Add(new LogEntry
                            {
                                Time = DateTime.Now,
                                Type = logType,
                                Message = message,
                                StackTrace = stackTrace
                            });
                        }
                    }
                }
                finally
                {
                    // 로그 가져오기 종료
                    endMethod?.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayModeLogSaver] 콘솔 로그 가져오기 실패: {e.Message}\n{e.StackTrace}");
            }

            return logs;
        }

        /// <summary>
        /// 현재 Unity 콘솔에 표시된 로그 개수를 반환합니다.
        /// </summary>
        public static int GetConsoleLogCount()
        {
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                if (logEntriesType == null) return 0;

                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                if (getCountMethod == null) return 0;

                return (int)getCountMethod.Invoke(null, null);
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 설정된 폴더의 모든 로그 파일을 삭제합니다.
        /// </summary>
        public static void DeleteAllLogFiles()
        {
            string folderPath = Settings.LogFolderPath;

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                Debug.LogWarning("[PlayModeLogSaver] 삭제할 로그 폴더가 없음");
                return;
            }

            string[] files = Directory.GetFiles(folderPath, $"*{LOG_FILE_EXTENSION}");
            int deletedCount = 0;

            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);

                    // .meta 파일도 삭제
                    string metaFile = file + ".meta";
                    if (File.Exists(metaFile))
                    {
                        File.Delete(metaFile);
                    }

                    deletedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PlayModeLogSaver] 파일 삭제 실패: {file}\n{e.Message}");
                }
            }

            Debug.Log($"[PlayModeLogSaver] {deletedCount}개 로그 파일 삭제됨");

            if (folderPath.StartsWith("Assets"))
            {
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 설정된 폴더의 로그 파일 목록을 반환합니다.
        /// </summary>
        public static string[] GetLogFiles()
        {
            string folderPath = Settings.LogFolderPath;

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return Array.Empty<string>();
            }

            string[] files = Directory.GetFiles(folderPath, $"*{LOG_FILE_EXTENSION}");
            Array.Sort(files);
            Array.Reverse(files); // 최신 파일이 먼저 오도록

            return files;
        }

        /// <summary>
        /// 가장 최근 로그 파일의 경로를 반환합니다.
        /// </summary>
        public static string GetLatestLogFilePath()
        {
            string[] files = GetLogFiles();
            return files.Length > 0 ? files[0] : null;
        }

        #endregion

        #region 내부 클래스

        // 로그 엔트리 구조체
        private struct LogEntry
        {
            public DateTime Time;
            public LogType Type;
            public string Message;
            public string StackTrace;
        }

        #endregion
    }
}
