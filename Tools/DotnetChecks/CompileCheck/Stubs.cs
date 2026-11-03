// Type stubs for packages that aren't available outside Unity (Input System, uGUI) and for
// unchanged project classes. They mirror only the members the code under test uses.
#pragma warning disable CS0067, CS1591
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilities { }
namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }
    public enum InputDeviceChange { Added, Removed, Disconnected, Reconnected, Enabled, Disabled, UsageChanged, ConfigurationChanged, SoftReset, HardReset }
    public enum Key { None, Space, Enter, Backquote, F1, F2, F3, F4, F5, F6, F7, F8, F9, R }
    public struct InputDeviceDescription { public string product; public string manufacturer; public string interfaceName; }
    public class InputControl { public InputDevice device => null; }
    public class InputDevice : InputControl
    {
        public InputDeviceDescription description => default;
        public string displayName => "";
        public bool added => true;
        public bool enabled => true;
    }
    public class Joystick : InputDevice { public static IReadOnlyList<Joystick> all => new List<Joystick>(); public static Joystick current => null; }
    public class Gamepad : InputDevice { }
    public class KeyControl : InputControl { public bool wasPressedThisFrame => false; }
    public class Keyboard : InputDevice { public static Keyboard current => null; public KeyControl this[Key key] => null; }
    public struct BindingSyntax { }
    public struct CompositeSyntax { public CompositeSyntax With(string part, string binding) => this; }
    public sealed class InputAction : IDisposable
    {
        public InputAction(string name = null, InputActionType type = InputActionType.Value, string binding = null) { }
        public BindingSyntax AddBinding(string path) => default;
        public CompositeSyntax AddCompositeBinding(string composite) => default;
        public void Enable() { }
        public void Disable() { }
        public void Dispose() { }
        public T ReadValue<T>() where T : struct => default;
        public bool WasPressedThisFrame() => false;
        public InputControl activeControl => null;
    }
    public static class InputSystem { public static event Action<InputDevice, InputDeviceChange> onDeviceChange; }
}
namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class EventSystem : UIBehaviour { }
    public abstract class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
}
namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : UnityEngine.EventSystems.BaseInputModule { public void AssignDefaultActions() { } }
}
namespace UnityEngine.UI
{
    using UnityEngine.Events;
    public class Graphic : UnityEngine.EventSystems.UIBehaviour { public Color color { get; set; } public RectTransform rectTransform => null; }
    public class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic { }
    public class Text : MaskableGraphic
    {
        public Font font { get; set; } public int fontSize { get; set; } public FontStyle fontStyle { get; set; }
        public string text { get; set; } public bool supportRichText { get; set; } public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; } public VerticalWrapMode verticalOverflow { get; set; }
    }
    public class Selectable : UnityEngine.EventSystems.UIBehaviour { public bool interactable { get; set; } public Graphic targetGraphic { get; set; } public ColorBlock colors { get; set; } }
    public struct ColorBlock { public Color highlightedColor; public Color pressedColor; }
    public class Button : Selectable { public class ButtonClickedEvent : UnityEvent { } public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent(); }
    public class Slider : Selectable { public float value { get; set; } public float minValue { get; set; } public float maxValue { get; set; } public RectTransform fillRect { get; set; } }
    public class CanvasScaler : UnityEngine.EventSystems.UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; } public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; } public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : UnityEngine.EventSystems.UIBehaviour { }
}
namespace DroneSim.Drone.Benchmark
{
    public class BenchmarkRunner : UnityEngine.MonoBehaviour
    {
        public bool IsRunning => false;
        public bool IsProtocolRunActive => false;
        public bool IsSessionInitialized => true;
        public string SessionId => "";
        public string SessionDirectoryPath => "";
        public int CompletedRunCount => 0;
        public int DefaultProtocolManeuverCount => 0;
        public void StartFullProtocol() { }
    }
}
namespace DroneSim.Drone.Training
{
    public class TrainingDrill : UnityEngine.MonoBehaviour
    {
        public DrillState State => DrillState.Instructions;
        public bool CanRestart => true;
        public void ContinueFromInstructions() { }
        public void RestartDrill() { }
    }
}
namespace DroneSim.VR
{
    public class VirtualRCControllerRig : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Transform LeftStick => null;
        public UnityEngine.Transform RightStick => null;
    }
}
