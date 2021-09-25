using Godot;

namespace CSharpScriptPicker
{
    [Tool]
    public class ScriptPickerResources: Resource
    {
        [Export]
        PackedScene backButton;
        [Export]
        PackedScene contentButton;
        [Export]
        PackedScene popup;

        public PackedScene BackButton => backButton;
        public PackedScene ContentButton => contentButton;
        public PackedScene Popup => popup;
    }
}