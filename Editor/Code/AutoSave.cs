#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

namespace PM.Tools
{
   public class AutoSave : EditorWindow
   {
      private readonly GUIStyle _guiStyleLabel = new GUIStyle();

      private const string LOGColor = "#CC0000";
      private const int LOGSize = 14;

      private const string EditorPrefPrefix = "PM_AS_";
      private const string AutoSaveKey = EditorPrefPrefix + "AUTOSAVE";
      private const string SaveOnPlayKey = EditorPrefPrefix + "SAVEONPLAY";
      private const string SaveAssetsKey = EditorPrefPrefix + "SAVEASSET";
      private const string DebugLogKey = EditorPrefPrefix + "DEBUGLOG";
      private const string SaveIntervalKey = EditorPrefPrefix + "SAVEINTERVAL";
      private const string BackupKey = EditorPrefPrefix + "BACKUP";
      private const string BackupPathKey = EditorPrefPrefix + "BACKUPPATH";
      private const string BackupCountKey = EditorPrefPrefix + "BACKUPCOUNT";

      private static bool _autoSave;
      private static bool _saveOnPlay;
      private static bool _saveAssets;
      private static bool _debugLog;
      private static bool _backup;
      private static bool _showBackup;

      private static DateTime _lastAutosave = DateTime.Now;

      private static int _saveInterval = 5;
      private static int _saveIntervalSlider = 5;

      private static string _backupPath;
      private static int _backupCount;

