using Godot;

namespace CSharpScriptPicker
{
    [Tool]
    public class Plugin : EditorPlugin
    {
        ScriptPickerPlugin pluginInstance;

        public override void _EnterTree()
        {
            ScriptPickerPlugin.RegisterSettings();
            pluginInstance = new ScriptPickerPlugin { EditorInterface = GetEditorInterface() };
            AddInspectorPlugin(pluginInstance);
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(pluginInstance);
        }
    }
}
