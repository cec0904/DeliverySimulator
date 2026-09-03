using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Invector.vCharacterController;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using TestMode = UnityEditor.TestTools.TestRunner.Api.TestMode;

public class RespawnIntegrationTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string BikePath = "Assets/JEC/Prefabs/Motorbike_Prefab.prefab";

    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, Private).GetValue(target);

    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);

    private static void Hold(MotorbikeSummoner summoner, float seconds)
    {
        Call(summoner, "AdvanceHold", false, 0f);
        Call(summoner, "AdvanceHold", true, seconds);
    }

    [Test]
    public void AuthoredPrefabsHaveRequiredReferences()
    {
        string[] paths = {
            BikePath, "Assets/JEC/Prefabs/ParkourTestPlayer.prefab",
            "Assets/JEC/Prefabs/RespawnSystem.prefab",
            "Assets/JEC/UI/Phone/MotorbikeSpeedometer.prefab",
            "Assets/JEC/UI/Phone/PhoneCanvas.prefab"
        };
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                Assert.Zero(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), path + "/" + child.name);
        }
        GameObject bike = AssetDatabase.LoadAssetAtPath<GameObject>(BikePath);
        Assert.NotNull(bike.GetComponent<MotorbikeCrashDetector>());
        Assert.True(MotorbikePlacementBounds.TryGetLocalBounds(bike, out Bounds bounds));
        Assert.Greater(bounds.size.y, 0.5f);
        Debug.Log("[RespawnTest] Bike collider local bounds: " + bounds);

        RespawnManager manager = AssetDatabase.LoadAssetAtPath<GameObject>(paths[2]).GetComponent<RespawnManager>();
        Assert.NotNull(Field<Transform>(manager, "policeRespawnPoint"));
        Assert.NotNull(Field<Transform>(manager, "hospitalRespawnPoint"));
        Assert.True(Field<RespawnFadeUI>(manager, "fadeUI").IsReady);
        MotorbikeSummoner summoner = AssetDatabase.LoadAssetAtPath<GameObject>(paths[1]).GetComponent<MotorbikeSummoner>();
        Assert.NotNull(Field<GameObject>(summoner, "motorbikePrefab"));

        MotorbikeSpeedometer speedometer = AssetDatabase.LoadAssetAtPath<GameObject>(paths[4]).GetComponentInChildren<MotorbikeSpeedometer>(true);
        Assert.NotNull(speedometer, "PhoneCanvas nested speedometer");
        Assert.NotNull(Field<TMP_Text>(speedometer, "speedText"));
        Assert.NotNull(Field<TMP_Text>(speedometer, "unitText"));
        Assert.NotNull(Field<CanvasGroup>(speedometer, "contentGroup"));
    }

    [UnityTest]
    public IEnumerator GameplayFlowRestoresInputAndReusesBike()
    {
        yield return new EnterPlayMode();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainScene_v03")
        {
            // The project's PlayFromInitScene always starts at the title screen.
            // Loading the saved gameplay scene here does not save/discard editor edits.
            UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/JEC/Scenes/MainScene_v03/MainScene_v03.unity");
            yield return null;
        }
        // Third-party AI/service errors remain in Editor.log. This integration
        // test checks explicit gameplay assertions, not a clean-console claim.
        if (!RespawnTestRunner.IsLiveRun) LogAssert.ignoreFailingMessages = true;
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 1f;
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        RespawnManager manager = Object.FindAnyObjectByType<RespawnManager>();
        MotorbikeMount bike = Object.FindAnyObjectByType<MotorbikeMount>();
        Assert.NotNull(player);
        Assert.NotNull(manager, "Open MainScene_v03 before running the integration test.");
        Assert.NotNull(bike);
        Vector3 initialPlayerPosition = player.position;
        Quaternion initialPlayerRotation = player.rotation;
        Transform originalParent = player.parent;
        vThirdPersonInput input = player.GetComponent<vThirdPersonInput>();
        Assert.False(input.lockCharacterInput);
        bool inputEnabled = input.enabled;
        PlayerQuestList quests = Object.FindAnyObjectByType<PlayerQuestList>();
        MotorbikeSummoner summoner = player.GetComponent<MotorbikeSummoner>();
        Assert.NotNull(summoner);
        // Prevent real keyboard input from competing with deterministic hold tests.
        summoner.enabled = false;
        FieldInfo currentBikeField = typeof(MotorbikeSummoner).GetField("currentBike", Private);
        currentBikeField.SetValue(summoner, bike);
        MotorbikeCrashDetector detector = bike.GetComponent<MotorbikeCrashDetector>();
        CitizenAI citizen = Object.FindAnyObjectByType<CitizenAI>();
        TrafficCarAI car = Object.FindAnyObjectByType<TrafficCarAI>();
        float actorsDeadline = Time.unscaledTime + 15f;
        while ((citizen == null || car == null) && Time.unscaledTime < actorsDeadline)
        {
            yield return null;
            citizen = Object.FindAnyObjectByType<CitizenAI>();
            car = Object.FindAnyObjectByType<TrafficCarAI>();
        }
        Assert.NotNull(citizen);
        Assert.NotNull(car);
        Collider citizenCollider = citizen.GetComponentInChildren<Collider>(true);
        Collider carCollider = car.GetComponentInChildren<Collider>(true);
        Assert.NotNull(citizenCollider);
        Assert.NotNull(carCollider);
        Debug.Log($"[RespawnTest] Actors: citizen={citizen.name}, trigger={citizenCollider.isTrigger}, car={car.name}, trigger={carCollider.isTrigger}");
        Assert.False((bool)Call(detector, "TryProcessImpact", carCollider, 10f), "Unmounted contact");

        bike.Interact(player.gameObject);
        Assert.True(bike.IsMounted);
        Assert.False((bool)Call(detector, "TryProcessImpact", citizenCollider, 3.99f), "Low-speed contact");
        GameObject environment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        environment.transform.position = new Vector3(10000f, 10000f, 10000f);
        Assert.False((bool)Call(detector, "TryProcessImpact", environment.GetComponent<Collider>(), 20f), "Environment impact");
        Object.Destroy(environment);

        Rigidbody bikeBody = bike.GetComponent<Rigidbody>();
        // Avoid the parked police car immediately in front of the authored bike.
        bikeBody.linearVelocity = Vector3.up * 5f;
        yield return new WaitForFixedUpdate();
        yield return null;
        MotorbikeSpeedometer meter = Object.FindAnyObjectByType<MotorbikeSpeedometer>();
        Assert.NotNull(meter);
        Assert.AreEqual(1f, Field<CanvasGroup>(meter, "contentGroup").alpha);
        Assert.Greater(bike.Bicycle.currentSpeedKmh, 1f, "Moving-bike speed");
        Assert.AreEqual(Mathf.RoundToInt(bike.Bicycle.currentSpeedKmh).ToString(), Field<TMP_Text>(meter, "speedText").text);
        for (Transform ancestor = meter.transform; ancestor != null; ancestor = ancestor.parent)
        {
            RectTransform rect = ancestor as RectTransform;
            Debug.Log($"[RespawnTest] UI {ancestor.name}: pos={ancestor.position}, scale={ancestor.lossyScale}, rect={(rect != null ? rect.rect.ToString() : "none")}");
        }
        Debug.Log($"[RespawnTest] Speed text: active={Field<TMP_Text>(meter, "speedText").isActiveAndEnabled}, color={Field<TMP_Text>(meter, "speedText").color}");
        Vector3[] meterCorners = new Vector3[4];
        Vector3[] phoneCorners = new Vector3[4];
        meter.GetComponent<RectTransform>().GetWorldCorners(meterCorners);
        ((RectTransform)meter.transform.parent).GetWorldCorners(phoneCorners);
        Assert.Greater(meterCorners[0].y, phoneCorners[2].y, "Speedometer must be above the closed phone frame");
        Assert.Greater(meterCorners[0].y, 0f, "Speedometer must be on screen");
        ScreenCapture.CaptureScreenshot("Logs/respawn-mounted.png");

        Vector3 crashSite = bike.transform.position;
        Assert.True((bool)Call(detector, "TryProcessImpact", citizenCollider, 5f),
            $"Citizen impact: active={citizen.gameObject.activeInHierarchy}, mounted={bike.IsMounted}, respawning={manager.IsRespawning}");
        Assert.False(RespawnManager.TryRequestRespawn(RespawnReason.VehicleCrash, player), "Duplicate respawn");
        bike.Interact(player.gameObject);
        Assert.True(bike.IsMounted, "F must not interrupt the transition");
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(crashSite, bike.transform.position), 0.1f);
        foreach (WheelCollider wheel in bike.GetComponentsInChildren<WheelCollider>())
            Assert.Zero(wheel.motorTorque, "Residual motor torque");

        RespawnFadeUI fade = Field<RespawnFadeUI>(manager, "fadeUI");
        CanvasGroup fadeGroup = Field<CanvasGroup>(fade, "fadeCanvasGroup");
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.That(fadeGroup.alpha, Is.InRange(0.1f, 0.95f));
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(1f, fadeGroup.alpha);
        Assert.False(input.enabled);
        Vector3[] corners = new Vector3[4];
        fade.GetComponent<RectTransform>().GetWorldCorners(corners);
        Assert.GreaterOrEqual(corners[2].x - corners[0].x, Screen.width - 1f, "Fade width");
        Assert.GreaterOrEqual(corners[2].y - corners[0].y, Screen.height - 1f, "Fade height");
        ScreenCapture.CaptureScreenshot("Logs/respawn-black.png");
        yield return new WaitForSecondsRealtime(4f);
        Assert.False(manager.IsRespawning);
        Assert.AreEqual(inputEnabled, input.enabled);
        Assert.False(input.lockCharacterInput, "Mounted input lock leaked into on-foot state");
        Assert.False(bike.IsMounted);
        Assert.AreEqual(originalParent, player.parent);
        Assert.False(player.GetComponent<Rigidbody>().isKinematic);
        Assert.True(player.GetComponent<Collider>().enabled);
        Assert.True(player.GetComponent<Player_Interact>().enabled);
        Assert.AreSame(quests, Object.FindAnyObjectByType<PlayerQuestList>());
        Assert.Less(Vector3.Distance(player.position, Field<Transform>(manager, "hospitalRespawnPoint").position), 4f);
        Assert.AreEqual("합의금을 물어주고 병문안을 다녀왔습니다.", Field<TMP_Text>(NpcQuestUIController.CreateIfMissing(), "interactionText").text);
        Assert.Less(Vector3.Distance(crashSite, bike.transform.position), 3f, "Bike should stay at the crash site");
        Assert.AreEqual(0f, Field<CanvasGroup>(meter, "contentGroup").alpha);
        Debug.Log("[RespawnTest] Citizen transition, full-screen fade, dismount and input restoration passed.");

        ScreenCapture.CaptureScreenshot("Logs/respawn-hospital.png");
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(player.position, bike.transform.position), 11f, "Summon from the hospital after leaving the crashed bike behind");
        Assert.AreEqual("오토바이를 소환했습니다.", Field<TMP_Text>(NpcQuestUIController.CreateIfMissing(), "interactionText").text);
        Debug.Log("[RespawnTest] Hospital summon passed.");

        bike.Interact(player.gameObject);
        Assert.True(bike.IsMounted, "Remount after respawn");
        float vehicleImpactSpeed = Field<float>(detector, "vehicleImpactSpeed");
        Assert.False((bool)Call(detector, "TryProcessImpact", carCollider, vehicleImpactSpeed - 0.01f),
            "Vehicle contact below its separate threshold must not respawn");
        Assert.True((bool)Call(detector, "TryProcessImpact", carCollider, vehicleImpactSpeed),
            "Vehicle contact at its threshold must respawn");
        yield return new WaitForSecondsRealtime(5.5f);
        Assert.False(manager.IsRespawning);
        Assert.False(input.lockCharacterInput);
        Assert.AreEqual("입원 후 퇴원했습니다.", Field<TMP_Text>(NpcQuestUIController.CreateIfMissing(), "interactionText").text);
        Assert.True(RespawnManager.TryRequestRespawn(RespawnReason.PoliceArrest, player));
        yield return new WaitForSecondsRealtime(5.5f);
        Assert.False(manager.IsRespawning);
        Assert.AreEqual("경찰서에서 풀려났습니다.", Field<TMP_Text>(NpcQuestUIController.CreateIfMissing(), "interactionText").text);
        Assert.Less(Vector3.Distance(player.position, Field<Transform>(manager, "policeRespawnPoint").position), 4f);
        Debug.Log("[RespawnTest] Vehicle and police reasons/messages passed.");
        ScreenCapture.CaptureScreenshot("Logs/respawn-police.png");

        Time.timeScale = 0f;
        Assert.True(RespawnManager.TryRequestRespawn(RespawnReason.PoliceArrest, player));
        yield return new WaitForSecondsRealtime(5.5f);
        Assert.False(manager.IsRespawning, "Fade/teleport must finish while timeScale is zero");
        Assert.Zero(Time.timeScale, "Respawn must not overwrite global timeScale");
        Time.timeScale = 1f;

        yield return PlayerTeleportUtility.Teleport(player, initialPlayerPosition, initialPlayerRotation);
        int bikeCount = Object.FindObjectsByType<MotorbikeCrashDetector>(FindObjectsInactive.Include).Length;
        foreach (float distance in new[] { 20f, 49f })
        {
            Vector3 far = player.position + Vector3.right * distance;
            bike.Relocate(far, Quaternion.identity);
            Hold(summoner, 2f);
            Assert.Less(Vector3.Distance(far, bike.transform.position), 0.01f, "Distance gate: " + distance);
        }
        Vector3 atBoundary = player.position + Vector3.right * 50f;
        bike.Relocate(atBoundary, Quaternion.identity);
        Hold(summoner, 1f);
        Assert.Less(Vector3.Distance(atBoundary, bike.transform.position), 0.01f, "Short hold");
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(player.position, bike.transform.position), 11f, "50m boundary summon");
        Assert.AreEqual(bikeCount, Object.FindObjectsByType<MotorbikeCrashDetector>(FindObjectsInactive.Include).Length);
        Assert.Less(bikeBody.linearVelocity.magnitude, 0.001f);
        Vector3 relocated = bike.transform.position;
        bike.Relocate(atBoundary, Quaternion.identity);
        Call(summoner, "AdvanceHold", true, 3f);
        Assert.Less(Vector3.Distance(atBoundary, bike.transform.position), 0.01f, "One request per hold");
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(player.position, bike.transform.position), 11f, "Release and retry");

        bike.Relocate(atBoundary, Quaternion.identity);
        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.transform.position = player.position + Vector3.up * 2f;
        blocker.transform.localScale = new Vector3(25f, 6f, 25f);
        Physics.SyncTransforms();
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(atBoundary, bike.transform.position), 0.01f, "Blocked area must not force placement");
        Object.Destroy(blocker);
        yield return null;
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(player.position, bike.transform.position), 11f);
        bike.Interact(player.gameObject);
        Assert.True(bike.IsMounted);
        Vector3 mountedPosition = bike.transform.position;
        Hold(summoner, 2f);
        Assert.Less(Vector3.Distance(mountedPosition, bike.transform.position), 0.01f, "Mounted summon blocked");
        bike.TryDismountForRespawn();

        Assert.AreEqual(1, bikeCount, "Missing-instance test requires one authored bike in this scene");
        Object.Destroy(bike.gameObject);
        yield return null;
        Hold(summoner, 2f);
        yield return null;
        MotorbikeMount replacement = Object.FindAnyObjectByType<MotorbikeMount>();
        Assert.NotNull(replacement, "Destroy recovery");
        Assert.AreEqual(1, Object.FindObjectsByType<MotorbikeCrashDetector>(FindObjectsInactive.Include).Length);
        replacement.Interact(player.gameObject);
        Assert.True(replacement.IsMounted, "Replacement can be mounted");
        Debug.Log("[RespawnTest] Hold timing, 20/49/50m gates, reuse, blocked placement, destroyed-bike recovery passed.");
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator RestoreEditorState()
    {
        LogAssert.ignoreFailingMessages = false;
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
    }
}

