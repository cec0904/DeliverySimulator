using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Invector.vCharacterController;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class CameraZoomUIInputTests
{
    private CameraZoomin zoom;
    private vThirdPersonCamera camera;
    private UIManager ui;
    private PhoneUIController phone;
    private GameObject mapPanel;
    private const float Distance = 4f;
    private static readonly MethodInfo Process = typeof(CameraZoomin).GetMethod("ProcessScroll", BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private void Wheel(float delta)
    {
        Process.Invoke(zoom, new object[] { delta });
    }

    private void Unchanged(string context)
    {
        Assert.That(camera.defaultDistance, Is.EqualTo(Distance).Within(0.0001f), context);
    }

    private IEnumerator FinishPhone(float wheel)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (phone.IsAnimating && Time.realtimeSinceStartup < deadline)
        {
            Assert.True(ui.IsCameraInputBlocked, "Phone animation must own camera input");
            Wheel(wheel);
            Unchanged("Wheel during phone animation");
            yield return null;
        }
        Assert.False(phone.IsAnimating, "Phone animation timeout");
    }

    private IEnumerator VerifyRelease()
    {
        Assert.False(ui.IsCameraInputBlocked);
        Wheel(0.1f);
        Unchanged("Closing frame");
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            Wheel(i % 2 == 0 ? 0.1f : -0.1f);
            Unchanged("Continuing UI wheel after close");
        }
        float quietDeadline = Time.unscaledTime + 0.15f;
        while (Time.unscaledTime < quietDeadline)
        {
            yield return null;
            Wheel(0f);
            Unchanged("Neutral input settling period");
        }
        yield return null;
        Wheel(0.1f);
        Assert.That(camera.defaultDistance, Is.EqualTo(Distance - zoom.zoomSpeed * 0.1f).Within(0.0001f), "New gameplay wheel must zoom in");
        yield return null;
        Wheel(-0.1f);
        Unchanged("New gameplay wheel must zoom out");
    }

    [UnityTest]
    public IEnumerator MapAndPhoneDoNotLeakWheelIntoCamera()
    {
        yield return new EnterPlayMode();
        if (!CameraZoomUIInputTestRunner.IsLiveRun) LogAssert.ignoreFailingMessages = true;
        SceneManager.LoadScene("Assets/JEC/Scenes/MainScene_v03/MainScene_v03.unity");
        yield return null;
        yield return null;
        ui = UIManager.Instance;
        zoom = Object.FindAnyObjectByType<CameraZoomin>();
        Assert.NotNull(ui);
        Assert.NotNull(zoom);
        camera = zoom.GetComponent<vThirdPersonCamera>();
        phone = Field<PhoneUIController>(ui, "phoneUIController");
        mapPanel = Field<GameObject>(ui, "mapPanel");
        Assert.NotNull(camera);
        Assert.NotNull(phone);
        Assert.NotNull(mapPanel);
        Assert.False(ui.IsCameraInputBlocked);
        // Deterministic input samples exercise the same production method called
        // by LateUpdate. UI events below still use the actual scene components.
        zoom.enabled = false;
        camera.defaultDistance = Distance;
        Wheel(0f);
        yield return null;

        for (int cycle = 0; cycle < 10; cycle++)
        {
            ui.TogglePanel(mapPanel);
            Assert.True(ui.IsCameraInputBlocked);
            Wheel(0.1f);
            Unchanged("Map opening frame");
            yield return null;
            var map = mapPanel.GetComponentInChildren<MapController>(true);
            Assert.NotNull(map);
            var exterior = Field<RectTransform>(map, "exteriorMap");
            var pointer = new PointerEventData(EventSystem.current)
            {
                scrollDelta = Vector2.up,
                position = RectTransformUtility.WorldToScreenPoint(null, exterior.TransformPoint(exterior.rect.center)),
                pointerCurrentRaycast = new RaycastResult { gameObject = exterior.gameObject }
            };
            float previousZoom = map.CurrentZoom;
            map.OnScroll(pointer);
            Assert.Greater(map.CurrentZoom, previousZoom, "Map zoom must still work");
            Wheel(0.1f);
            Unchanged("Map and camera receive the same wheel sample");
            // Input must remain UI-owned even if another system unpauses time.
            Time.timeScale = 1f;
            Wheel(-0.1f);
            Unchanged("UI ownership does not depend on timeScale");
            Time.timeScale = 0f;
            if (cycle % 2 == 0) ui.TogglePanel(mapPanel);
            else typeof(UIManager).GetMethod("HandleEscapeKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            yield return VerifyRelease();

            phone.TogglePhone();
            Assert.True(phone.IsAnimating);
            Assert.True(ui.IsCameraInputBlocked, "Direct phone open must synchronously block camera input");
            Assert.AreEqual(0f, Time.timeScale);
            yield return FinishPhone(0.1f);
            Assert.True(phone.IsOpen);
            Wheel(-0.1f);
            Unchanged("Open phone");
            if (cycle == 0) VerifyPhoneScroll();
            if (cycle == 5) VerifySubscriptions();
            if (cycle % 2 == 0) phone.TogglePhone();
            else typeof(UIManager).GetMethod("HandleEscapeKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            yield return FinishPhone(-0.1f);
            Assert.False(phone.IsOpen);
            yield return VerifyRelease();
        }

        // Both UI changes occur before the camera samples this frame.
        ui.OpenMapPanel();
        ui.ClosePanel(mapPanel);
        Assert.AreEqual(Time.frameCount, ui.LastCameraInputStateChangeFrame);
        Wheel(0.1f);
        Unchanged("Open and close within one frame");
        yield return VerifyRelease();

        phone.OpenPhone();
        yield return FinishPhone(0.1f);
        phone.ClosePhone();
        ui.OpenMapPanel();
        yield return FinishPhone(-0.1f);
        Assert.True(ui.IsCameraInputBlocked, "Map still blocks after phone finishes closing");
        Wheel(0.1f);
        Unchanged("Overlapping map and phone");
        ui.ClosePanel(mapPanel);
        yield return VerifyRelease();

        // A disabled/re-enabled zoom component must also reject an old gesture.
        zoom.enabled = true;
        zoom.enabled = false;
        Wheel(0.1f);
        Unchanged("Re-enabled camera");
        yield return VerifyRelease();
        Debug.Log("[CameraWheelQA] PASS: map 10 cycles, phone 10 cycles, animation input, ESC/toggle closes, same-frame open/close, continuing gesture, neutral rearm, overlapping UI, UI scroll, subscriptions, camera re-enable.");
        if (CameraZoomUIInputTestRunner.UseNativeInput)
            yield return NativeWheelSmoke();
        yield return new ExitPlayMode();
    }

    private IEnumerator NativeWheelSmoke()
    {
#if UNITY_EDITOR_WIN
        var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        gameView.Focus();
        var window = gameView.position;
        NativeWheel.SetCursorPos((int)window.center.x, (int)window.center.y);
        zoom.enabled = true;
        camera.defaultDistance = Distance;
        float initialQuietDeadline = Time.unscaledTime + 0.2f;
        while (Time.unscaledTime < initialQuietDeadline) yield return null;
        yield return NativeWheelPulse(120);
        Assert.Less(camera.defaultDistance, Distance, "Real OS wheel must reach Input.GetAxis and LateUpdate");
        camera.defaultDistance = Distance;

        for (int cycle = 0; cycle < 3; cycle++)
        {
            ui.OpenMapPanel();
            yield return NativeWheelPulse(120);
            Unchanged("Native wheel while map is open");
            NativeWheel.Send(-120);
            ui.ClosePanel(mapPanel);
            yield return NativeWheelObserve();
            Unchanged("Native wheel on map close");
            phone.OpenPhone();
            while (phone.IsAnimating) yield return null;
            yield return NativeWheelPulse(-120);
            Unchanged("Native wheel while phone is open");
            phone.ClosePhone();
            yield return NativeWheelPulse(120);
            while (phone.IsAnimating) yield return null;
            Unchanged("Native wheel while phone closes");
            float quietDeadline = Time.unscaledTime + 0.2f;
            while (Time.unscaledTime < quietDeadline) yield return null;
            yield return NativeWheelPulse(-120);
            Assert.Greater(camera.defaultDistance, Distance, "Fresh native wheel after UI close must work");
            camera.defaultDistance = Distance;
        }
        Debug.Log("[CameraWheelQA] PASS: real Windows wheel -> Input.GetAxis -> LateUpdate; map/phone 3 cycles and fresh gameplay zoom.");
#else
        Assert.Fail("Native wheel QA is Windows-only.");
        yield break;
#endif
    }

    private IEnumerator NativeWheelPulse(int delta)
    {
#if UNITY_EDITOR_WIN
        NativeWheel.Send(delta);
#endif
        yield return NativeWheelObserve();
    }

    private IEnumerator NativeWheelObserve()
    {
        float deadline = Time.realtimeSinceStartup + 0.25f;
        bool received = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            received |= Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f;
        }
        Assert.True(received, "Windows wheel injection must actually reach the Game view");
    }

#if UNITY_EDITOR_WIN
    private static class NativeWheel
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
        public static void Send(int delta)
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
            Assert.AreEqual((uint)System.Diagnostics.Process.GetCurrentProcess().Id, pid, "Do not send wheel input to another application");
            mouse_event(0x0800, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
        }
    }
