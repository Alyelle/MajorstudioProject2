using UnityEngine;

// Puts the 12 musical notes around a clock face and points the orbit at whichever note is playing.
// Melodies end up "drawing" shapes around the cursor.
// Reads a ChromaAnalyzer; SongMouseController reads this to set its orbit angle.
// Turned on/off by the "Use Pitch" checkbox on SongMouseController. It sets the DIRECTION of the fake cursor's arrow.
// Hover any field name in the Inspector to see what it does.
public class NoteOrbitDriver : MonoBehaviour
{
    public enum Layout
    {
        Chromatic,      // C, C#, D, D# ... around the circle: a step up in pitch = a small step around
        CircleOfFifths  // C, G, D, A ... around the circle: notes that sound good together sit next to each other
    }

    public enum AngleMode
    {
        StrongestNote,  // snaps to one of 12 positions
        WeightedBlend   // averages all notes, glides between positions (chords land "between" notes)
    }

    // ───────────────────────────────────────────── SOURCE
    [Header("1. Source")]
    [Tooltip("Where the note data comes from. Leave empty to use the ChromaAnalyzer on this object.")]
    public ChromaAnalyzer analyzer;

    // ───────────────────────────────────────────── MAPPING
    [Header("2. Notes around the circle")]
    [Tooltip("Chromatic: C, C#, D... in order, so melodies walking up the scale move smoothly around.\nCircleOfFifths: C, G, D, A..., so chord changes make smaller, more musical jumps.")]
    public Layout layout = Layout.Chromatic;

    [Tooltip("StrongestNote: snaps to 12 exact positions (sharper, more 'clock-like').\nWeightedBlend: mixes all notes, so it glides and chords land in between.")]
    public AngleMode angleMode = AngleMode.StrongestNote;

    [Tooltip("Rotates the whole clock face, in degrees. 90 = C at the top.")]
    [Range(-180f, 180f)] public float rotationOffset = 90f;

    [Tooltip("Ticked: notes go clockwise. Unticked: counter-clockwise.")]
    public bool clockwise = true;

    // ───────────────────────────────────────────── MOTION
    [Header("3. Motion")]
    [Tooltip("How fast the dot swings to a new note. Low = lazy sweeping arcs, high = snaps instantly.")]
    [Range(0.5f, 40f)] public float turnSpeed = 10f;

    [Tooltip("Ignore the notes when they aren't clear (drums only, noise). It holds its last direction instead.\n0 = always follow, 0.3 = only fairly clear notes.")]
    [Range(0f, 1f)] public float clarityThreshold = 0.15f;

    [Tooltip("Ticked: while the notes aren't clear, fall back to the normal volume spin.\nUnticked: freeze at the last note.")]
    public bool spinWhenUnclear = false;

    // ───────────────────────────────────────────── GIZMO
    [Header("4. Scene view helper")]
    [Tooltip("Draw the 12-note clock face around this object in the Scene view (and in the Game view if Gizmos is on).")]
    public bool drawNoteWheel = true;

    [Tooltip("Size of the drawn clock face, in world units (just a visual guide, it does not affect movement).")]
    [Range(0.2f, 5f)] public float wheelRadius = 1.2f;

    // ───────────────────────────────────────────── DEBUG
    [Header("Live readings")]
    [Tooltip("The note it's currently pointing at.")]
    [ReadOnly] public string pointingAt = "-";
    [Tooltip("Current angle in degrees.")]
    [ReadOnly] public float currentAngle;
    [Tooltip("Ticked while the notes are clear enough to follow.")]
    [ReadOnly] public bool following;

    float targetAngle;
    bool hasAngle;

    /// Current orbit angle in radians (what SongMouseController uses).
    public float AngleRadians => currentAngle * Mathf.Deg2Rad;

    /// 1 when this angle should be used right now, 0 when it shouldn't (nothing heard yet, or unclear + Spin When Unclear).
    /// SongMouseController uses it to blend between the note direction and plain spinning.
    public float EffectiveInfluence => (!hasAngle || (!following && spinWhenUnclear)) ? 0f : 1f;

    void Awake()
    {
        if (analyzer == null) analyzer = GetComponent<ChromaAnalyzer>();
    }

    void Update()
    {
        if (analyzer == null) { following = false; return; }

        following = analyzer.HasSignal && analyzer.clarity >= clarityThreshold;
        if (following)
        {
            targetAngle = angleMode == AngleMode.StrongestNote
                ? NoteAngle(analyzer.dominantNote)
                : BlendedAngle(analyzer.chroma);
            pointingAt = ChromaAnalyzer.NoteNames[analyzer.dominantNote];
            if (!hasAngle) { currentAngle = targetAngle; hasAngle = true; }
        }

        if (hasAngle)
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    // Angle (degrees) of a note's slot on the clock face.
    public float NoteAngle(int note)
    {
        int slot = layout == Layout.CircleOfFifths ? (note * 7) % 12 : note;
        float dir = clockwise ? -1f : 1f;
        return rotationOffset + dir * slot * 30f;
    }

    float BlendedAngle(float[] chroma)
    {
        Vector2 sum = Vector2.zero;
        for (int n = 0; n < 12; n++)
        {
            float a = NoteAngle(n) * Mathf.Deg2Rad;
            sum += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * chroma[n];
        }
        return sum.sqrMagnitude < 1e-8f ? targetAngle : Mathf.Atan2(sum.y, sum.x) * Mathf.Rad2Deg;
    }

    void OnDrawGizmos()
    {
        if (!drawNoteWheel) return;
        Vector3 c = transform.position;
        if (Application.isPlaying)
        {
            // Center the wheel on the real orbit center (the mouse), not on the orbiting dot.
            var smc = GetComponent<SongMouseController>();
            if (smc != null) c = smc.OrbitCenter;
        }

        for (int n = 0; n < 12; n++)
        {
            float a = NoteAngle(n) * Mathf.Deg2Rad;
            Vector3 p = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * wheelRadius;
            float strength = (Application.isPlaying && analyzer != null) ? analyzer.chroma[n] : 0.3f;
            Gizmos.color = Color.Lerp(new Color(1f, 1f, 1f, 0.25f), Color.cyan, strength);
            Gizmos.DrawSphere(p, 0.05f + 0.1f * strength);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Gizmos.color;
            UnityEditor.Handles.Label(p * 1f + (p - c).normalized * 0.2f, ChromaAnalyzer.NoteNames[n]);
#endif
        }

        if (Application.isPlaying && hasAngle)
        {
            float a = currentAngle * Mathf.Deg2Rad;
            Gizmos.color = following ? Color.yellow : Color.gray;
            Gizmos.DrawLine(c, c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * wheelRadius);
        }
    }
}
