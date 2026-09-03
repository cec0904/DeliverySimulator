using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class PhoneQuestNotificationTests
{
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    [Test]
    public void PrefabHasEditableCornerBadge()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JEC/UI/Phone/PhoneCanvas.prefab");
        var notification = prefab.GetComponentInChildren<PhoneQuestNotification>(true);
        Assert.NotNull(notification);
        Assert.NotNull(Field<PhoneUIController>(notification, "phoneUIController"));
        var badge = Field<GameObject>(notification, "badgeRoot");
        var label = Field<TMP_Text>(notification, "countText");
        Assert.NotNull(badge);
        Assert.NotNull(label);
        Assert.False(notification.transform.IsChildOf(badge.transform), "Hiding the badge must not disable its listener");
        Assert.AreEqual("PhoneFrame", badge.transform.parent.name);
        Assert.AreEqual(badge.transform, label.transform.parent);
        var rect = (RectTransform)badge.transform;
        Assert.AreEqual(Vector2.one, rect.anchorMin);
        Assert.AreEqual(Vector2.one, rect.anchorMax);
        Assert.AreEqual(rect.sizeDelta.x, rect.sizeDelta.y);
        var image = badge.GetComponent<Image>();
        Assert.AreEqual(AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"), image.sprite);
        Assert.Greater(image.color.r, 0.8f);
        Assert.Less(image.color.g, 0.2f);
        Assert.Less(image.color.b, 0.2f);
        Assert.AreEqual(Color.white, label.color);
        Assert.False(image.raycastTarget);
        Assert.False(label.raycastTarget);
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            Assert.Zero(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), child.name);
    }

    private static void Expect(PhoneQuestNotification notification, int count, bool visible)
    {
        Assert.AreEqual(count.ToString(), Field<TMP_Text>(notification, "countText").text, "Offer count");
        Assert.AreEqual(visible, Field<GameObject>(notification, "badgeRoot").activeInHierarchy, "Badge visibility");
    }

    private static IEnumerator FinishAnimation(PhoneUIController phone)
    {
        float deadline = Time.realtimeSinceStartup + 6f;
        while (phone.IsAnimating && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.False(phone.IsAnimating, "Unscaled phone animation timed out");
    }

    private static IEnumerator Capture(string suffix)
    {
        Canvas.ForceUpdateCanvases();
        yield return null;
        ScreenCapture.CaptureScreenshot("Logs/phone-notification-" + suffix + ".png");
        // The screenshot is rendered at end of frame, before later test changes.
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator OffersAndRepeatedPhoneTransitionsStaySynchronized()
    {
        yield return new EnterPlayMode();
        if (!PhoneQuestNotificationTestRunner.IsLiveRun) LogAssert.ignoreFailingMessages = true;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainScene_v03")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/JEC/Scenes/MainScene_v03/MainScene_v03.unity");
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1f);
        var notification = Object.FindAnyObjectByType<PhoneQuestNotification>();
        Assert.NotNull(notification, "MainScene prefab instance must inherit the badge");
        var phone = Field<PhoneUIController>(notification, "phoneUIController");
        var manager = Object.FindAnyObjectByType<questManager>();
        var selected = Object.FindAnyObjectByType<PlayerQuestList>();
        Assert.NotNull(manager);
        Assert.NotNull(selected);
        Assert.AreEqual(manager, Field<questManager>(notification, "questManager"));
        Expect(notification, manager.QuestOffers.Count, manager.QuestOffers.Count > 0);

        // Freeze only automatic offer generation; exercise the real public quest operations.
        manager.enabled = false;
        // The authored scene currently caps offers at one. Raise the limit only on
        // this disposable Play Mode instance to cover two-digit badge text too.
        Debug.Log("[PhoneNotificationTest] Authored offer limit=" + Field<int>(manager, "MaxRefreshQuestCount"));
        manager.GetType().GetField("MaxRefreshQuestCount", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, 10);
        Time.timeScale = 1f;
        while (selected.SelectedQuests.Count > 0)
            Assert.True(selected.TryCancelQuest(selected.SelectedQuests[0].runtimeQuestId));
        while (manager.QuestOffers.Count > 0)
            Assert.True(manager.TryCancelQuestOffer(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, 0, false);

        Assert.True(manager.TryAddRandomQuestOffer());
        Expect(notification, 1, true);
        yield return Capture("one");
        while (manager.QuestOffers.Count < 10) Assert.True(manager.TryAddRandomQuestOffer());
        Expect(notification, 10, true);
        yield return Capture("ten");
        Assert.True(manager.TryAcceptQuest(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, 9, true);
        Assert.True(manager.TryCancelQuestOffer(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, 8, true);
        while (manager.QuestOffers.Count > 0)
            Assert.True(manager.TryCancelQuestOffer(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, 0, false);
        yield return Capture("zero");

        // A rejected acceptance must leave the pending-offer count unchanged.
        while (selected.SelectedQuests.Count < Field<int>(selected, "maxSelectedQuestCount"))
        {
            Assert.True(manager.TryAddRandomQuestOffer());
            Assert.True(manager.TryAcceptQuest(manager.QuestOffers[0].runtimeQuestId));
            Expect(notification, 0, false);
        }
        Assert.True(manager.TryAddRandomQuestOffer());
        Assert.False(manager.TryAcceptQuest(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, 1, true);
        while (selected.SelectedQuests.Count > 0)
            Assert.True(selected.TryCancelQuest(selected.SelectedQuests[0].runtimeQuestId));

        for (int cycle = 0; cycle < 3; cycle++)
        {
            Time.timeScale = 0f;
            phone.TogglePhone(); // Same entry point used by UIManager's Q key handler.
            Assert.True(phone.IsAnimating);
            Expect(notification, manager.QuestOffers.Count, false);
            yield return FinishAnimation(phone);
            Assert.True(phone.IsOpen);
            Assert.True(manager.TryAddRandomQuestOffer());
            Expect(notification, manager.QuestOffers.Count, false);
            Assert.True(manager.TryAcceptQuest(manager.QuestOffers[0].runtimeQuestId));
            Expect(notification, manager.QuestOffers.Count, false);
            if (cycle == 0) yield return Capture("open");
            phone.TogglePhone();
            Expect(notification, manager.QuestOffers.Count, false);
            yield return FinishAnimation(phone);
            Assert.False(phone.IsOpen);
            Expect(notification, manager.QuestOffers.Count, true);
        }

        // Accepting the final offer while open must not bring back an empty badge on close.
        phone.OpenPhone();
        yield return FinishAnimation(phone);
        Assert.AreEqual(1, manager.QuestOffers.Count);
        Assert.True(manager.TryAcceptQuest(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, 0, false);
        phone.ClosePhone();
        yield return FinishAnimation(phone);
        Expect(notification, 0, false);
        Assert.True(manager.TryAddRandomQuestOffer());
        Expect(notification, 1, true);

        notification.enabled = false;
        Assert.False(Field<GameObject>(notification, "badgeRoot").activeSelf);
        Assert.True(manager.TryAddRandomQuestOffer());
        notification.enabled = true;
        Expect(notification, manager.QuestOffers.Count, true);
        Assert.True(manager.TryCancelQuestOffer(manager.QuestOffers[0].runtimeQuestId));
        Expect(notification, manager.QuestOffers.Count, true);

        var badgeRect = (RectTransform)Field<GameObject>(notification, "badgeRoot").transform;
        var corners = new Vector3[4];
        badgeRect.GetWorldCorners(corners);
        Debug.Log($"[PhoneNotificationTest] Screen={Screen.width}x{Screen.height}, badge corners={corners[0]}..{corners[2]}");
        Assert.GreaterOrEqual(corners[0].x, 0f);
        Assert.GreaterOrEqual(corners[0].y, 0f);
        Assert.LessOrEqual(corners[2].x, Screen.width);
        Assert.LessOrEqual(corners[2].y, Screen.height);
        Debug.Log("[PhoneNotificationTest] PASS: initial/0/1/10 offers, create, accept, cancel, capacity rejection, three open/close cycles at timeScale=0, close after final acceptance, re-enable subscriptions, visible bounds.");
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator RestoreEditorState()
    {
        LogAssert.ignoreFailingMessages = false;
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
    }
}

// This opt-in runner enters Play Mode without saving/discarding an edited scene.
[InitializeOnLoad]
public static class PhoneQuestNotificationTestRunner
{
    private const string Pending = "JEC.PhoneNotification.LivePending";
    private static Stack<IEnumerator> steps;
    private static int lastFrame;
    public static bool IsLiveRun { get; private set; }

    static PhoneQuestNotificationTestRunner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += () =>
        {
            const string request = "Temp/RunPhoneNotificationTests.trigger";
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
            File.Delete(request);
            Run();
        };
    }

    [MenuItem("Tools/JEC/Run Phone Notification QA")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { new PhoneQuestNotificationTests().PrefabHasEditableCornerBadge(); }
        catch (System.Exception exception) { Finish("FAILED\n" + exception); return; }
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        IsLiveRun = true;
        steps = new Stack<IEnumerator>();
        steps.Push(new PhoneQuestNotificationTests().OffersAndRepeatedPhoneTransitionsStaySynchronized());
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
        File.WriteAllText("Logs/PhoneNotificationTests.txt", result);
        Debug.Log("[PhoneNotificationTest] " + result);
        if (EditorApplication.isPlaying) { Time.timeScale = 1f; EditorApplication.ExitPlaymode(); }
    }
}
