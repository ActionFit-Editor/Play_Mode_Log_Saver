using System.IO;
using UnityEditor;
using UnityEngine;

namespace CustomPackage.PlayModeLogSaver
{
    /// <summary>
    /// 플레이 모드 로그 저장 설정을 관리하는 에디터 윈도우.
    /// 폴더 드래그앤드롭 설정, 로그 파일 관리 기능을 제공합니다.
    /// </summary>
    public class PlayModeLogSaverWindow : EditorWindow
    {
        #region Fields

        // 스크롤 위치
        private Vector2 _scrollPosition;

        // 로그 파일 목록 캐시
        private string[] _logFiles;

        // 선택된 로그 파일 인덱스
        private int _selectedLogIndex = -1;

        // 로그 파일 내용 미리보기
        private string _logPreview;

        // 미리보기 스크롤 위치
        private Vector2 _previewScrollPosition;

        // 폴드아웃 상태
        private bool _showSettings = true;
        private bool _showLogFiles = true;
        private bool _showPreview = false;

        #endregion

        #region 메뉴

        [MenuItem("Tools/ActionFit/Play Mode Log Saver", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayModeLogSaverWindow>("Play Mode Log Saver");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        #endregion

        #region Unity 콜백

        private void OnEnable()
        {
            RefreshLogFiles();
        }

        private void OnFocus()
        {
            RefreshLogFiles();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawSettingsSection();
            EditorGUILayout.Space(10);

            DrawManualSaveSection();
            EditorGUILayout.Space(10);

            DrawLogFilesSection();
            EditorGUILayout.Space(10);

            DrawPreviewSection();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region UI 그리기

        // 헤더
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.FlexibleSpace();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Play Mode Log Saver", titleStyle, GUILayout.Height(30));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "플레이 모드 종료 시 콘솔 로그를 자동으로 txt 파일로 저장합니다.\n" +
                "컴파일 에러 등 에디터 로그는 [콘솔 로그 저장] 버튼으로 수동 저장할 수 있습니다.",
                MessageType.Info);
        }

        // 설정 섹션
        private void DrawSettingsSection()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "설정", true, EditorStyles.foldoutHeader);

            if (!_showSettings) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var settings = PlayModeLogSaver.Settings;

            // 활성화 토글
            EditorGUI.BeginChangeCheck();
            bool isEnabled = EditorGUILayout.Toggle("로그 저장 활성화", settings.IsEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Toggle Log Saver");
                settings.IsEnabled = isEnabled;
                EditorUtility.SetDirty(settings);
            }

            EditorGUILayout.Space(5);

            // 폴더 경로 드래그앤드롭 영역
            DrawFolderDropArea(settings);

            EditorGUILayout.Space(5);

            // 옵션들
            EditorGUI.BeginChangeCheck();

            bool includeStackTrace = EditorGUILayout.Toggle("스택 트레이스 포함", settings.IncludeStackTrace);
            bool logOnlyErrors = EditorGUILayout.Toggle("Error/Warning만 저장", settings.LogOnlyErrorsAndWarnings);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(settings, "Change Log Settings");
                settings.IncludeStackTrace = includeStackTrace;
                settings.LogOnlyErrorsAndWarnings = logOnlyErrors;
                EditorUtility.SetDirty(settings);
            }

            EditorGUILayout.Space(5);

            // 현재 상태 표시
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("현재 수집된 로그:", GUILayout.Width(120));
            EditorGUILayout.LabelField($"{PlayModeLogSaver.LogCount}개", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // 폴더 드래그앤드롭 영역
        private void DrawFolderDropArea(PlayModeLogSettings settings)
        {
            EditorGUILayout.LabelField("저장 폴더 (드래그앤드롭):");

            // 드롭 영역
            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));

            // 배경 색상
            Color bgColor = string.IsNullOrEmpty(settings.LogFolderPath)
                ? new Color(1f, 0.5f, 0.5f, 0.3f)  // 빨간색 (미설정)
                : new Color(0.5f, 1f, 0.5f, 0.3f); // 초록색 (설정됨)

            EditorGUI.DrawRect(dropArea, bgColor);

            // 테두리
            Handles.DrawSolidRectangleWithOutline(dropArea, Color.clear, Color.gray);

            // 텍스트
            GUIStyle centerStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            string displayPath = string.IsNullOrEmpty(settings.LogFolderPath)
                ? "폴더를 여기에 드래그앤드롭하세요"
                : settings.LogFolderPath;

            EditorGUI.LabelField(dropArea, displayPath, centerStyle);

            // 드래그앤드롭 이벤트 처리
            Event evt = Event.current;
            if (dropArea.Contains(evt.mousePosition))
            {
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                        if (IsFolderDrag())
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            evt.Use();
                        }
                        break;

