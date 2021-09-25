using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

namespace CSharpScriptPicker
{
    [Tool]
    public class ScriptPickerPlugin : EditorInspectorPlugin
    {
        public EditorInterface EditorInterface { get; set; }

        const string SettingsPrefix = "csharp_script_picker/";
        const string SortByNamespaceSettingName = "use_namespaces";
        const string SortByNamespaceSettingPath = SettingsPrefix + SortByNamespaceSettingName;
        
        const string LogErrorsAndWarningsSettingName = "enable_logging";
        const string LogErrorsAndWarningsSettingPath = SettingsPrefix + LogErrorsAndWarningsSettingName;
        
        const string IgnoredTypeSettingName = "ignored_types";
        const string IgnoredTypeSettingPath = SettingsPrefix + IgnoredTypeSettingName;
        
        const string IgnoredPathsSettingName = "ignored_folder_paths";
        const string IgnoredPathsSettingPath = SettingsPrefix + IgnoredPathsSettingName;

        public static void RegisterSettings()
        {
            AddBoolSetting(SortByNamespaceSettingName, SortByNamespaceSettingPath, true, true);
            AddBoolSetting(LogErrorsAndWarningsSettingName, LogErrorsAndWarningsSettingPath, false, true);
            AddStringArraySetting(IgnoredTypeSettingName, IgnoredTypeSettingPath, new string[0]);
            AddStringArraySetting(IgnoredPathsSettingName, IgnoredPathsSettingPath, new[] { "res://addons/*" });
            
            void AddBoolSetting(string name, string path, bool defaultValue, bool initialValue)
            {
                var propInfoDict = new Dictionary();
                propInfoDict["name"] = name;
                propInfoDict["type"] = Variant.Type.Bool;
                ProjectSettings.SetSetting(path, defaultValue);
                ProjectSettings.AddPropertyInfo(propInfoDict);
                ProjectSettings.SetInitialValue(path, initialValue);
            }

            void AddStringArraySetting(string name, string path, string[] defaultValue)
            {
                var propInfoDict = new Dictionary();
                propInfoDict["name"] = name;
                propInfoDict["type"] = Variant.Type.StringArray;
                ProjectSettings.SetSetting(path, defaultValue);
                ProjectSettings.AddPropertyInfo(propInfoDict);
                ProjectSettings.SetInitialValue(path, new string[0]);
            }
        }

        public static bool ShouldSortByNamespace
        {
            get
            {
                if (ProjectSettings.HasSetting(SortByNamespaceSettingPath))
                {
                    return (bool)ProjectSettings.GetSetting(SortByNamespaceSettingPath);
                }
                return true;
            }
        }

        public static bool EnableLogging
        {
            get
            {
                if (ProjectSettings.HasSetting(LogErrorsAndWarningsSettingPath))
                {
                    return (bool)ProjectSettings.GetSetting(LogErrorsAndWarningsSettingPath);
                }
                return true;
            }
        }

        static string[] GetSettingsStringArray(string path)
        {
            if (ProjectSettings.HasSetting(path))
            {
                var settingsValue = ProjectSettings.GetSetting(path);
                if (settingsValue == null) return new string[0];
                if (settingsValue is Array arr) return arr.Cast<string>().ToArray(); //varies depending on content https://github.com/godotengine/godot/issues/53047
                if (settingsValue is string[] sArr) return sArr;
            }
            return new string[0];
        }

        public static string[] IgnoredTypes => GetSettingsStringArray(IgnoredTypeSettingPath);

        public static string[] IgnoredPaths => GetSettingsStringArray(IgnoredPathsSettingPath);

        public override bool CanHandle(Object obj)
        {
            //some common exclusions
            if (obj is Script) return false;
            if (obj is PackedScene) return false;
            
            return obj is Resource || obj is Node;
        }

        public override void ParseCategory(Object obj, string category)
        {
            if (category == "Node" || category == "Resource")
            {
                AddPickerButton(obj);
            }
        }

        void AddPickerButton(Object obj)
        {
            var container = new HBoxContainer();
            container.AddChild(new Label {Text = string.Empty, SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill});
            var btn = new ScriptPickerButton
            {
                Resources = ResourceLoader.Load<ScriptPickerResources>("res://addons/csharp-script-picker/ScriptPickerResources.tres"),
                Target = obj,
                SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill,
                Text = "Pick C# Script",
                ClipText = true,
                HintTooltip = "Allows selecting c# scripts by namespace or folder,\noptionally filtered by type or path (see project settings)"
            };
            container.AddChild(btn);
            AddCustomControl(container);
            container.CallDeferred("raise");
        }
    }
}