#endif

    private void VerifyPhoneScroll()
    {
        var questUI = Object.FindObjectsByType<PhoneQuestUIController>(FindObjectsInactive.Include)
            .FirstOrDefault(x => Field<PhoneUIController>(x, "phoneUIController") == phone);
        Assert.NotNull(questUI, "Locate quest UI through its phone reference, not an assumed hierarchy");
        Field<Button>(questUI, "newQuestButton").onClick.Invoke();
        var content = (RectTransform)Field<Transform>(questUI, "newQuestContent");
        var scroll = content.GetComponentInParent<ScrollRect>();
        Assert.NotNull(scroll);
        Assert.True(scroll.isActiveAndEnabled);
        // Runtime-only rows ensure the real quest ScrollRect has overflow even
        // when the player's offer list happens to be empty. No quest data edited.
        var rows = new List<GameObject>();
        try
        {
            for (int i = 0; i < 20; i++)
            {
                var row = new GameObject("Wheel QA row", typeof(RectTransform), typeof(LayoutElement));
                row.transform.SetParent(content, false);
                row.GetComponent<LayoutElement>().preferredHeight = 200f;
                rows.Add(row);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
            scroll.StopMovement();
            Vector2 before = content.anchoredPosition;
            scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.down * 5f });
            Assert.Greater((content.anchoredPosition - before).sqrMagnitude, 0.001f, "Phone list must still scroll");
            Wheel(-0.1f);
            Unchanged("Phone UI and camera receive the same wheel sample");
        }
        finally
        {
            foreach (var row in rows) Object.Destroy(row);
        }
    }

    private void VerifySubscriptions()
    {
        foreach (string name in new[] { "TransitionStarted", "StateChanged" })
        {
            var field = typeof(PhoneUIController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 3; i++)
            {
                ui.enabled = false;
                Assert.AreEqual(0, ((Delegate)field.GetValue(phone))?.GetInvocationList().Count(d => ReferenceEquals(d.Target, ui)) ?? 0);
                ui.enabled = true;
                Assert.AreEqual(1, ((Delegate)field.GetValue(phone)).GetInvocationList().Count(d => ReferenceEquals(d.Target, ui)));
                Assert.AreEqual(0f, Time.timeScale, "Re-enabled manager must retain the open UI pause");
            }
        }
    }

    [UnityTearDown]
    public IEnumerator RestoreEditor()
    {
        LogAssert.ignoreFailingMessages = false;
        if (EditorApplication.isPlaying)
        {
            Time.timeScale = 1f;
            yield return new ExitPlayMode();
        }
    }
}

