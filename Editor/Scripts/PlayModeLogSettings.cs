using ActionFit.SOSingleton;
using UnityEngine;

namespace CustomPackage.PlayModeLogSaver
{
    /// <summary>
    /// 플레이 모드 로그 저장 설정을 담는 ScriptableObject.
    /// 로그 저장 폴더 경로와 활성화 여부를 저장합니다.
    /// </summary>
    [ActionFitSettingsAsset(
        "PlayModeLogSaver",
        ActionFitSettingsAssetLifetime.EditorOnly,
        LegacyPaths = new string[]
        {
            "Assets/Editor/PlayModeLogSaver/PlayModeLogSettings.asset"
        })]
    public class PlayModeLogSettings : ScriptableObject
    {
        [SerializeField] private string _logFolderPath = "Assets/Logs"; // 로그 저장 폴더 경로
        [SerializeField] private bool _isEnabled = true; // 로그 저장 활성화 여부
        [SerializeField] private bool _includeStackTrace = true; // 스택 트레이스 포함 여부
        [SerializeField] private bool _logOnlyErrorsAndWarnings = false; // Error/Warning만 저장

        // 로그 저장 폴더 경로
        public string LogFolderPath
        {
            get => _logFolderPath;
            set => _logFolderPath = value;
        }

        // 로그 저장 활성화 여부
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        // 스택 트레이스 포함 여부
        public bool IncludeStackTrace
        {
            get => _includeStackTrace;
            set => _includeStackTrace = value;
        }

        // Error/Warning만 저장 여부
        public bool LogOnlyErrorsAndWarnings
        {
            get => _logOnlyErrorsAndWarnings;
            set => _logOnlyErrorsAndWarnings = value;
        }
    }
}
