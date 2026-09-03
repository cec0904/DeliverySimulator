using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class MotorbikeSpeedometerTests
{
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static void Expect(CanvasGroup group, bool visible)
    {
        Assert.AreEqual(visible ? 1f : 0f, group.alpha, "Speedometer visibility");
        Assert.False(group.interactable);
        Assert.False(group.blocksRaycasts);
    }

    private static IEnumerator FinishAnimation(PhoneUIController phone, CanvasGroup group, bool visibleAfter)
    {
        float deadline = Time.realtimeSinceStartup + 6f;
        while (phone.IsAnimating && Time.realtimeSinceStartup < deadline)
        {
            Expect(group, false);
            yield return null;
        }
        Assert.False(phone.IsAnimating, "Phone animation timed out");
        Expect(group, visibleAfter);
    }

    [UnityTest]
    public IEnumerator OnlyMountedAndFullyClosedShowsSpeedometer()
    {
        yield return new EnterPlayMode();
        if (!MotorbikeSpeedometerTestRunner.IsLiveRun) LogAssert.ignoreFailingMessages = true;
        SceneManager.LoadScene("Assets/JEC/Scenes/MainScene_v03/MainScene_v03.unity");
        yield return null;
        yield return null;

        var meter = Object.FindAnyObjectByType<MotorbikeSpeedometer>();
        var bike = Object.FindAnyObjectByType<MotorbikeMount>();
        var player = GameObject.FindGameObjectWithTag("Player");
        Assert.NotNull(meter);
        Assert.NotNull(bike);
        Assert.NotNull(player);
        var phone = meter.GetComponentInParent<PhoneUIController>(true);
        Assert.NotNull(phone);
        Assert.AreSame(phone, Field<PhoneUIController>(meter, "phoneUIController"), "Automatic phone binding");
        var group = Field<CanvasGroup>(meter, "contentGroup");
        Assert.NotNull(group);
        Assert.False(bike.IsMounted);
        Expect(group, false);

        // Isolate visibility from traffic/physics and real keyboard input in this Play session.
        Object.FindAnyObjectByType<UIManager>().enabled = false;
        bike.GetComponent<MotorbikeCrashDetector>().enabled = false;
        Time.timeScale = 0f;
        phone.TogglePhone();
        Expect(group, false);
        yield return FinishAnimation(phone, group, false);
        phone.TogglePhone();
        yield return FinishAnimation(phone, group, false);

        bike.Interact(player);
        Assert.True(bike.IsMounted);
        yield return null;
        Expect(group, true);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            Time.timeScale = 0f;
            phone.TogglePhone();
            Assert.True(phone.IsAnimating);
            Expect(group, false); // Must hide synchronously, before another Update.
            yield return FinishAnimation(phone, group, false);
            Assert.True(phone.IsOpen);
            meter.enabled = false;
            Expect(group, false);
            meter.enabled = true;
            Expect(group, false);
            phone.TogglePhone();
            Expect(group, false);
            yield return FinishAnimation(phone, group, true);
            Assert.False(phone.IsOpen);
        }

        // Closing must re-check mount state, not restore a cached visible value.
        phone.TogglePhone();
        yield return FinishAnimation(phone, group, false);
        Assert.True(bike.TryDismountForRespawn());
        yield return null;
        phone.TogglePhone();
        yield return FinishAnimation(phone, group, false);
        Assert.False(bike.IsMounted);

        // Mount input has a scaled-time cooldown after dismounting.
        Time.timeScale = 1f;
        float remountTime = Time.time + Field<float>(bike, "inputCooldown") + 0.05f;
        while (Time.time < remountTime) yield return null;
        Time.timeScale = 0f;
        bike.Interact(player);
        Assert.True(bike.IsMounted, "Remount after input cooldown");
        yield return null;
        Expect(group, true);
        phone.TogglePhone();
        yield return FinishAnimation(phone, group, false);
        phone.TogglePhone();
        Assert.True(bike.TryDismountForRespawn());
        yield return FinishAnimation(phone, group, false);
        meter.enabled = false;
        meter.enabled = true;
        Expect(group, false);
        Debug.Log("[SpeedometerTest] PASS: unmounted hidden, mounted visible, immediate opening hide, open/closing hidden, three paused animation cycles, listener re-enable, dismount while open/closing, remount.");
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator RestoreEditorState()
    {
        LogAssert.ignoreFailingMessages = false;
        if (EditorApplication.isPlaying)
        {
            Time.timeScale = 1f;
            yield return new ExitPlayMode();
        }
    }
}

// Opt-in live runner avoids saving or discarding an edited scene to start the test.
[InitializeOnLoad]
public static class MotorbikeSpeedometerTestRunner
{
    private const string Pending = "JEC.Speedometer.LivePending";
    private static Stack<IEnumerator> steps;
    private static int lastFrame;
    public static bool IsLiveRun { get; private set; }

    static MotorbikeSpeedometerTestRunner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/RunSpeedometerTests.trigger";
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            File.Delete(request);
            Run();
        };
    }

    [MenuItem("Tools/JEC/Run Speedometer Phone QA")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        IsLiveRun = true;
        steps = new Stack<IEnumerator>();
        steps.Push(new MotorbikeSpeedometerTests().OnlyMountedAndFullyClosedShowsSpeedometer());
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
                IEnumerator step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                if (step.Current is EnterPlayMode) continue;
                if (step.Current is ExitPlayMode) { Finish("PASSED"); return; }
                if (step.Current is IEnumerator nested) { steps.Push(nested); continue; }
                return;
            }
            Finish("PASSED");
        }
        catch (System.Exception exception) { Finish("FAILED\n" + exception); }
    }

    private static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        steps = null;
        IsLiveRun = false;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/SpeedometerPhoneTests.txt", System.DateTime.Now.ToString("O") + "\n" + result);
        Debug.Log("[SpeedometerTest] " + result);
        if (EditorApplication.isPlaying) { Time.timeScale = 1f; EditorApplication.ExitPlaymode(); }
    }
}