                    case EventType.DragPerform:
                        if (IsFolderDrag())
                        {
                            DragAndDrop.AcceptDrag();

                            string path = DragAndDrop.paths[0];

                            Undo.RecordObject(settings, "Set Log Folder");
                            settings.LogFolderPath = path;
                            EditorUtility.SetDirty(settings);

                            RefreshLogFiles();

                            Debug.Log($"[PlayModeLogSaver] 로그 폴더 설정됨: {path}");
                            evt.Use();
                        }
                        break;
                }
            }

            // 폴더 선택 버튼
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("폴더 선택...", GUILayout.Height(25)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel(
                    "로그 저장 폴더 선택",
                    string.IsNullOrEmpty(settings.LogFolderPath) ? "Assets" : settings.LogFolderPath,
                    "");

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // Assets 폴더 기준 상대 경로로 변환
                    string dataPath = Application.dataPath;
                    if (selectedPath.StartsWith(dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(dataPath.Length);
                    }

                    Undo.RecordObject(settings, "Set Log Folder");
                    settings.LogFolderPath = selectedPath;
                    EditorUtility.SetDirty(settings);

                    RefreshLogFiles();
                }
            }

            if (GUILayout.Button("Explorer에서 열기", GUILayout.Height(25)))
            {
                if (!string.IsNullOrEmpty(settings.LogFolderPath) && Directory.Exists(settings.LogFolderPath))
                {
                    EditorUtility.RevealInFinder(settings.LogFolderPath);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // 수동 저장 섹션
        private void DrawManualSaveSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("수동 저장", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // 현재 콘솔 로그 개수 표시
            int consoleLogCount = PlayModeLogSaver.GetConsoleLogCount();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("현재 콘솔 로그:", GUILayout.Width(100));

            GUIStyle countStyle = new GUIStyle(EditorStyles.boldLabel);
            if (consoleLogCount > 0)
            {
                countStyle.normal.textColor = new Color(0.2f, 0.6f, 1f);
            }
            EditorGUILayout.LabelField($"{consoleLogCount}개", countStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 수동 저장 버튼
            GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
            GUI.enabled = consoleLogCount > 0 && !string.IsNullOrEmpty(PlayModeLogSaver.Settings.LogFolderPath);

            if (GUILayout.Button("현재 콘솔 로그 저장", GUILayout.Height(35)))
            {
                string savedPath = PlayModeLogSaver.SaveConsoleLogsToFile();
                if (!string.IsNullOrEmpty(savedPath))
                {
                    RefreshLogFiles();

                    // 저장된 파일 선택
                    _selectedLogIndex = 0;
                    LoadLogPreview(savedPath);
                    _showPreview = true;

                    EditorUtility.DisplayDialog(
                        "저장 완료",
                        $"콘솔 로그가 저장되었습니다.\n\n{Path.GetFileName(savedPath)}",
                        "확인");
                }
            }

            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            if (string.IsNullOrEmpty(PlayModeLogSaver.Settings.LogFolderPath))
            {
                EditorGUILayout.HelpBox("먼저 저장 폴더를 설정해주세요.", MessageType.Warning);
            }
            else if (consoleLogCount == 0)
            {
                EditorGUILayout.HelpBox("저장할 콘솔 로그가 없습니다.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        // 로그 파일 목록 섹션
        private void DrawLogFilesSection()
        {
            _showLogFiles = EditorGUILayout.Foldout(_showLogFiles, $"저장된 로그 파일 ({_logFiles?.Length ?? 0}개)", true, EditorStyles.foldoutHeader);

            if (!_showLogFiles) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 버튼들
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("새로고침", GUILayout.Height(25)))
            {
                RefreshLogFiles();
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("모든 로그 삭제", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "로그 파일 삭제",
                    "설정된 폴더의 모든 로그 파일을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                    "삭제",
                    "취소"))
                {
                    PlayModeLogSaver.DeleteAllLogFiles();
                    RefreshLogFiles();
                    _selectedLogIndex = -1;
                    _logPreview = null;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 로그 파일 목록
            if (_logFiles == null || _logFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("저장된 로그 파일이 없습니다.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < _logFiles.Length && i < 20; i++) // 최대 20개 표시
                {
                    string fileName = Path.GetFileName(_logFiles[i]);
                    bool isSelected = (_selectedLogIndex == i);

                    EditorGUILayout.BeginHorizontal();

                    // 선택 버튼
                    GUI.backgroundColor = isSelected ? new Color(0.5f, 0.8f, 1f) : Color.white;
                    if (GUILayout.Button(fileName, GUILayout.Height(22)))
                    {
                        _selectedLogIndex = i;
                        LoadLogPreview(_logFiles[i]);
                        _showPreview = true;
                    }
                    GUI.backgroundColor = Color.white;

                    // 파일 열기 버튼
                    if (GUILayout.Button("열기", GUILayout.Width(40), GUILayout.Height(22)))
                    {
                        System.Diagnostics.Process.Start(_logFiles[i]);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (_logFiles.Length > 20)
                {
                    EditorGUILayout.HelpBox($"외 {_logFiles.Length - 20}개 파일...", MessageType.None);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // 미리보기 섹션
        private void DrawPreviewSection()
        {
            if (_selectedLogIndex < 0 || string.IsNullOrEmpty(_logPreview)) return;

            _showPreview = EditorGUILayout.Foldout(_showPreview, "로그 미리보기", true, EditorStyles.foldoutHeader);

            if (!_showPreview) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 미리보기 스크롤 영역
            _previewScrollPosition = EditorGUILayout.BeginScrollView(
                _previewScrollPosition,
                GUILayout.Height(300));

            GUIStyle textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = false
            };

            EditorGUILayout.TextArea(_logPreview, textStyle, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 유틸리티

        // 폴더 드래그인지 확인
        private bool IsFolderDrag()
        {
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length != 1)
                return false;

            string path = DragAndDrop.paths[0];
            return Directory.Exists(path);
        }

        // 로그 파일 목록 새로고침
        private void RefreshLogFiles()
        {
            _logFiles = PlayModeLogSaver.GetLogFiles();
            Repaint();
        }

        // 로그 파일 미리보기 로드
        private void LoadLogPreview(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logPreview = "파일을 찾을 수 없습니다.";
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);

                // 너무 긴 경우 잘라내기
                if (content.Length > 50000)
                {
                    content = content.Substring(0, 50000) + "\n\n... (내용이 너무 길어 생략됨)";
                }

                _logPreview = content;
            }
            catch (System.Exception e)
            {
                _logPreview = $"파일 읽기 오류: {e.Message}";
            }
        }

        #endregion
    }
}