      private Texture2D AsAsset => AssetDatabase.LoadAssetAtPath(Path + "asset.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsDisable => AssetDatabase.LoadAssetAtPath(Path + "disable.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsEnable => AssetDatabase.LoadAssetAtPath(Path + "enable.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsInfo => AssetDatabase.LoadAssetAtPath(Path + "info.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsOnOff => AssetDatabase.LoadAssetAtPath(Path + "onoff.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsOnPlay => AssetDatabase.LoadAssetAtPath(Path + "play.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsTime => AssetDatabase.LoadAssetAtPath(Path + "time.png", typeof(Texture2D)) as Texture2D;
      private Texture2D AsBackup => AssetDatabase.LoadAssetAtPath(Path + "backup.png", typeof(Texture2D)) as Texture2D;
      private Texture2D PmLogo => AssetDatabase.LoadAssetAtPath(Path + "logo.png", typeof(Texture2D)) as Texture2D;

      private static int BackupCount => EditorPrefs.GetInt(BackupCountKey);

      private string Path
      {
         get
         {
            string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            path = path[..path.LastIndexOf('/')];
            path = path[..(path.LastIndexOf('/') + 1)];
            return path + "Textures/";
         }
      }

      private static void AutosaveOff()
      {
         EditorApplication.update -= EditorUpdate;
         EditorApplication.playModeStateChanged -= OnEnterInPlayMode;
         _autoSave = false;
         Log(0, "OFF !");
      }

      private static void AutosaveOn()
      {
         _lastAutosave = DateTime.Now;
         EditorApplication.update += EditorUpdate;
         EditorApplication.playModeStateChanged += OnEnterInPlayMode;
         _autoSave = true;
         Log(0, "ON !");
      }

      private static void EditorUpdate()
      {
         if (_lastAutosave.AddMinutes(_saveInterval) > DateTime.Now) return;

         for (int i = 0; i < SceneManager.sceneCount; i++)
         {
            var scene = SceneManager.GetSceneAt(i);

            if (scene.isDirty)
            {
               Save();
               _lastAutosave = DateTime.Now;
               break;
            }
         }
      }

      private static void LoadSettings()
      {
         _autoSave = EditorPrefs.GetBool(AutoSaveKey, true);
         _saveOnPlay = EditorPrefs.GetBool(SaveOnPlayKey, true);
         _saveAssets = EditorPrefs.GetBool(SaveAssetsKey, true);
         _debugLog = EditorPrefs.GetBool(DebugLogKey, true);
         _saveInterval = EditorPrefs.GetInt(SaveIntervalKey, 2);
         _saveIntervalSlider = EditorPrefs.GetInt(SaveIntervalKey, 2);
         _backup = EditorPrefs.GetBool(BackupKey, true);
         _backupPath = EditorPrefs.GetString(BackupPathKey, "_project/AutoSave");
         _backupCount = EditorPrefs.GetInt(BackupCountKey, 10);
      }

      private static void SaveSettings()
      {
         EditorPrefs.SetBool(AutoSaveKey, _autoSave);
         EditorPrefs.SetBool(SaveOnPlayKey, _saveOnPlay);
         EditorPrefs.SetBool(SaveAssetsKey, _saveAssets);
         EditorPrefs.SetBool(DebugLogKey, _debugLog);
         EditorPrefs.SetBool(BackupKey, _backup);
         EditorPrefs.SetInt(SaveIntervalKey, _saveInterval);
         EditorPrefs.SetString(BackupPathKey, _backupPath);
         EditorPrefs.SetInt(BackupCountKey, _backupCount);
      }

      private static void OnEnterInPlayMode(PlayModeStateChange state)
      {
         if (_saveOnPlay && !EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            Save();
      }

      [InitializeOnLoadMethod]
      private static void OpenWindowOnStart()
      {
          if (SessionState.GetBool("PM_AUTOSAVE_FIRSTSTART", false)) return;
      
          var existingWindow = GetWindow<AutoSave>(false, "AutoSave", false);
          if (existingWindow != null) return;
      
          OpenWindow();
          SessionState.SetBool("PM_AUTOSAVE_FIRSTSTART", true);
      }

      [MenuItem("Tools/ProjectMakers/AutoSave")]
      private static void OpenWindow()
      {
         Debug.Log($"Opening AutoSave window...");
         GetWindow<AutoSave>("AutoSave");
      }

      private static void Save()
      {
         Scene activeScene = SceneManager.GetActiveScene();
         SaveAllDirtyScenes();

         if (_saveAssets)
            AssetDatabase.SaveAssets();

         Log(0, $"Scene '{activeScene.name}' has been saved.");

         if (!_backup)
            return;

         BackupActiveScene(activeScene);
      }

      private static void SaveAllDirtyScenes()
      {
         for (int i = 0; i < SceneManager.sceneCount; i++)
         {
            var scene = SceneManager.GetSceneAt(i);

            if (!scene.isDirty) continue;

            try
            {
               EditorSceneManager.SaveScene(scene);
            }
            catch (Exception e)
            {
               Log(2, $"Error occurred while saving scene '{scene.name}'.\nException: {e}");
            }
         }
      }

      private static void BackupActiveScene(Scene activeScene)
      {
         var username = SystemInfo.deviceName;
         var curSceneName = activeScene.name;
         var fileName = BackupFileName(curSceneName);
         var path = System.IO.Path.Combine("Assets/", _backupPath, username, curSceneName);
         var filePath = System.IO.Path.Combine(path, fileName);

         if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

         try
         {
            EditorSceneManager.SaveScene(activeScene, filePath, true);
            Log(0, $"Backup created for scene '{curSceneName}'.");
            ClearBackupFolder(path);
         }
         catch (Exception e)
         {
            Log(2, $"Error occurred while creating a backup for scene '{curSceneName}'.\nException: {e}");
         }
      }

      private static void ClearBackupFolder(string path)
      {
         var fileInfo = new DirectoryInfo(path).GetFiles("*.unity");

         if (fileInfo.Length > _backupCount)
         {
            var oldestUnityFile = fileInfo.OrderBy(x => x.LastWriteTime).First();
            var metaFilePath = $"{oldestUnityFile.FullName}.meta";

            oldestUnityFile.Delete();

            if (File.Exists(metaFilePath))
               File.Delete(metaFilePath);

            AssetDatabase.Refresh();
         }
      }

      private static string BackupFileName(string curSceneName)
      {
         var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
         var filename = $"{curSceneName} v.{timestamp}.unity";
         return filename;
      }

      private static void Log(int type, string body)
      {
         if (!_debugLog) return;

         switch (type)
         {
            case 1:
               Debug.LogWarning($"<color={LOGColor}><size={LOGSize}><b>PM - Autosave: </b></size></color>{body}");
               break;
            case 2:
               Debug.LogError($"<color={LOGColor}><size={LOGSize}><b>PM - Autosave: </b></size></color>{body}");
               break;
            default:
               Debug.Log($"<color={LOGColor}><size={LOGSize}><b>PM - Autosave: </b></size></color>{body}");
               break;
         }
      }

      private void OnEnable()
      {
         LoadSettings();

         if (_autoSave)
            AutosaveOn();
         else
            AutosaveOff();
      }

      private void OnGUI()
      {
         EditorGUI.BeginChangeCheck();

         if (_saveInterval != _saveIntervalSlider)
         {
            _saveInterval = _saveIntervalSlider;
            Log(0, "Saveinterval = " + _saveInterval + " min!");
         }

         GUILayout.Space(20);
         GUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();
         GUILayout.Label(PmLogo);
         GUILayout.FlexibleSpace();
         GUILayout.EndHorizontal();
         GUILayout.Space(10);
         EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
         GUILayout.Space(10);
         GUILayout.BeginHorizontal();
         DrawButton(ref _debugLog, AsInfo, "Create Debug.Log", "Debug.Log");
         DrawButton(ref _saveOnPlay, AsOnPlay, "Save on Play", "Save on Play");
         DrawButton(ref _saveAssets, AsAsset, "Save Assets", "Save Assets");
         GUILayout.FlexibleSpace();
         GUILayout.BeginVertical();
         GUILayout.BeginHorizontal();
         GUILayout.Space(10);
         GUILayout.Label(string.Empty, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
         GUILayout.EndHorizontal();
         GUILayout.Space(2);
         GUILayout.BeginHorizontal();

         _saveIntervalSlider = EditorGUILayout.IntSlider(string.Empty, _saveIntervalSlider, 1, 30);

         GUILayout.BeginVertical();
         GUILayout.Space(-4);
         EditorGUILayout.LabelField(new GUIContent(AsTime, "Save interval in minutes"), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28));
         GUILayout.EndVertical();
         GUILayout.EndHorizontal();
         GUILayout.EndVertical();
         DrawButton(ref _autoSave, AsOnOff, "AutoSave ON/OFF", "AutoSave");
         GUILayout.EndHorizontal();
         GUILayout.Space(10);
         GUILayout.BeginHorizontal();

         GUI.skin.button.normal.textColor = Color.white;
         GUI.backgroundColor = new Color(0.63f, 0f, 0f);

         if (GUILayout.Button("Save it manually!", EditorStyles.toolbarButton))
            Save();

         if (GUILayout.Button("Change backup settings...", EditorStyles.toolbarButton))
            _showBackup = !_showBackup;

         GUI.skin.button.normal.textColor = Color.black;
         GUI.backgroundColor = Color.white;
         GUILayout.EndHorizontal();

         if (_showBackup)
         {
            GUILayout.BeginHorizontal();
            DrawButton(ref _backup, AsBackup, "Save Project in backupfolder", "Backup");
            GUILayout.BeginVertical();
            GUILayout.Space(24);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            Vector2 pathSize = GUI.skin.box.CalcSize(new GUIContent(_backupPath));
            EditorGUIUtility.labelWidth = 115;

            _backupPath = EditorGUILayout.TextField("Backup save path: ", _backupPath, GUILayout.MinWidth(EditorGUIUtility.labelWidth + pathSize.x + 15));

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Space(24);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);

            Vector2 countSize = GUI.skin.box.CalcSize(new GUIContent(BackupCount.ToString()));
            EditorGUIUtility.labelWidth = 88;
            EditorPrefs.SetInt(BackupCountKey, EditorGUILayout.IntField("Backup count: ", BackupCount, GUILayout.MinWidth(EditorGUIUtility.labelWidth + countSize.x + 8), GUILayout.MaxWidth(EditorGUIUtility.labelWidth + countSize.x + 8)));

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("The backup directory is created in your project directory in \"Assets/your specified path\".", _guiStyleLabel);
         }

         GUILayout.Space(10);
         _guiStyleLabel.fontSize = 12;
         _guiStyleLabel.fontStyle = FontStyle.Italic;
         _guiStyleLabel.normal.textColor = new Color(.58f, .58f, .58f);

         GUILayout.FlexibleSpace();
         GUILayout.BeginHorizontal();

         if (GUILayout.Button("ProjectMakers.de", EditorStyles.toolbarButton))
            Application.OpenURL("https://projectmakers.de");

         GUILayout.EndHorizontal();

         if (EditorGUI.EndChangeCheck())
            SaveSettings();
      }

      private void DrawButton(ref bool buttonState, Texture buttonInfoText, string buttonText, string logMessage)
      {
         GUILayout.BeginVertical();
         GUILayout.BeginHorizontal();
         GUILayout.Space(10);
         GUILayout.Label(buttonState ? AsEnable : AsDisable, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
         GUILayout.EndHorizontal();

         if (GUILayout.Button(new GUIContent(buttonInfoText, buttonText), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28)))
         {
            buttonState = !buttonState;
            Log(0, $"{logMessage} = {buttonState} !");
            SaveSettings();
         }

         GUILayout.EndVertical();
      }
   }
}
#endif
