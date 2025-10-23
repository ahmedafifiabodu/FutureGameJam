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
    public float rampDuration = 60f; // Stage 1 ramp time (seconds)

    [Header("Music4 behavior")]
    public bool quickSkipMusic4 = true;     // true = touch M4 briefly then move on
    public float music4HoldSeconds = 0.75f; // how long to hold M4 when quickSkipMusic4 is true

    [Header("Master Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f; // overall volume multiplier

    [Header("Robustness")]
    public bool force2D = true;
    public bool preloadAudio = true;
    public float fallbackStageSecondsIfClipMissing = 8f;

    [Header("Debug / HUD")]
    public bool showHUD = true;           // F1 toggle
    public bool enableShortcuts = true;   // [ prev, ] next, R reset
    public bool logStages = false;
    public bool skipRampOnPlay = false;   // start right after ramp (Music1+Music2) for testing
    public KeyCode toggleHUDKey = KeyCode.F1;

    private enum Stage
    {
        RampM1 = 1,         // 1) Music1 pitch 1->max over rampDuration
        M1withM2,           // 2) Music1 @ max + Music2
        M1withM3,           // 3) Music1 @ max + Music3 (forward)
        ToM4,               // 4) Turn off M1 and switch to Music4 (brief if quickSkip)
        ToM3Reversed,       // 5) Drop M4 and go to Music3 reversed (solo)
        Rev3PlusRev1,       // 6) Reversed Music3 + Reversed Music1
        Rev3Only,           // 7) Reversed Music3 only
        Rev3PlusRev1_2,     // 8) Reversed Music3 + Reversed Music1 again
        Fwd3PlusM1,         // 9) Music3 forward + Music1 @ max
        Rev3PlusRev1_End    // 10) Reversed Music3 + Reversed Music1, then loop to Stage 3
    }

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

    private Stage currentStage;
    private Coroutine sequenceCo;
    private readonly Dictionary<AudioSource, Coroutine> fadeMap = new Dictionary<AudioSource, Coroutine>();

    // Master volume support: keep base volumes separate from master multiplier
    private readonly Dictionary<AudioSource, float> baseVolumeMap = new Dictionary<AudioSource, float>();
    private float lastAppliedMasterVolume = 1f;
    private AudioSource[] allSources;

    private double totalStartDSP;

    void Awake()
    {
        var count = FindObjectsOfType<PitchAwareMusicPlayer>().Length;
        if (count > 1)
            Debug.LogWarning($"[{name}] Multiple PitchAwareMusicPlayer instances in scene: {count}");
    }

    void OnEnable()
    {
        SetupSources();
        StartFrom(currentStage);
    }

    void OnDisable()
    {
        if (sequenceCo != null) StopCoroutine(sequenceCo);
        foreach (var kv in fadeMap)
            if (kv.Value != null) StopCoroutine(kv.Value);
        fadeMap.Clear();
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
            src.pitch = 1f;   // reset pitch
            baseVolumeMap[src] = 0f; // start silent (base volume)
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

        if (music1Forward != null)
        {
            baseVolumeMap[music1Forward] = 1f; // audible at start
            music1Forward.pitch = 1f;          // will ramp in Stage 1
        }

        totalStartDSP = AudioSettings.dspTime;
        currentStage = skipRampOnPlay ? Stage.M1withM2 : Stage.RampM1;

        // Ensure initial application of masterVolume
        lastAppliedMasterVolume = -1f; // force apply
        ApplyMasterVolumeToAll();

        SetDebugStage(currentStage, 0f, 0f);
    }

    // Entry to start sequence from any stage, then loop Stage 3..10 forever
    private void StartFrom(Stage s)
    {
        if (sequenceCo != null) StopCoroutine(sequenceCo);
        sequenceCo = StartCoroutine(PlayFrom(s));
    }

    private IEnumerator PlayFrom(Stage s)
    {
        // Handle Stage 1 and Stage 2 (one-time)
        if (s == Stage.RampM1)
        {
            if (!skipRampOnPlay)
                yield return Stage_RampM1();
            s = Stage.M1withM2;
        }
        if (s == Stage.M1withM2)
        {
            yield return Stage_M1withM2();
            s = Stage.M1withM3;
        }

        // Finish current loop cycle from s (if s is within the cycle)
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
            case Stage.M1withM3:            yield return Stage_M1withM3(); break;
            case Stage.ToM4:                 yield return Stage_ToM4(); break;
            case Stage.ToM3Reversed:         yield return Stage_ToM3Reversed(); break;
            case Stage.Rev3PlusRev1:         yield return Stage_Rev3PlusRev1(); break;
            case Stage.Rev3Only:             yield return Stage_Rev3Only(); break;
            case Stage.Rev3PlusRev1_2:       yield return Stage_Rev3PlusRev1_2(); break;
            case Stage.Fwd3PlusM1:           yield return Stage_Fwd3PlusM1(); break;
            case Stage.Rev3PlusRev1_End:     yield return Stage_Rev3PlusRev1_End(); break;
        }
    }

    // ===== STAGES =====

    // 1) Music1 ramps 1 -> pitchMax over rampDuration
    private IEnumerator Stage_RampM1()
    {
        EnterStage(Stage.RampM1);
        yield return WaitDSPSeconds(rampDuration, t =>
        {
            if (music1Forward != null)
                music1Forward.pitch = Mathf.Lerp(1f, pitchMax, t);
        });
        if (music1Forward != null) music1Forward.pitch = pitchMax; // lock at max
    }

    // 2) Music1 @ max + Music2
    private IEnumerator Stage_M1withM2()
    {
        EnterStage(Stage.M1withM2);
        yield return WaitForClipReady(music2);
        float dur = ComputeDurationOrFallback(music2);
        yield return WaitDSPSeconds(dur);
    }

    // 3) Music1 @ max + Music3 forward
    private IEnumerator Stage_M1withM3()
    {
        EnterStage(Stage.M1withM3);
        yield return WaitForClipReady(music3Forward);
        float dur = ComputeDurationOrFallback(music3Forward);
        yield return WaitDSPSeconds(dur);
    }

    // 4) Turn off Music1 (simultaneous) and switch to Music4 (brief if quickSkip)
    private IEnumerator Stage_ToM4()
    {
        EnterStage(Stage.ToM4);
        yield return WaitForClipReady(music4);

        float dur = quickSkipMusic4
            ? Mathf.Max(music4HoldSeconds, crossfadeDuration)
            : ComputeDurationOrFallback(music4);

        yield return WaitDSPSeconds(dur);
    }

    // 5) Music3 reversed (solo)
    private IEnumerator Stage_ToM3Reversed()
    {
        EnterStage(Stage.ToM3Reversed);
        yield return WaitForClipReady(music3Reverse);
        float dur = ComputeDurationOrFallback(music3Reverse);
        yield return WaitDSPSeconds(dur);
    }

    // 6) Reversed Music3 + Reversed Music1
    private IEnumerator Stage_Rev3PlusRev1()
    {
        EnterStage(Stage.Rev3PlusRev1);
        yield return WaitForClipReady(music3Reverse);
        yield return WaitForClipReady(music1Reverse);
        float dur = Mathf.Max(ComputeDurationOrFallback(music3Reverse), ComputeDurationOrFallback(music1Reverse));
        yield return WaitDSPSeconds(dur);
    }

    // 7) Reversed Music3 (solo)
    private IEnumerator Stage_Rev3Only()
    {
        EnterStage(Stage.Rev3Only);
        yield return WaitForClipReady(music3Reverse);
        float dur = ComputeDurationOrFallback(music3Reverse);
        yield return WaitDSPSeconds(dur);
    }

    // 8) Reversed Music3 + Reversed Music1 (again)
    private IEnumerator Stage_Rev3PlusRev1_2()
    {
        EnterStage(Stage.Rev3PlusRev1_2);
        yield return WaitForClipReady(music3Reverse);
        yield return WaitForClipReady(music1Reverse);
        float dur = Mathf.Max(ComputeDurationOrFallback(music3Reverse), ComputeDurationOrFallback(music1Reverse));
        yield return WaitDSPSeconds(dur);
    }

    // 9) Music3 forward + Music1 @ max pitch
    private IEnumerator Stage_Fwd3PlusM1()
    {
        EnterStage(Stage.Fwd3PlusM1);
        yield return WaitForClipReady(music3Forward);
        float dur = ComputeDurationOrFallback(music3Forward);
        yield return WaitDSPSeconds(dur);
    }

    // 10) Reversed Music3 + Reversed Music1, then loop to Stage 3
    private IEnumerator Stage_Rev3PlusRev1_End()
    {
        EnterStage(Stage.Rev3PlusRev1_End);
        yield return WaitForClipReady(music3Reverse);
        yield return WaitForClipReady(music1Reverse);
        float dur = Mathf.Max(ComputeDurationOrFallback(music3Reverse), ComputeDurationOrFallback(music1Reverse));
        yield return WaitDSPSeconds(dur);
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
            case Stage.RampM1:
                if (music1Forward != null) music1Forward.pitch = 1f;
                Full(music1Forward);
                Kill(music2); Kill(music3Forward); Kill(music4); Kill(music3Reverse); Kill(music1Reverse);
                break;

            case Stage.M1withM2:
                Pitch1(music2);
                Full(music1Forward); if (music1Forward != null) music1Forward.pitch = pitchMax;
                Full(music2);
                Kill(music3Forward); Kill(music4); Kill(music3Reverse); Kill(music1Reverse);
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

            case Stage.Fwd3PlusM1:
                Pitch1(music3Forward);
                Full(music3Forward);
                Full(music1Forward); if (music1Forward != null) music1Forward.pitch = pitchMax;
                Kill(music1Reverse); Kill(music3Reverse); Kill(music4); Kill(music2);
                break;

            case Stage.Rev3PlusRev1_End:
                Pitch1(music3Reverse); Pitch1(music1Reverse);
                Full(music3Reverse); Full(music1Reverse);
                Kill(music1Forward); Kill(music3Forward); Kill(music4); Kill(music2);
                break;
        }
    }

    // ===== TIMING / FADES (DSP-driven) =====

    private IEnumerator WaitDSPSeconds(float seconds, System.Action<float> onProgress = null)
    {
        if (seconds <= 0f)
        {
            debugStageTime = 0f;
            debugStageTarget = 0f;
            yield break;
        }

        double t0 = AudioSettings.dspTime;
        debugStageTarget = seconds;

        while (true)
        {
            double now = AudioSettings.dspTime;
            float elapsed = (float)(now - t0);
            debugStageTime = Mathf.Min(elapsed, seconds);
            debugTotalTime = (float)(now - totalStartDSP);

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

        double t0 = AudioSettings.dspTime;
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
            if (AudioSettings.dspTime - t0 > timeout)
            {
                Debug.LogWarning($"[{name}] Timeout waiting for {src.name} to load. Using fallback.");
                break;
            }
            // Keep Inspector/HUD updated while waiting
            debugStageTime = (float)(AudioSettings.dspTime - t0);
            debugTotalTime = (float)(AudioSettings.dspTime - totalStartDSP);
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

        fadeMap[src] = StartCoroutine(FadeBaseCoroutineDSP(src, targetBase, duration));
    }

    private IEnumerator FadeBaseCoroutineDSP(AudioSource src, float targetBase, float duration)
    {
        double start = AudioSettings.dspTime;
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
            double now = AudioSettings.dspTime;
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
        debugTotalTime = (float)(AudioSettings.dspTime - totalStartDSP);
    }

    // ===== Debug controls + master volume reapply =====

    void Update()
    {
        if (!Mathf.Approximately(masterVolume, lastAppliedMasterVolume))
            ApplyMasterVolumeToAll();

        if (!enableShortcuts) return;

        if (Input.GetKeyDown(toggleHUDKey)) showHUD = !showHUD;
        if (Input.GetKeyDown(KeyCode.RightBracket)) StartFrom(NextStage(currentStage));
        if (Input.GetKeyDown(KeyCode.LeftBracket))  StartFrom(PrevStage(currentStage));
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetupSources();
            StartFrom(currentStage);
        }
    }

    private Stage NextStage(Stage s)
    {
        if (s == Stage.Rev3PlusRev1_End) return Stage.M1withM3; // loop to Stage 3
        return (Stage)Mathf.Clamp(((int)s) + 1, (int)Stage.RampM1, (int)Stage.Rev3PlusRev1_End);
    }

    private Stage PrevStage(Stage s)
    {
        if (s == Stage.M1withM3) return Stage.Rev3PlusRev1_End; // wrap back
        return (Stage)Mathf.Clamp(((int)s) - 1, (int)Stage.RampM1, (int)Stage.Rev3PlusRev1_End);
    }

    // ===== HUD =====

    void OnGUI()
    {
        if (!showHUD) return;

        float w = 520f;
        float h = 260f;
        Rect r = new Rect(10, 10, w, h);
        GUI.Box(r, "Pitch Aware Music Player (DSP-driven)");

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
        GUI.Label(new Rect(x, y, w - 20, 20), $"Total Time: {debugTotalTime:F2}s   TimeScale: {Time.timeScale:F2}  dspTime: {AudioSettings.dspTime:F2}");
        y += line;

        // Progress bar
        var barBg = new Rect(x, y, w - 20, 18);
        GUI.Box(barBg, GUIContent.none);
        var barFill = new Rect(barBg.x + 2, barBg.y + 2, (barBg.width - 4) * progress, barBg.height - 4);
        GUI.Box(barFill, GUIContent.none);
        y += 26f;

        // Master volume slider
        GUI.Label(new Rect(x, y, 120, 20), $"Master Volume: {masterVolume:F2}");
        float newMV = GUI.HorizontalSlider(new Rect(x + 120, y + 5, w - 150, 20), masterVolume, 0f, 1f);
        if (!Mathf.Approximately(newMV, masterVolume))
        {
            masterVolume = newMV;
            ApplyMasterVolumeToAll();
        }

        // Buttons
        float btnY = r.y + h - 30f;
        if (GUI.Button(new Rect(x, btnY, 60, 20), "< Prev")) StartFrom(PrevStage(currentStage));
        if (GUI.Button(new Rect(x + 70, btnY, 60, 20), "Next >")) StartFrom(NextStage(currentStage));
        if (GUI.Button(new Rect(x + 140, btnY, 60, 20), "Reset"))
        {
            SetupSources();
            StartFrom(currentStage);
        }
        GUI.Label(new Rect(x + 210, btnY, w - 230, 20), "[ / ] next/prev, R reset, F1 HUD");
    }
}