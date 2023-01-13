#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;


public class AutoSave : EditorWindow
{
   private readonly GUIStyle _guiStyleLabel = new GUIStyle();

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

   private int BackupCount => EditorPrefs.GetInt("PM_AS_BACKUPCOUNT");

   private string Path
   {
      get
      {
         string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
         path = path.Substring(0, path.LastIndexOf('/'));
         path = path.Substring(0, path.LastIndexOf('/') + 1);
         return path + "Textures/";
      }
   }

   private static void AutosaveOff()
   {
      EditorApplication.update -= EditorUpdate;
      EditorApplication.playModeStateChanged -= OnEnterInPlayMode;
      _autoSave = false;
      Log(0, "AutoSave - OFF !");
   }

   private static void AutosaveOn()
   {
      _lastAutosave = DateTime.Now;
      EditorApplication.update += EditorUpdate;
      EditorApplication.playModeStateChanged += OnEnterInPlayMode;
      _autoSave = true;
      Log(0, "AutoSave - ON !");
   }

   private static void EditorUpdate()
   {
      if (_lastAutosave.AddMinutes(_saveInterval) > DateTime.Now) return;

      for (int i = 0; i < EditorSceneManager.sceneCount; i++)
      {
         var scene = EditorSceneManager.GetSceneAt(i);

         if (scene.isDirty)
         {
            Save();
            _lastAutosave = DateTime.Now;
            break;
         }
      }
      
      // if (!SceneManager.GetActiveScene().isDirty) return;
   }

   private void LoadSettings()
   {
      _autoSave = EditorPrefs.GetBool("PM_AS_AUTOSAVE", true);
      _saveOnPlay = EditorPrefs.GetBool("PM_AS_SAVEONPLAY", true);
      _saveAssets = EditorPrefs.GetBool("PM_AS_SAVEASSET", true);
      _debugLog = EditorPrefs.GetBool("PM_AS_DEBUGLOG", true);
      _saveInterval = EditorPrefs.GetInt("PM_AS_SAVEINTERVAL", 2);
      _saveIntervalSlider = EditorPrefs.GetInt("PM_AS_SAVEINTERVAL", 2);
      _backup = EditorPrefs.GetBool("PM_AS_BACKUP", true);
      _backupPath = EditorPrefs.GetString("PM_AS_BACKUPPATH", "Backup/Test");
      _backupCount = EditorPrefs.GetInt("PM_AS_BACKUPCOUNT", 10);
   }

   private static void OnEnterInPlayMode(PlayModeStateChange state)
   {
      if (_saveOnPlay && !EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
         Save();
   }

   [MenuItem("Tools/ProjectMakers/AutoSave")]
   private static void OpenWindow()
   {
      GetWindow<AutoSave>("AutoSave");
   }

   private static void Save()
   {
      Scene activeScene = SceneManager.GetActiveScene();
      
      try
      {
         for (int i = 0; i < EditorSceneManager.sceneCount; i++)
         {
            var scene = EditorSceneManager.GetSceneAt(i);
            
            EditorSceneManager.SaveScene(scene);
         }
         
         // EditorSceneManager.SaveScene(activeScene);

         if (_saveAssets)
            AssetDatabase.SaveAssets();

         Log(0, $"AutoSave - {activeScene.name} is saved!");
      }
      catch (Exception e)
      {
         Log(2, $"AutoSave - {activeScene.name} is !! NOT !! saved!\nException: {e}");
      }

      if (!_backup) return;

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
         Log(0, $"AutoSave - Create a backup for - {curSceneName} - Scene!");
         ClearBackupFolder(path);
      }
      catch (Exception e)
      {
         Log(2, $"AutoSave - Create a !! NO !! backup for -{curSceneName} - Scene!\nException: {e}");
      }
   }

   private static void ClearBackupFolder(string path)
   {
      var fileInfo = new DirectoryInfo(path).GetFiles();

      // The *2 because each file has a ".meta" file 
      if (fileInfo.Length >= _backupCount * 2)
      {
         var dir = fileInfo.OrderByDescending(x => x.LastWriteTime).Skip(_backupCount * 2);

         foreach (var fi in dir)
            fi.Delete();

         AssetDatabase.Refresh();
      }
   }

   private static string BackupFileName(string curSceneName)
   {
      var filename = DateTime.Now.Ticks.ToString();
      filename = filename.Remove(filename.Length - 6);
      filename = filename.Remove(0, 3);
      filename = int.Parse(filename).ToString("#,###");
      filename = filename.Replace(",", ".");
      filename = curSceneName + " v." + filename + ".unity";
      return filename;
   }

