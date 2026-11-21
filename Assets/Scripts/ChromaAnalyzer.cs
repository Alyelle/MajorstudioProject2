using UnityEngine;

// "Which musical note is playing right now?"
// Listens to an AudioSource, folds every frequency down into the 12 notes (C, C#, D ... B),
// and reports how strong each note is. It doesn't move anything by itself; other scripts
// (like NoteOrbitDriver) read its results.
// Turned on/off by the "Use Pitch" checkbox on SongMouseController.
// Hover any field name in the Inspector to see what it does.
public class ChromaAnalyzer : MonoBehaviour
{
    public static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public enum Resolution { Low_1024 = 1024, Medium_2048 = 2048, High_4096 = 4096, Max_8192 = 8192 }

    // ───────────────────────────────────────────── INPUT
    [Header("1. Input")]
    [Tooltip("The AudioSource to listen to. Leave empty to use the one on this object (SongMouseController creates it).")]
    public AudioSource source;

    [Tooltip("How finely frequencies are split. Higher = can tell low notes apart better, but slightly more delay and CPU.\nMax_8192 is about 3 Hz per band, which is good enough for bass notes.")]
    public Resolution resolution = Resolution.Max_8192;

    // ───────────────────────────────────────────── RANGE
    [Header("2. Which frequencies count")]
    [Tooltip("Ignore everything below this (Hz). ~60 skips rumble and kick drums.\nRaise to ~150 to ignore the bassline and focus on melody/chords.")]
    [Range(20f, 1000f)] public float minFrequency = 60f;

    [Tooltip("Ignore everything above this (Hz). ~2000 covers most melodies and vocals.\nHigher lets cymbals and hiss in, which muddies the notes.")]
    [Range(500f, 8000f)] public float maxFrequency = 2000f;

    // ───────────────────────────────────────────── CLEANUP
    [Header("3. Cleanup")]
    [Tooltip("How smoothly the note readings change. Low = jumps on every note instantly. High = only follows longer, held notes.")]
    [Range(0f, 0.98f)] public float smoothing = 0.8f;

    [Tooltip("Squashes loud peaks so quieter notes still count. 1 = raw, 0.5 = gentler (recommended).")]
    [Range(0.2f, 1f)] public float compression = 0.5f;

    [Tooltip("Removes the 'background' level shared by all 12 notes, so the real notes stand out more. 0 = off, 1 = strong.")]
    [Range(0f, 1f)] public float backgroundRemoval = 0.6f;

    // ───────────────────────────────────────────── DEBUG
    [Header("Live readings")]
    [Tooltip("The strongest note right now.")]
    [ReadOnly] public string noteName = "-";
    [Tooltip("The strongest note as a number: 0 = C, 1 = C#, ... 11 = B.")]
    [ReadOnly] public int dominantNote;
    [Tooltip("How clearly one note stands out from the others, 0–1. Low = noisy or drums only, high = a clear note or chord.")]
    [ReadOnly] public float clarity;
    [Tooltip("Strength of each note, 0–1. Element 0 = C, 1 = C#, ... 11 = B.")]
    [ReadOnly] public float[] chroma = new float[12];

    /// True once it has heard something this frame.
    public bool HasSignal { get; private set; }

    float[] spectrum;
    int[] binToNote;           // which of the 12 notes each spectrum bin belongs to (-1 = ignored)
    int builtSize;
    float builtMin, builtMax, builtRate;
    readonly float[] raw = new float[12];

    void Update()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null || !source.isPlaying) { HasSignal = false; Fade(); return; }

        int size = (int)resolution;
        EnsureTables(size);
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        System.Array.Clear(raw, 0, 12);
        for (int i = 0; i < size; i++)
        {
            int n = binToNote[i];
            if (n >= 0) raw[n] += spectrum[i];
        }

        // Compress, remove shared background, normalize to 0..1.
        float mean = 0f;
        for (int n = 0; n < 12; n++) { raw[n] = Mathf.Pow(raw[n], compression); mean += raw[n]; }
        mean /= 12f;
        float max = 0f;
        for (int n = 0; n < 12; n++)
        {
            raw[n] = Mathf.Max(0f, raw[n] - mean * backgroundRemoval);
            if (raw[n] > max) max = raw[n];
        }

        HasSignal = max > 1e-6f;
        float keep = smoothing;
        for (int n = 0; n < 12; n++)
        {
            float v = HasSignal ? raw[n] / max : 0f;
            chroma[n] = Mathf.Lerp(v, chroma[n], keep);
        }

        UpdateSummary();
    }

    void Fade()
    {
        for (int n = 0; n < 12; n++) chroma[n] *= smoothing;
        UpdateSummary();
    }

    void UpdateSummary()
    {
        int best = 0; float bestV = 0f, sum = 0f;
        for (int n = 0; n < 12; n++)
        {
            sum += chroma[n];
            if (chroma[n] > bestV) { bestV = chroma[n]; best = n; }
        }
        dominantNote = best;
        // Clarity: 1 when one note dominates, near 0 when all notes are equal.
        clarity = sum > 1e-6f ? Mathf.Clamp01((bestV - (sum - bestV) / 11f) / Mathf.Max(bestV, 1e-6f)) : 0f;
        noteName = bestV > 1e-3f ? NoteNames[best] : "-";
    }

    void EnsureTables(int size)
    {
        float rate = AudioSettings.outputSampleRate;
        if (spectrum != null && builtSize == size && builtMin == minFrequency && builtMax == maxFrequency && builtRate == rate) return;

        spectrum = new float[size];
        binToNote = new int[size];
        float binHz = rate * 0.5f / size;
        for (int i = 0; i < size; i++)
        {
            float f = i * binHz;
            if (i == 0 || f < minFrequency || f > maxFrequency) { binToNote[i] = -1; continue; }
            float midi = 69f + 12f * Mathf.Log(f / 440f, 2f);
            binToNote[i] = ((Mathf.RoundToInt(midi) % 12) + 12) % 12;
        }
        builtSize = size; builtMin = minFrequency; builtMax = maxFrequency; builtRate = rate;
    }
}
