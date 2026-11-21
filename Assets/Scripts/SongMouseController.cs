using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// THE FAKE CURSOR
// A red dot placed relative to your real mouse using a VECTOR (an arrow):
//
//     dot position = mouse + direction × magnitude
//
//   PITCH  -> DIRECTION  (which way the arrow points: at the note being played, on a 12-note clock face)
//   VOLUME -> MAGNITUDE  (how long the arrow is: quiet = short, loud = long)
//   With Volume off, magnitude is 0, so the dot sits right on the mouse.
//
// HOW THE INSPECTOR IS ORGANIZED
//   SONG / PEN / CURSOR = basic setup, always on.
//   Then one section per EFFECT. Each has a "Use ..." checkbox:
//     tick it to turn that effect on (its settings appear underneath), untick to turn it off.
//       PITCH       -> direction
//       VOLUME      -> magnitude
//       HIGHS       -> jitter / shake
//       BASS BEATS  -> kicks the dot sideways on each beat
//       BASS        -> dot grows and shrinks
//   ADVANCED = rarely needed. LIVE READINGS = what the song is doing right now.
//
// Hover any field name in the Inspector to see what it does.
public class SongMouseController : MonoBehaviour
{
    // ═════════════════════════════════════════════ BASICS
    [Header("SONG")]
    [Tooltip("The music that drives the movement. Drag an audio file from your Assets here.\n\nTip: in the song's import settings, set Load Type to 'Decompress On Load' so the whole song can be scanned at start.")]
    public AudioClip song;

    [Tooltip("Restart the song when it ends.")]
    public bool loop = true;