   private void SaveSettings()
   {
      EditorPrefs.SetBool("PM_AS_AUTOSAVE", _autoSave);
      EditorPrefs.SetBool("PM_AS_SAVEONPLAY", _saveOnPlay);
      EditorPrefs.SetBool("PM_AS_SAVEASSET", _saveAssets);
      EditorPrefs.SetBool("PM_AS_DEBUGLOG", _debugLog);
      EditorPrefs.SetBool("PM_AS_BACKUP", _backup);
      EditorPrefs.SetInt("PM_AS_SAVEINTERVAL", _saveInterval);
      EditorPrefs.SetString("PM_AS_BACKUPPATH", _backupPath);
      EditorPrefs.SetInt("PM_AS_BACKUPCOUNT", _backupCount);
   }

   private void ResetSettings()
   {
      EditorPrefs.DeleteKey("PM_AS_AUTOSAVE");
      EditorPrefs.DeleteKey("PM_AS_SAVEONPLAY");
      EditorPrefs.DeleteKey("PM_AS_SAVEASSET");
      EditorPrefs.DeleteKey("PM_AS_DEBUGLOG");
      EditorPrefs.DeleteKey("PM_AS_BACKUP");
      EditorPrefs.DeleteKey("PM_AS_SAVEINTERVAL");
      EditorPrefs.DeleteKey("PM_AS_BACKUPPATH");
      EditorPrefs.DeleteKey("PM_AS_BACKUPCOUNT");
   }

   private static void Log(int type, string body)
   {
      if (_debugLog)
      {
         switch (type)
         {
            case 1:
               Debug.LogWarning(body);
               break;
            case 2:
               Debug.LogError(body);
               break;
            default:
               Debug.Log(body);
               break;
         }
      }
   }

   private void OnEnable()
   {
      LoadSettings();

      if (_autoSave)
         AutosaveOn();
   }

   private void OnGUI()
   {
      if (_saveInterval != _saveIntervalSlider)
      {
         _saveInterval = _saveIntervalSlider;
         Log(0, "AutoSave - Saveinterval = " + _saveInterval + " min!");
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
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      GUILayout.Space(10);
      GUILayout.Label(_debugLog ? AsEnable : AsDisable, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
      GUILayout.EndHorizontal();

      if (GUILayout.Button(new GUIContent(AsInfo, "Create Debug.Log"), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28)))
      {
         _debugLog = !_debugLog;
         Log(0, "AutoSave - Debug.Log = " + _debugLog + " !");
         SaveSettings();
      }

      GUILayout.EndVertical();
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      GUILayout.Space(10);
      GUILayout.Label(_saveOnPlay ? AsEnable : AsDisable, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
      GUILayout.EndHorizontal();

      if (GUILayout.Button(new GUIContent(AsOnPlay, "Save on Play"), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28)))
      {
         _saveOnPlay = !_saveOnPlay;
         Log(0, "AutoSave - Save on Play = " + _saveOnPlay + " !");
         SaveSettings();
      }

      GUILayout.EndVertical();
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      GUILayout.Space(10);
      GUILayout.Label(_saveAssets ? AsEnable : AsDisable, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
      GUILayout.EndHorizontal();

      if (GUILayout.Button(new GUIContent(AsAsset, "Save Assets"), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28)))
      {
         _saveAssets = !_saveAssets;
         Log(0, "AutoSave - Save Assets = " + _saveAssets + " !");
         SaveSettings();
      }

      GUILayout.EndVertical();
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
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal();
      GUILayout.Space(10);
      GUILayout.Label(_autoSave ? AsEnable : AsDisable, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
      GUILayout.EndHorizontal();

      if (GUILayout.Button(new GUIContent(AsOnOff, "AutoSave ON/OFF"), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28)))
      {
         if (_autoSave)
            AutosaveOff();
         else
            AutosaveOn();

         SaveSettings();
      }

      GUILayout.EndVertical();
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
         GUILayout.BeginVertical();
         GUILayout.BeginHorizontal();
         GUILayout.Space(10);
         GUILayout.Label(_backup ? AsEnable : AsDisable, GUILayout.MaxHeight(16), GUILayout.MaxWidth(16));
         GUILayout.EndHorizontal();

         if (GUILayout.Button(new GUIContent(AsBackup, "Save Project in backupfolder"), GUILayout.MaxHeight(28), GUILayout.MaxWidth(28)))
         {
            _backup = !_backup;
            Log(0, "AutoSave - Backup = " + _backup + " !");
            SaveSettings();
         }

         GUILayout.EndVertical();
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
         EditorPrefs.SetInt("PM_AS_BACKUPCOUNT", EditorGUILayout.IntField("Backup count: ", BackupCount, GUILayout.MinWidth(EditorGUIUtility.labelWidth + countSize.x + 8), GUILayout.MaxWidth(EditorGUIUtility.labelWidth + countSize.x + 8)));

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

      GUILayout.Label("This window must be open for automatic saving!\nThe window does not have to be in the foreground, but don't close it.", _guiStyleLabel);

      GUILayout.FlexibleSpace();
      GUILayout.BeginHorizontal();

      if (GUILayout.Button("ProjectMakers.de", EditorStyles.toolbarButton))
         Application.OpenURL("https://projectmakers.de");

      GUILayout.EndHorizontal();
   }
}
#endif