[InitializeOnLoad]
public static class CameraZoomUIInputTestRunner
{
    private const string Pending = "JEC.CameraWheelQA.Pending";
    private static Stack<IEnumerator> steps;
    private static int lastFrame;
    public static bool IsLiveRun { get; private set; }
    public static bool UseNativeInput { get; private set; }
    static CameraZoomUIInputTestRunner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += () =>
        {
            const string trigger = "Temp/RunCameraWheelQA.trigger";
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(trigger)) return;
            SessionState.SetBool(Pending + ".Native", File.ReadAllText(trigger).Contains("native"));
            File.Delete(trigger);
            Run();
        };
    }

    [MenuItem("Tools/JEC/Run Camera Wheel UI QA")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && steps != null) Finish("CANCELLED");
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        IsLiveRun = true;
        UseNativeInput = SessionState.GetBool(Pending + ".Native", false);
        steps = new Stack<IEnumerator>();
        steps.Push(new CameraZoomUIInputTests().MapAndPhoneDoNotLeakWheelIntoCamera());
        lastFrame = -1;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { Finish("CANCELLED"); return; }
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            while (steps.Count > 0)
            {
                var step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                if (step.Current is EnterPlayMode) continue;
                if (step.Current is ExitPlayMode) { Finish("PASSED"); return; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Finish("PASSED");
        }
        catch (Exception error) { Finish("FAILED\n" + error); }
    }

    private static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        steps = null;
        IsLiveRun = false;
        UseNativeInput = false;
        SessionState.SetBool(Pending + ".Native", false);
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/CameraWheelUIQA.txt", DateTime.Now.ToString("O") + "\n" + result);
        Debug.Log("[CameraWheelQA] " + result);
        if (EditorApplication.isPlaying) { Time.timeScale = 1f; EditorApplication.ExitPlaymode(); }
    }
}