    [Tooltip("How loud YOU hear the song. Does not change the movement.")]
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("PEN (drawing)")]
    [Tooltip("The PenManager that leaves marks. In AJ_Test this is the PenManager on HandCenter. Leave empty to turn drawing off.")]
    public PenManager penManager;

    [Tooltip("Draw while you hold the left mouse button.")]
    public bool drawOnClick = true;

    [Tooltip("Draw all the time, without clicking. The dot leaves a trail as it orbits.")]
    public bool drawAutomatically = false;

    [Tooltip("Also leave a mark automatically on every detected beat, even without clicking.")]
    public bool drawOnBeat = false;

    [Header("CURSOR")]
    [Tooltip("Hide the real Windows mouse pointer while playing, so only the fake cursor (this red dot) is visible.")]
    public bool hideRealCursor = false;

    // ═════════════════════════════════════════════ EFFECTS
    [Header("EFFECT: PITCH  →  DIRECTION of the arrow")]
    [Tooltip("ON: the 12 notes sit around a clock face and the arrow points at whichever note is playing.\nOFF: the arrow just spins around at Spin Speed.\n\nDetailed pitch settings are on the 'Chroma Analyzer' (listening) and 'Note Orbit Driver' (clock face) components on this same object. They're added automatically if missing.")]
    public bool usePitch = true;

    [ShowIf("usePitch", false)]
    [Tooltip("Only when Pitch is OFF: how fast the arrow spins around. 1 = slow, 6 ≈ one full turn per second.")]
    [Range(0f, 20f)] public float spinSpeed = 2f;

    [ShowIf("usePitch", false)]
    [Tooltip("Only when Pitch is OFF: vocals, guitars and synths make it spin faster. 0 = off.")]
    [Range(0f, 5f)] public float midsSpinBoost = 1f;

    [Header("EFFECT: VOLUME  →  MAGNITUDE (length) of the arrow")]
    [Tooltip("ON: quiet = short arrow (dot near the mouse), loud = long arrow (dot far from the mouse).\nOFF: length is 0, so the dot sits right on the mouse.")]
    public bool useVolume = true;

    [ShowIf("useVolume")]
    [Tooltip("Arrow length in the song's QUIETEST parts, in world units. 0 = the dot sits on the mouse when quiet.")]
    [Range(0f, 5f)] public float quietMagnitude = 0f;

    [ShowIf("useVolume")]
    [Tooltip("Arrow length in the song's LOUDEST parts, in world units.")]
    [FormerlySerializedAs("orbitRadius")]
    [Range(0f, 5f)] public float loudMagnitude = 1.2f;

    [ShowIf("useVolume")]
    [Tooltip("Most songs barely change in volume. When ON, the song's own quiet parts are treated as 'quiet' and its loud parts as 'loud', so the movement has a big range.\nTurn OFF to use the raw volume instead.")]
    public bool stretchVolume = true;

    [ShowIf("useVolume")]
    [Tooltip("(Volume stretch) Anything this quiet or quieter counts as 'slowest'.\n0.1 = the quietest 10% of the song.")]
    [Range(0f, 0.5f)] public float quietPercentile = 0.1f;

    [ShowIf("useVolume")]
    [Tooltip("(Volume stretch) Anything this loud or louder counts as 'fastest'.\n0.95 = only the loudest 5% of the song hits full speed.")]
    [Range(0.5f, 1f)] public float loudPercentile = 0.95f;

    [ShowIf("useVolume")]
    [Tooltip("(Volume stretch) How dramatic the slow/fast difference is.\n1 = even. 2–3 = quiet stays slow longer, loud kicks in harder. 5+ = almost on/off.")]
    [Range(1f, 6f)] public float contrast = 2.5f;

    [ShowIf("useVolume")]
    [Tooltip("(Volume stretch) Only used if the song couldn't be scanned at start (Load Type = Streaming). How fast the live quiet/loud levels adjust.")]
    [Range(0.01f, 1f)] public float adaptRate = 0.05f;

    [Header("EFFECT: HIGHS  →  jitter (random movement)")]
    [Tooltip("ON: the dot makes random movements on top of everything else.")]
    public bool useJitter = true;

    [ShowIf("useJitter")]
    [Tooltip("Pick the style of random movement:\nTICKED = TWITCH: every so often (random timing) it jumps somewhere random, then settles back. Less often, bigger.\nUNTICKED = SHAKE: a constant fast shake driven by hi-hats and cymbals.")]
    public bool twitchMode = true;

    // ── Twitch settings
    [ShowIf("useJitter, twitchMode")]
    [Tooltip("How far each twitch throws the dot, in world units.")]
    [Range(0f, 5f)] public float twitchSize = 0.6f;

    [ShowIf("useJitter, twitchMode")]
    [Tooltip("Shortest wait between twitches, in seconds.")]
    [Range(0.05f, 10f)] public float twitchMinInterval = 0.8f;

    [ShowIf("useJitter, twitchMode")]
    [Tooltip("Longest wait between twitches, in seconds. Each wait is a random time between Min and Max, so it feels unpredictable.\nSet both the same for a steady rhythm.")]
    [Range(0.05f, 10f)] public float twitchMaxInterval = 2.5f;

    [ShowIf("useJitter, twitchMode")]
    [Tooltip("How fast it moves to the twitch spot. Low = a quick slide, high = an instant snap.")]
    [Range(1f, 60f)] public float twitchSnappiness = 25f;

    [ShowIf("useJitter, twitchMode")]
    [Tooltip("How long it stays at the twitch spot before going back, in seconds.")]
    [Range(0f, 3f)] public float twitchHoldTime = 0.15f;

    [ShowIf("useJitter, twitchMode")]
    [Tooltip("Ticked: after each twitch it goes back to where it should be.\nUnticked: it stays pushed off until the next twitch moves it somewhere else.")]
    public bool twitchReturns = true;

    [ShowIf("useJitter, twitchMode")]
    [Tooltip("How much the hi-hats/cymbals (highs) affect twitches.\n0 = always full Twitch Size, ignore the music.\n1 = bigger twitches during busy highs, small ones in calm parts; twitches also come sooner when the highs are busy.")]
    [Range(0f, 1f)] public float twitchFollowsHighs = 0.5f;

    // ── Shake settings
    [ShowIf("useJitter, !twitchMode")]
    [Tooltip("How far it shakes, in world units (scaled by how busy the highs are).")]
    [Range(0f, 2f)] public float jitterAmount = 0.1f;

    [ShowIf("useJitter, !twitchMode")]
    [Tooltip("How fast it shakes. Low = slow wandering, high = fast buzzing. (Was always 25 before.)")]
    [Range(0.5f, 40f)] public float shakeSpeed = 25f;

    [Header("EFFECT: BASS BEATS  →  kick")]
    [Tooltip("ON: each bass hit knocks the dot sideways, then it springs back.")]
    public bool useBeatKick = true;

    [ShowIf("useBeatKick")]
    [Tooltip("How hard each beat knocks it.")]
    [Range(0f, 10f)] public float kickStrength = 2.5f;

    [ShowIf("useBeatKick")]
    [Tooltip("How much louder than usual the bass must be to count as a beat.\nLower = more beats. Higher = only the big hits.\n(Also used by Draw On Beat.)")]
    [Range(1f, 3f)] public float beatThreshold = 1.4f;

    [ShowIf("useBeatKick")]
    [Tooltip("Shortest time between two beats, in seconds. Stops one drum hit counting twice.")]
    [Range(0.05f, 1f)] public float beatCooldown = 0.2f;

    [ShowIf("useBeatKick")]
    [Tooltip("How strongly it's pulled back after a kick. Higher = snaps back faster.")]
    [Range(1f, 200f)] public float kickSpring = 60f;

    [ShowIf("useBeatKick")]
    [Tooltip("How quickly the bounce settles. Low = wobbly, high = settles right away.")]
    [Range(0f, 30f)] public float kickDamping = 8f;

    [Header("EFFECT: BASS  →  size pulse")]
    [Tooltip("ON: the dot (and the white circle) grow with the bass.")]
    [FormerlySerializedAs("pulseScale")]
    public bool usePulse = true;

    [ShowIf("usePulse")]
    [Tooltip("How much bigger it gets. 0.5 = up to 50% bigger.")]
    [Range(0f, 2f)] public float pulseAmount = 0.5f;

    // ═════════════════════════════════════════════ ADVANCED
    [Header("ADVANCED")]
    [Tooltip("Tick to show rarely-needed settings.")]
    public bool showAdvanced = false;

    [ShowIf("showAdvanced")]
    [Tooltip("ON: the circle is always centered exactly on your cursor.\nOFF: the center trails behind the cursor (slow when quiet, fast when loud).")]
    public bool lockCenterToMouse = true;

    [ShowIf("showAdvanced")]
    [Tooltip("1 = circle around your mouse. 0 = ignore the mouse and circle the middle of the screen.")]
    [Range(0f, 1f)] public float mouseInfluence = 1f;

    [ShowIf("showAdvanced")]
    [Tooltip("Only when Lock Center To Mouse is OFF: catch-up speed in QUIET parts.")]
    [Range(0f, 50f)] public float minFollowSpeed = 0.5f;

    [ShowIf("showAdvanced")]
    [Tooltip("Only when Lock Center To Mouse is OFF: catch-up speed in LOUD parts.")]
    [Range(0f, 50f)] public float maxFollowSpeed = 25f;

    [ShowIf("showAdvanced")]
    [Tooltip("Boosts the bass/mids/highs readings. Raise if jitter, pulse and kicks feel too weak.")]
    [Range(0.5f, 50f)] public float sensitivity = 8f;

    [ShowIf("showAdvanced")]
    [Tooltip("How quickly the readings react. Low = smooth and floaty, high = twitchy.")]
    [Range(1f, 40f)] public float levelSmoothing = 12f;

    [ShowIf("showAdvanced")]
    [Tooltip("The Note Orbit Driver used by the Pitch effect. Leave empty to use (or add) the one on this object.")]
    public NoteOrbitDriver noteOrbit;

    // ═════════════════════════════════════════════ LIVE READINGS
    [Header("LIVE READINGS (watch while playing)")]
    [Tooltip("Where the arrow points right now, in degrees (0 = right, 90 = up).")]
    [ReadOnly] public float direction;
    [Tooltip("How long the arrow is right now, in world units.")]
    [ReadOnly] public float magnitude;
    [Tooltip("Volume after stretching, 0–1. Drives speed and size when Volume is on.")]
    [ReadOnly] public float loudness;
    [Tooltip("Volume before stretching. Compare with Quiet Level / Loud Level.")]
    [ReadOnly] public float rawLoudness;
    [Tooltip("Raw volume that counts as 'quiet'.")]
    [ReadOnly] public float quietLevel;
    [Tooltip("Raw volume that counts as 'loud'.")]
    [ReadOnly] public float loudLevel;
    [Tooltip("Ticked if the whole song was scanned at start. If unticked, set the song's Load Type to 'Decompress On Load'.")]
    [ReadOnly] public bool prescanned;
    [Tooltip("Bass level, 0–1.")]
    [ReadOnly] public float bass;
    [Tooltip("Mids level, 0–1.")]
    [ReadOnly] public float mids;
    [Tooltip("Highs level, 0–1.")]
    [ReadOnly] public float highs;
    [Tooltip("Flashes on when a beat is detected.")]
    [ReadOnly] public bool beatThisFrame;

    const int SpectrumSize = 1024;
    readonly float[] spectrum = new float[SpectrumSize];
    readonly float[] samples = new float[SpectrumSize];

    AudioSource source;
    NewInputTest input;
    ChromaAnalyzer chroma;

    Vector3 followPos;
    float spinAngle;    // degrees; the direction used when Pitch is off
    float noteBlend;    // smoothed 0..1 amount the notes control the direction
    Vector3 kickOffset;
    Vector3 kickVelocity;
    float bassAverage;
    float beatTimer;
    Vector3 baseScale;
    float noiseSeed;
    Vector3 twitchOffset, twitchTarget;
    float twitchTimer = 1f, twitchHoldTimer;

    /// The point the dot orbits around (the mouse, when Lock Center To Mouse is on).
    public Vector3 OrbitCenter => followPos;

    void Awake()
    {
        input = new NewInputTest();

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        baseScale = transform.localScale;
        noiseSeed = Random.value * 100f;
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    // Keep the pitch components' checkboxes in sync with "Use Pitch" in the editor too.
    void OnValidate() => SyncPitchComponents(false);

    void Start()
    {
        followPos = transform.position;
        SyncPitchComponents(true);

        if (song == null)
        {
            Debug.LogWarning("SongMouseController: no song assigned — drag an AudioClip into the 'Song' field.");
            return;
        }
        prescanned = PrescanSong();
        if (!prescanned)
            Debug.LogWarning("SongMouseController: couldn't pre-scan the song (set its Load Type to 'Decompress On Load' or 'Compressed In Memory'). Falling back to live adaptive stretching.");

        source.clip = song;
        source.loop = loop;
        source.volume = volume;
        source.Play();
    }

    // Finds (and at runtime, adds if missing) the pitch components, and turns them on/off with Use Pitch.
    void SyncPitchComponents(bool addIfMissing)
    {
        if (chroma == null) chroma = GetComponent<ChromaAnalyzer>();
        if (noteOrbit == null) noteOrbit = GetComponent<NoteOrbitDriver>();
        if (addIfMissing && usePitch)
        {
            if (chroma == null) chroma = gameObject.AddComponent<ChromaAnalyzer>();
            if (noteOrbit == null) noteOrbit = gameObject.AddComponent<NoteOrbitDriver>();
        }
        if (chroma != null) chroma.enabled = usePitch;
        if (noteOrbit != null) noteOrbit.enabled = usePitch;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        source.volume = volume;
        Cursor.visible = !hideRealCursor;
        SyncPitchComponents(true);

        AnalyzeAudio(dt);


        // ── Center: your mouse (or trailing behind it, see Advanced)
        Vector3 mouseScreen = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
        Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector3 targetScreen = Vector3.Lerp(centerScreen, mouseScreen, mouseInfluence);
        targetScreen.z = Camera.main.nearClipPlane;
        Vector3 target = Camera.main.ScreenToWorldPoint(targetScreen);

        float followSpeed = Mathf.Lerp(minFollowSpeed, maxFollowSpeed, useVolume ? loudness : 0.5f);
        followPos = lockCenterToMouse
            ? target
            : Vector3.Lerp(followPos, target, 1f - Mathf.Exp(-followSpeed * dt));

        // ── DIRECTION (PITCH): point at the current note. Falls back to spinning when Pitch is off
        //    (or the notes are unclear and Note Orbit Driver's "Spin When Unclear" is ticked).
        spinAngle += (spinSpeed + mids * midsSpinBoost * 4f) * Mathf.Rad2Deg * dt;
        float wantBlend = (usePitch && noteOrbit != null && noteOrbit.isActiveAndEnabled)
            ? noteOrbit.EffectiveInfluence
            : 0f;
        noteBlend = Mathf.MoveTowards(noteBlend, wantBlend, 3f * dt);
        direction = noteBlend > 0f
            ? Mathf.LerpAngle(spinAngle, noteOrbit.currentAngle, noteBlend)
            : spinAngle;
        if (noteBlend >= 1f) spinAngle = direction; // so switching back to spin carries on from here
        direction = Mathf.Repeat(direction, 360f);
        spinAngle = Mathf.Repeat(spinAngle, 360f);

        // ── MAGNITUDE (VOLUME): quiet = short, loud = long. Off = 0 (dot on the mouse).
        magnitude = useVolume ? Mathf.Lerp(quietMagnitude, loudMagnitude, loudness) : 0f;

        // ── The vector
        float rad = direction * Mathf.Deg2Rad;
        Vector3 orbit = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * magnitude;

        // ── Jitter (HIGHS): occasional twitches, or a constant shake
        Vector3 jitter = Vector3.zero;
        if (useJitter && twitchMode)
            jitter = UpdateTwitch(dt);
        else if (useJitter)
        {
            float t = Time.time * shakeSpeed;
            jitter = new Vector3(
                Mathf.PerlinNoise(noiseSeed, t) - 0.5f,
                Mathf.PerlinNoise(t, noiseSeed) - 0.5f, 0f) * (2f * jitterAmount * highs);
        }
        if (!useJitter || !twitchMode) twitchOffset = twitchTarget = Vector3.zero;

        // ── Kick (BASS BEATS): random impulse, then a damped spring pulls it back.
        if (beatThisFrame)
        {
            if (useBeatKick) kickVelocity += (Vector3)(Random.insideUnitCircle.normalized * kickStrength * 10f);
            if (drawOnBeat && penManager != null) penManager.OnClickFrame(penManager.spreadDelay);
        }
        Vector3 springForce = -kickOffset * kickSpring - kickVelocity * kickDamping;
        kickVelocity += springForce * dt;
        kickOffset += kickVelocity * dt;

        Vector3 pos = followPos + orbit + jitter + kickOffset;
        pos.z = target.z;
        transform.position = pos;

        // ── Size pulse (BASS)
        transform.localScale = usePulse ? baseScale * (1f + bass * pulseAmount) : baseScale;

        // ── Pen
        bool clicking = drawOnClick && Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (penManager != null && (drawAutomatically || clicking))
            penManager.OnClickFrame(dt);

        if (input.Test.Restart.triggered)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Waits a random time, jumps to a random spot, holds, then (optionally) returns.
    Vector3 UpdateTwitch(float dt)
    {
        float highsMix = Mathf.Lerp(1f, highs, twitchFollowsHighs); // 1 = ignore highs

        // Busy highs make the timer run faster, so twitches come sooner.
        twitchTimer -= dt * Mathf.Lerp(1f, 0.5f + highs * 1.5f, twitchFollowsHighs);
        if (twitchTimer <= 0f)
        {
            twitchTarget = (Vector3)(Random.insideUnitCircle.normalized * twitchSize * Random.Range(0.6f, 1f) * highsMix);
            twitchHoldTimer = twitchHoldTime;
            float lo = Mathf.Min(twitchMinInterval, twitchMaxInterval);
            float hi = Mathf.Max(twitchMinInterval, twitchMaxInterval);
            twitchTimer = Random.Range(lo, hi);
        }

        if (twitchReturns && twitchHoldTimer > 0f)
        {
            // Only start counting the hold once it has (nearly) arrived.
            if ((twitchOffset - twitchTarget).sqrMagnitude < 0.0025f) twitchHoldTimer -= dt;
            if (twitchHoldTimer <= 0f) twitchTarget = Vector3.zero;
        }

        twitchOffset = Vector3.Lerp(twitchOffset, twitchTarget, 1f - Mathf.Exp(-twitchSnappiness * dt));
        return twitchOffset;
    }

    void AnalyzeAudio(float dt)
    {
        beatThisFrame = false;
        if (!source.isPlaying)
        {
            loudness = Mathf.Lerp(loudness, 0f, 1f - Mathf.Exp(-levelSmoothing * dt));
            bass = mids = highs = 0f;
            return;
        }

        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        source.GetOutputData(samples, 0);

        // RMS loudness (divide out the volume slider so it doesn't affect motion).
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++) sum += samples[i] * samples[i];
        float rms = Mathf.Sqrt(sum / samples.Length) / Mathf.Max(volume, 0.0001f);

        // Bin width = (sampleRate / 2) / SpectrumSize  (~23 Hz at 48 kHz).
        float binHz = AudioSettings.outputSampleRate * 0.5f / SpectrumSize;
        float rawBass = BandEnergy(20f, 250f, binHz);
        float rawMids = BandEnergy(250f, 2000f, binHz);
        float rawHighs = BandEnergy(2000f, 10000f, binHz);

        float k = 1f - Mathf.Exp(-levelSmoothing * dt);
        rawLoudness = Mathf.Lerp(rawLoudness, rms, k);
        loudness = stretchVolume ? StretchLoudness(rawLoudness, dt) : Mathf.Clamp01(rawLoudness * sensitivity);
        bass = Mathf.Lerp(bass, Mathf.Clamp01(rawBass * sensitivity * 4f), k);
        mids = Mathf.Lerp(mids, Mathf.Clamp01(rawMids * sensitivity * 8f), k);
        highs = Mathf.Lerp(highs, Mathf.Clamp01(rawHighs * sensitivity * 20f), k);

        // Beat detection: instantaneous bass vs. its running average.
        beatTimer -= dt;
        bassAverage = Mathf.Lerp(bassAverage, rawBass, 1f - Mathf.Exp(-1.5f * dt));
        if (beatTimer <= 0f && rawBass > bassAverage * beatThreshold && rawBass > 0.001f)
        {
            beatThisFrame = true;
            beatTimer = beatCooldown;
        }
    }

    // Map the song's own quiet..loud range onto 0..1, then exaggerate it with an S-curve.
    float StretchLoudness(float rms, float dt)
    {
        if (!prescanned)
        {
            // Live fallback: min/max snap outward instantly and drift back inward slowly.
            if (loudLevel <= 0f) { quietLevel = rms; loudLevel = rms + 0.0001f; }
            float drift = 1f - Mathf.Exp(-adaptRate * dt);
            quietLevel = rms < quietLevel ? rms : Mathf.Lerp(quietLevel, rms, drift);
            loudLevel = rms > loudLevel ? rms : Mathf.Lerp(loudLevel, rms, drift);
        }

        float n = Mathf.Clamp01((rms - quietLevel) / Mathf.Max(loudLevel - quietLevel, 0.0001f));
        return n < 0.5f
            ? 0.5f * Mathf.Pow(2f * n, contrast)
            : 1f - 0.5f * Mathf.Pow(2f * (1f - n), contrast);
    }

    // Read the whole clip once, measure loudness in the same window size used at runtime,
    // and take percentiles as the song's "quiet" and "loud" levels.
    bool PrescanSong()
    {
        if (song.loadType == AudioClipLoadType.Streaming) return false;
        if (song.loadState != AudioDataLoadState.Loaded && !song.LoadAudioData()) return false;

        int channels = song.channels;
        int windows = song.samples / SpectrumSize;
        if (windows < 2) return false;

        float[] buffer = new float[SpectrumSize * channels];
        var levels = new System.Collections.Generic.List<float>(windows);
        for (int w = 0; w < windows; w++)
        {
            if (!song.GetData(buffer, w * SpectrumSize)) return false;
            float s = 0f;
            for (int i = 0; i < buffer.Length; i += channels) s += buffer[i] * buffer[i]; // channel 0, like GetOutputData
            float rms = Mathf.Sqrt(s / SpectrumSize);
            if (rms > 0.0005f) levels.Add(rms); // ignore digital silence (intros/outros)
        }
        if (levels.Count < 2) return false;

        levels.Sort();
        quietLevel = levels[Mathf.FloorToInt(quietPercentile * (levels.Count - 1))];
        loudLevel = levels[Mathf.FloorToInt(loudPercentile * (levels.Count - 1))];
        if (loudLevel <= quietLevel) loudLevel = quietLevel + 0.0001f;
        return true;
    }

    float BandEnergy(float lowHz, float highHz, float binHz)
    {
        int lo = Mathf.Clamp(Mathf.FloorToInt(lowHz / binHz), 0, SpectrumSize - 1);
        int hi = Mathf.Clamp(Mathf.CeilToInt(highHz / binHz), lo + 1, SpectrumSize);
        float e = 0f;
        for (int i = lo; i < hi; i++) e += spectrum[i];
        return e;
    }
}