[InitializeOnLoad]
public static class RespawnTestRunner
{
    public static bool IsLiveRun { get; private set; }
    static RespawnTestRunner()
    {
        TestRunnerApi.RegisterTestCallback(new Recorder());
        EditorApplication.delayCall += RunQueuedTest;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void RunQueuedTest()
    {
        const string request = "Temp/RunRespawnTests.trigger";
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        File.Delete(request);
        RunLive();
    }

    private const string LivePending = "JEC.Respawn.LiveTestPending";
    private static Stack<IEnumerator> liveSteps;
    private static int lastFrame;
    private static float? waitForFixedTime;

    [MenuItem("Tools/JEC/Run Respawn Tests In Current Scene")]
    public static void RunLive()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        new RespawnIntegrationTests().AuthoredPrefabsHaveRequiredReferences();
        SessionState.SetBool(LivePending, true);
        Debug.Log("[RespawnTest] Entering current scene without saving or replacing it.");
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(LivePending, false)) return;
        SessionState.SetBool(LivePending, false);
        IsLiveRun = true;
        liveSteps = new Stack<IEnumerator>();
        liveSteps.Push(new RespawnIntegrationTests().GameplayFlowRestoresInputAndReusesBike());
        lastFrame = -1;
        waitForFixedTime = null;
        EditorApplication.update += TickLive;
    }

    private static void TickLive()
    {
        if (!EditorApplication.isPlaying) { FinishLive("CANCELLED"); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (waitForFixedTime.HasValue && Mathf.Approximately(Time.fixedTime, waitForFixedTime.Value)) return;
        waitForFixedTime = null;
        try
        {
            while (liveSteps.Count > 0)
            {
                IEnumerator step = liveSteps.Peek();
                if (!step.MoveNext()) { liveSteps.Pop(); continue; }
                if (step.Current is EnterPlayMode) continue;
                if (step.Current is ExitPlayMode) { FinishLive("PASSED"); return; }
                if (step.Current is IEnumerator nested) { liveSteps.Push(nested); continue; }
                if (step.Current is WaitForFixedUpdate) waitForFixedTime = Time.fixedTime;
                return;
            }
            FinishLive("PASSED");
        }
        catch (System.Exception exception)
        {
            FinishLive("FAILED\n" + exception);
        }
    }

    private static void FinishLive(string result)
    {
        EditorApplication.update -= TickLive;
        liveSteps = null;
        Time.timeScale = 1f;
        if (!IsLiveRun) LogAssert.ignoreFailingMessages = false;
        IsLiveRun = false;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/RespawnLiveTests.txt", result);
        Debug.Log("[RespawnTest] Live result: " + result);
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    [MenuItem("Tools/JEC/Run Respawn Regression Tests %#F9")]
    public static void Run()
    {
        Debug.Log("[RespawnTest] Starting regression tests.");
        ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(
            new Filter { testMode = TestMode.EditMode, testNames = new[] { "RespawnIntegrationTests" } }));
    }

    private sealed class Recorder : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/RespawnTests.xml", result.ToXml().OuterXml);
            Debug.Log($"[RespawnTest] Finished: passed={result.PassCount}, failed={result.FailCount}");
        }
    }
}
