using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PitchAwareMusicPlayer : MonoBehaviour
{
    [Header("Audio Sources (loop = true)")]
    public AudioSource music1Forward;
    public AudioSource music1Reverse;
    public AudioSource music2;
    public AudioSource music3Forward;
    public AudioSource music3Reverse;
    public AudioSource music4;

    [Header("Settings")]
    [Range(0.1f, 4f)] public float pitchMax = 3f;
    public float crossfadeDuration = 2f;

    [Header("Music4 behavior")]
    public bool quickSkipMusic4 = true;     // true = touch M4 briefly then move on
    public float music4HoldSeconds = 0.75f; // hold time when skipping

    [Header("Master Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f; // overall volume multiplier

    [Header("Clock / Timer")]
    public ClockMode clockMode = ClockMode.DSP;     // DSP or Realtime
    public bool autoCalibrateClock = true;          // measure DSP vs realtime at start
    [Range(0.5f, 5f)] public float calibrateSeconds = 1.5f;
    [Range(0.0f, 0.5f)] public float ratioTolerance = 0.1f; // if |ratio - 1| > tolerance, switch to Realtime

    [Header("Robustness")]
    public bool force2D = true;
    public bool preloadAudio = true;
    public float fallbackStageSecondsIfClipMissing = 8f;

    [Header("Debug / HUD")]
    public bool showHUD = true;           // F1 toggle (legacy input only)
    public bool enableShortcuts = true;   // [ prev, ] next, R reset (legacy input only)
    public bool logStages = false;
    public KeyCode toggleHUDKey = KeyCode.F1;

    public enum Stage
    {
        M2Only = 2,          // 2) Music2 only
        M1withM3 = 3,        // 3) Music1 @ max + Music3 (forward)
        ToM4 = 4,            // 4) Turn off M1 and switch to Music4 (brief if quickSkip)
        ToM3Reversed = 5,    // 5) Music3 reversed (solo)
        Rev3PlusRev1 = 6,    // 6) Reversed Music3 + Reversed Music1
        Rev3Only = 7,        // 7) Reversed Music3 only
        Rev3PlusRev1_2 = 8,  // 8) Reversed Music3 + Reversed Music1 again
        Fwd3PlusM1 = 9,      // 9) Music3 forward + Music1 @ max
        Rev3PlusRev1_End = 10// 10) Reversed Music3 + Reversed Music1, then loop to Stage 3
    }

    public enum ClockMode { DSP, Realtime }

    // Loop cycle (3 → 10)
    private static readonly Stage[] LoopCycle = new Stage[]
    {
        Stage.M1withM3, Stage.ToM4, Stage.ToM3Reversed, Stage.Rev3PlusRev1,
        Stage.Rev3Only, Stage.Rev3PlusRev1_2, Stage.Fwd3PlusM1, Stage.Rev3PlusRev1_End
    };

    // Debug (visible in Inspector)
    [SerializeField] private int debugStageNumber;
    [SerializeField] private string debugStageName;
    [SerializeField] private float debugStageTime;
    [SerializeField] private float debugStageTarget;
    [SerializeField] private float debugTotalTime;

    // Clock debug
    [SerializeField] private string activeClockLabel = "DSP";
    [SerializeField] private float dspToRealRatio = 1f;

    private Stage currentStage;
    private Coroutine sequenceCo;
    private readonly Dictionary<AudioSource, Coroutine> fadeMap = new Dictionary<AudioSource, Coroutine>();

    // Master volume: base volumes separate from master multiplier
    private readonly Dictionary<AudioSource, float> baseVolumeMap = new Dictionary<AudioSource, float>();
    private float lastAppliedMasterVolume = 1f;
    private AudioSource[] allSources;

    private double totalStartClock;            // start time on the active clock
    private double totalStartDSP;
    private double totalStartReal;

    private bool didInitialSegment = false;    // after M2Only and inserted Fwd3PlusM1
    private bool useRealtimeClock = false;     // true if forced or auto-switched

    void Awake()
    {
        var count = FindObjectsOfType<PitchAwareMusicPlayer>().Length;
        if (count > 1)
            Debug.LogWarning($"[{name}] Multiple PitchAwareMusicPlayer instances in scene: {count}");
    }

    void OnEnable()
    {
        SetupSources();
        StartCoroutine(BootAndStart());
    }

    void OnDisable()
    {
        if (sequenceCo != null) StopCoroutine(sequenceCo);
        foreach (var kv in fadeMap)
            if (kv.Value != null) StopCoroutine(kv.Value);
        fadeMap.Clear();
    }

    private IEnumerator BootAndStart()
    {
        // Initial clock selection
        useRealtimeClock = (clockMode == ClockMode.Realtime);

        if (autoCalibrateClock && clockMode == ClockMode.DSP)
            yield return CalibrateClockAndMaybeSwitch();

        totalStartClock = Now();
        StartFrom(currentStage);
    }

    private void SetupSources()
    {
        allSources = new AudioSource[] { music1Forward, music1Reverse, music2, music3Forward, music3Reverse, music4 };
        baseVolumeMap.Clear();

        foreach (var src in allSources)
        {
            if (src == null) continue;
            if (force2D) src.spatialBlend = 0f;
            src.loop = true;
            src.pitch = 1f;         // reset pitch
            baseVolumeMap[src] = 0f;// start silent (base volume)
        }

        if (preloadAudio)
        {
            foreach (var src in allSources)
                if (src?.clip != null) src.clip.LoadAudioData();
        }

        foreach (var src in allSources)
        {
            if (src == null) continue;
            if (src.clip == null)
                Debug.LogWarning($"[{name}] AudioSource '{src?.name}' has no clip assigned.");
            if (!src.isPlaying) src.Play();
        }

        // Start at Stage 2: Music2 only
        totalStartDSP  = AudioSettings.dspTime;
        totalStartReal = Time.realtimeSinceStartup;
        totalStartClock = Now();

        currentStage = Stage.M2Only;
        didInitialSegment = false;

        // Initialize master volume
        lastAppliedMasterVolume = -1f; // force apply
        ApplyMasterVolumeToAll();

        SetDebugStage(currentStage, 0f, 0f);
    }

    private IEnumerator CalibrateClockAndMaybeSwitch()
    {
        // Measure how much dspTime advances vs real time over a small period
        double r0 = Time.realtimeSinceStartup;
        double d0 = AudioSettings.dspTime;
        double end = r0 + calibrateSeconds;

        while (Time.realtimeSinceStartup < end)
            yield return null;

        double r1 = Time.realtimeSinceStartup;
        double d1 = AudioSettings.dspTime;

        double realDelta = Mathf.Max(0.0001f, (float)(r1 - r0));
        double dspDelta  = Mathf.Max(0.0001f, (float)(d1 - d0));
        dspToRealRatio = (float)(dspDelta / realDelta);

        bool ratioOK = Mathf.Abs(dspToRealRatio - 1f) <= ratioTolerance;
        if (!ratioOK)
        {
            // Switch to realtime clock if DSP is off-speed
            useRealtimeClock = true;
        }

        activeClockLabel = useRealtimeClock ? "Realtime" : "DSP";
        if (logStages)
            Debug.Log($"[{name}] Clock calibration: ratio dsp/real = {dspToRealRatio:F3}, using {activeClockLabel} clock.");
    }

    // Entry to start sequence from any stage, then loop Stage 3..10 forever
    private void StartFrom(Stage s)
    {
        if (sequenceCo != null) StopCoroutine(sequenceCo);
        // If starting from M2Only, we haven't finished initial segment yet
        didInitialSegment = (s != Stage.M2Only);
        sequenceCo = StartCoroutine(PlayFrom(s));
    }

    private IEnumerator PlayFrom(Stage s)
    {
        // Initial segment: Stage 2 -> Stage 9 -> Stage 3
        if (s == Stage.M2Only)
        {
            yield return Stage_M2Only();         // 2) Music2 only
            didInitialSegment = true;
            yield return Stage_Fwd3PlusM1();     // 9) INSERTED here
            s = Stage.M1withM3;                  // then continue with 3
        }

        // Finish current loop segment from s (if s belongs to the cycle)
        yield return PlaySegmentFromStage3(s);

        // Loop Stage 3..10 forever
        while (true)
        {
            foreach (var st in LoopCycle)
                yield return PlayOne(st);
        }
    }

    private IEnumerator PlaySegmentFromStage3(Stage s)
    {
        int idx = System.Array.IndexOf(LoopCycle, s);
        if (idx < 0) idx = 0; // if s not in cycle, start from Stage 3

        for (int i = idx; i < LoopCycle.Length; i++)
            yield return PlayOne(LoopCycle[i]);
    }

    private IEnumerator PlayOne(Stage s)
    {
        switch (s)
        {
            case Stage.M2Only:                yield return Stage_M2Only(); break;
            case Stage.M1withM3:              yield return Stage_M1withM3(); break;
            case Stage.ToM4:                  yield return Stage_ToM4(); break;
            case Stage.ToM3Reversed:          yield return Stage_ToM3Reversed(); break;
            case Stage.Rev3PlusRev1:          yield return Stage_Rev3PlusRev1(); break;
            case Stage.Rev3Only:              yield return Stage_Rev3Only(); break;
            case Stage.Rev3PlusRev1_2:        yield return Stage_Rev3PlusRev1_2(); break;
            case Stage.Fwd3PlusM1:            yield return Stage_Fwd3PlusM1(); break;
            case Stage.Rev3PlusRev1_End:      yield return Stage_Rev3PlusRev1_End(); break;
        }
    }

    // ===== STAGES =====

    // 2) Music2 only
    private IEnumerator Stage_M2Only()
    {
        EnterStage(Stage.M2Only);
        yield return WaitForClipReady(music2);
        float dur = ComputeDurationOrFallback(music2);
        yield return WaitClockSeconds(dur);
    }

    // 9) Music3 forward + Music1 @ max (inserted after 2 on first pass, also in loop)
    private IEnumerator Stage_Fwd3PlusM1()
    {
        EnterStage(Stage.Fwd3PlusM1);
        yield return WaitForClipReady(music3Forward);
        float dur = ComputeDurationOrFallback(music3Forward);
        yield return WaitClockSeconds(dur);
    }

    // 3) Music1 @ max + Music3 forward
    private IEnumerator Stage_M1withM3()
    {
        EnterStage(Stage.M1withM3);
        yield return WaitForClipReady(music3Forward);
        float dur = ComputeDurationOrFallback(music3Forward);
        yield return WaitClockSeconds(dur);
    }

    // 4) Turn off Music1 and switch to Music4 (brief if quickSkip)
    private IEnumerator Stage_ToM4()
    {
        EnterStage(Stage.ToM4);
        yield return WaitForClipReady(music4);

        float dur = quickSkipMusic4
            ? Mathf.Max(music4HoldSeconds, crossfadeDuration)
            : ComputeDurationOrFallback(music4);

        yield return WaitClockSeconds(dur);
    }

    // 5) Music3 reversed (solo)
    private IEnumerator Stage_ToM3Reversed()
    {
        EnterStage(Stage.ToM3Reversed);
        yield return WaitForClipReady(music3Reverse);
        float dur = ComputeDurationOrFallback(music3Reverse);
        yield return WaitClockSeconds(dur);
    }

    // 6) Reversed Music3 + Reversed Music1
    private IEnumerator Stage_Rev3PlusRev1()
    {
        EnterStage(Stage.Rev3PlusRev1);
        yield return WaitForClipReady(music3Reverse);
        yield return WaitForClipReady(music1Reverse);
        float dur = Mathf.Max(ComputeDurationOrFallback(music3Reverse), ComputeDurationOrFallback(music1Reverse));
        yield return WaitClockSeconds(dur);
    }

    // 7) Reversed Music3 only
    private IEnumerator Stage_Rev3Only()
    {
        EnterStage(Stage.Rev3Only);
        yield return WaitForClipReady(music3Reverse);
        float dur = ComputeDurationOrFallback(music3Reverse);
        yield return WaitClockSeconds(dur);
    }

    // 8) Reversed Music3 + Reversed Music1 (again)
    private IEnumerator Stage_Rev3PlusRev1_2()
    {
        EnterStage(Stage.Rev3PlusRev1_2);
        yield return WaitForClipReady(music3Reverse);
        yield return WaitForClipReady(music1Reverse);
        float dur = Mathf.Max(ComputeDurationOrFallback(music3Reverse), ComputeDurationOrFallback(music1Reverse));
        yield return WaitClockSeconds(dur);
    }

    // 10) Reversed Music3 + Reversed Music1, then loop to Stage 3
    private IEnumerator Stage_Rev3PlusRev1_End()
    {
        EnterStage(Stage.Rev3PlusRev1_End);
        yield return WaitForClipReady(music3Reverse);
        yield return WaitForClipReady(music1Reverse);
        float dur = Mathf.Max(ComputeDurationOrFallback(music3Reverse), ComputeDurationOrFallback(music1Reverse));
        yield return WaitClockSeconds(dur);
    }

    // ===== ENTER STAGE: fades + pitch reset per stage =====

    private void EnterStage(Stage s)
    {
        currentStage = s;
        SetDebugStage(s, 0f, 0f);
        if (logStages) Debug.Log($"[{name}] Enter stage {(int)s} - {s}");

        void Full(AudioSource a) => FadeBaseTo(a, 1f, crossfadeDuration);
        void Kill(AudioSource a) => FadeBaseTo(a, 0f, crossfadeDuration);
        void Pitch1(AudioSource a) { if (a != null) a.pitch = 1f; }

        switch (s)
        {
            case Stage.M2Only:
                Pitch1(music2);
                Full(music2);
                Kill(music1Forward); Kill(music3Forward); Kill(music4); Kill(music3Reverse); Kill(music1Reverse);
                break;

            case Stage.Fwd3PlusM1:
                Pitch1(music3Forward);
                Full(music3Forward);
                Full(music1Forward); if (music1Forward != null) music1Forward.pitch = pitchMax;
                Kill(music1Reverse); Kill(music3Reverse); Kill(music4); Kill(music2);
                break;

            case Stage.M1withM3:
                Pitch1(music3Forward);
                Full(music1Forward); if (music1Forward != null) music1Forward.pitch = pitchMax;
                Full(music3Forward);
                Kill(music2); Kill(music4); Kill(music3Reverse); Kill(music1Reverse);
                break;

            case Stage.ToM4:
                Pitch1(music4);
                Kill(music1Forward); // turn off the simultaneous one (M1)
                Kill(music3Forward);
                Full(music4);
                Kill(music3Reverse); Kill(music1Reverse);
                break;

            case Stage.ToM3Reversed:
                Pitch1(music3Reverse);
                Kill(music4); Kill(music3Forward);
                Full(music3Reverse); // solo reversed Music3
                Kill(music1Forward); Kill(music1Reverse);
                break;

            case Stage.Rev3PlusRev1:
                Pitch1(music3Reverse); Pitch1(music1Reverse);
                Full(music3Reverse); Full(music1Reverse);
                Kill(music1Forward); Kill(music3Forward); Kill(music4); Kill(music2);
                break;

            case Stage.Rev3Only:
                Pitch1(music3Reverse);
                Full(music3Reverse); Kill(music1Reverse);
                Kill(music1Forward); Kill(music3Forward); Kill(music4); Kill(music2);
                break;

            case Stage.Rev3PlusRev1_2:
                Pitch1(music3Reverse); Pitch1(music1Reverse);
                Full(music3Reverse); Full(music1Reverse);
                Kill(music1Forward); Kill(music3Forward); Kill(music4); Kill(music2);
                break;

            case Stage.Rev3PlusRev1_End:
                Pitch1(music3Reverse); Pitch1(music1Reverse);
                Full(music3Reverse); Full(music1Reverse);
                Kill(music1Forward); Kill(music3Forward); Kill(music4); Kill(music2);
                break;
        }
    }

    // ===== CLOCKED TIMING / FADES =====

    private double Now()
    {
        return useRealtimeClock ? (double)Time.realtimeSinceStartup : AudioSettings.dspTime;
    }

    private IEnumerator WaitClockSeconds(float seconds, System.Action<float> onProgress = null)
    {
        if (seconds <= 0f)
        {
            debugStageTime = 0f;
            debugStageTarget = 0f;
            yield break;
        }

        double t0 = Now();
        debugStageTarget = seconds;

        while (true)
        {
            double now = Now();
            float elapsed = (float)(now - t0);
            debugStageTime = Mathf.Min(elapsed, seconds);
            debugTotalTime = (float)(now - totalStartClock);

            float t = Mathf.Clamp01(seconds > 0f ? elapsed / seconds : 1f);
            onProgress?.Invoke(t);

            if (elapsed >= seconds) break;
            yield return null;
        }

        onProgress?.Invoke(1f);
    }

    private IEnumerator WaitForClipReady(AudioSource src)
    {
        if (src == null || src.clip == null) yield break;

        var clip = src.clip;
        if (clip.loadState == AudioDataLoadState.Unloaded && preloadAudio)
            clip.LoadAudioData();

        double t0 = Now();
        const double timeout = 10.0;

        while (true)
        {
            var ls = clip.loadState;
            if (ls == AudioDataLoadState.Loaded) break;
            if (ls == AudioDataLoadState.Failed)
            {
                Debug.LogError($"[{name}] {src.name} failed to load audio data. Will use fallback duration.");
                break;
            }
            if (Now() - t0 > timeout)
            {
                Debug.LogWarning($"[{name}] Timeout waiting for {src.name} to load. Using fallback.");
                break;
            }
            // Keep Inspector/HUD updated while waiting
            debugStageTime = (float)(Now() - t0);
            debugTotalTime = (float)(Now() - totalStartClock);
            yield return null;
        }
    }

    private float ComputeDurationOrFallback(AudioSource src)
    {
        if (src == null || src.clip == null)
            return Mathf.Max(0.1f, fallbackStageSecondsIfClipMissing);

        float p = src.pitch;
        if (!float.IsFinite(p) || Mathf.Abs(p) < 0.01f) p = 1f;
        return src.clip.length / Mathf.Abs(p);
    }

    // Master-aware fade: animates base volume and applies masterVolume
    private void FadeBaseTo(AudioSource src, float targetBase, float duration)
    {
        if (src == null) return;
        if (!baseVolumeMap.ContainsKey(src)) baseVolumeMap[src] = 0f;

        if (fadeMap.TryGetValue(src, out var running) && running != null)
            StopCoroutine(running);

        fadeMap[src] = StartCoroutine(FadeBaseCoroutineClock(src, targetBase, duration));
    }

    private IEnumerator FadeBaseCoroutineClock(AudioSource src, float targetBase, float duration)
    {
        double start = Now();
        float startBase = baseVolumeMap.TryGetValue(src, out var b) ? b : src.volume / Mathf.Max(0.0001f, lastAppliedMasterVolume);
        targetBase = Mathf.Clamp01(targetBase);

        if (duration <= 0f)
        {
            baseVolumeMap[src] = targetBase;
            src.volume = targetBase * masterVolume;
            lastAppliedMasterVolume = masterVolume;
            yield break;
        }

        while (true)
        {
            double now = Now();
            float t = Mathf.Clamp01((float)((now - start) / duration));
            float curBase = Mathf.Lerp(startBase, targetBase, t);
            baseVolumeMap[src] = curBase;
            src.volume = curBase * masterVolume;
            lastAppliedMasterVolume = masterVolume;

            if (t >= 1f) break;
            yield return null;
        }
        src.volume = targetBase * masterVolume;
        baseVolumeMap[src] = targetBase;
        lastAppliedMasterVolume = masterVolume;
    }

    private void ApplyMasterVolumeToAll()
    {
        foreach (var src in allSources)
        {
            if (src == null) continue;
            float baseVol = baseVolumeMap.TryGetValue(src, out var v) ? v : src.volume / Mathf.Max(0.0001f, lastAppliedMasterVolume);
            src.volume = baseVol * masterVolume;
        }
        lastAppliedMasterVolume = masterVolume;
    }

    private void SetDebugStage(Stage s, float elapsed, float target)
    {
        debugStageNumber = (int)s;
        debugStageName = s.ToString();
        debugStageTime = elapsed;
        debugStageTarget = target;
        debugTotalTime = (float)(Now() - totalStartClock);
    }

    // ===== Debug controls + master volume reapply =====

    void Update()
    {
        if (!Mathf.Approximately(masterVolume, lastAppliedMasterVolume))
            ApplyMasterVolumeToAll();

        // Shortcuts only when legacy input is enabled in Project Settings
        #if ENABLE_LEGACY_INPUT_MANAGER
        if (!enableShortcuts) return;

        if (Input.GetKeyDown(toggleHUDKey)) showHUD = !showHUD;
        if (Input.GetKeyDown(KeyCode.RightBracket)) StartFrom(NextStage(currentStage));
        if (Input.GetKeyDown(KeyCode.LeftBracket))  StartFrom(PrevStage(currentStage));
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetupSources();
            StartCoroutine(BootAndStart());
        }
        #endif
    }

    private Stage NextStage(Stage s)
    {
        // Initial: 2 -> 9 -> 3 -> 4 -> ... -> 10 -> 3...
        if (!didInitialSegment)
        {
            if (s == Stage.M2Only)            return Stage.Fwd3PlusM1;
            if (s == Stage.Fwd3PlusM1)        return Stage.M1withM3;
            if (s == Stage.Rev3PlusRev1_End)  return Stage.M1withM3;
            return (Stage)Mathf.Clamp(((int)s) + 1, (int)Stage.M2Only, (int)Stage.Rev3PlusRev1_End);
        }
        else
        {
            if (s == Stage.Rev3PlusRev1_End)  return Stage.M1withM3; // loop to Stage 3
            return (Stage)Mathf.Clamp(((int)s) + 1, (int)Stage.M1withM3, (int)Stage.Rev3PlusRev1_End);
        }
    }

    private Stage PrevStage(Stage s)
    {
        if (!didInitialSegment)
        {
            // Reverse of the initial ordering
            if (s == Stage.M2Only)            return Stage.Rev3PlusRev1_End; // wrap
            if (s == Stage.Fwd3PlusM1)        return Stage.M2Only;
            if (s == Stage.M1withM3)          return Stage.Fwd3PlusM1;
            return (Stage)Mathf.Clamp(((int)s) - 1, (int)Stage.M2Only, (int)Stage.Rev3PlusRev1_End);
        }
        else
        {
            if (s == Stage.M1withM3)          return Stage.Rev3PlusRev1_End; // wrap within loop
            return (Stage)Mathf.Clamp(((int)s) - 1, (int)Stage.M1withM3, (int)Stage.Rev3PlusRev1_End);
        }
    }

    // ===== Public controls (no input system required) =====
    [ContextMenu("Next Stage")]
    public void ContextNextStage() => StartFrom(NextStage(currentStage));

    [ContextMenu("Prev Stage")]
    public void ContextPrevStage() => StartFrom(PrevStage(currentStage));

    [ContextMenu("Reset Player")]
    public void ContextResetPlayer()
    {
        SetupSources();
        StartCoroutine(BootAndStart());
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        ApplyMasterVolumeToAll();
    }

    // ===== HUD (display only; toggle via legacy key or set showHUD in Inspector) =====
    void OnGUI()
    {
        if (!showHUD) return;

        float w = 560f;
        float h = 220f;
        Rect r = new Rect(10, 10, w, h);
        GUI.Box(r, "Pitch Aware Music Player");

        float line = 22f;
        float y = r.y + 25f;
        float x = r.x + 10f;

        float progress = (debugStageTarget > 0f && debugStageTarget < float.PositiveInfinity)
            ? Mathf.Clamp01(debugStageTime / debugStageTarget)
            : 0f;

        GUI.Label(new Rect(x, y, w - 20, 20), $"Stage: {debugStageNumber} - {debugStageName}");
        y += line;
        GUI.Label(new Rect(x, y, w - 20, 20), $"Stage Time: {debugStageTime:F2}s / {(float.IsInfinity(debugStageTarget) ? "waiting…" : debugStageTarget.ToString("F2")+"s")}  ({progress * 100f:F0}%)");
        y += line;

        // Clock info
        GUI.Label(new Rect(x, y, w - 20, 20), $"Clock: {activeClockLabel}  |  dsp/real ratio: {dspToRealRatio:F3}  |  TimeScale: {Time.timeScale:F2}");
        y += line;

        // Progress bar
        var barBg = new Rect(x, y, w - 20, 18);
        GUI.Box(barBg, GUIContent.none);
        var barFill = new Rect(barBg.x + 2, barBg.y + 2, (barBg.width - 4) * progress, barBg.height - 4);
        GUI.Box(barFill, GUIContent.none);
    }
}