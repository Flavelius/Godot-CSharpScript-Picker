using Godot;

namespace CSharpScriptPicker
{
    [Tool]
    public class ScriptPickerButton: Button
    {
        public ScriptPickerResources Resources { get; set; }
        public Object Target { get; set; }

        ScriptPickerPopup popupInstance;

        public override void _EnterTree()
        {
            base._EnterTree();
            Icon = GetIcon("ListSelect", "EditorIcons");
            ExpandIcon = true;
        }

        public override void _Pressed()
        {
            if (IsInstanceValid(popupInstance))
            {
                popupInstance.QueueFree();
                popupInstance = null;
            }
            popupInstance = Resources.Popup.Instance<ScriptPickerPopup>();
            AddChild(popupInstance);
            popupInstance.Load(Resources, Target);
            popupInstance.Popup_();
            popupInstance.RectGlobalPosition = GetClampedPosition(popupInstance, GetViewport().GetMousePosition());
        }

        Vector2 GetClampedPosition(Control c, Vector2 pos)
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            var rectSize = c.RectSize;
            return new Vector2(Mathf.Clamp(pos.x, 20, viewportSize.x-rectSize.x-20), Mathf.Clamp(pos.y, 20, viewportSize.y-rectSize.y-20));
        }
    }
}