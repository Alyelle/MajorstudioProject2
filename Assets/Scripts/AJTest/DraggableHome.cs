using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// A spot that Draggables snap into. Drag a Draggable (with the fake cursor) close to it and let go:
// if it's within Snap Radius, it snaps to this home's position and stops bouncing.
// Hover any field name in the Inspector to see what it does.
public class DraggableHome : MonoBehaviour
{
    // ───────────────────────────────────────────── SNAPPING
    [Header("1. Snapping")]
    [Tooltip("How close (center to center, in world units) a Draggable must be to snap in. Shown as a green circle in the Scene view when selected.")]
    [Range(0.1f, 10f)] public float snapRadius = 1f;

    [Tooltip("Ticked: it only snaps when you LET GO inside the radius.\nUnticked: it snaps the moment you drag it into the radius, and pulls it out of your hand.")]
    public bool snapOnlyOnRelease = true;

    [Tooltip("How fast it glides into place. 0 = teleports instantly. Around 15 = a quick slide.")]
    [Range(0f, 50f)] public float snapSpeed = 15f;

    // ───────────────────────────────────────────── RULES
    [Header("2. Rules")]
    [Tooltip("How many Draggables this home can hold at once. Extra ones won't snap in. 0 = unlimited.")]
    [Min(0)] public int capacity = 1;

    [Tooltip("Ticked: you can grab a Draggable back out of the home.\nUnticked: once it's home, it's stuck there for good.")]
    public bool allowPullOut = true;

    [Tooltip("Optional. Only these Draggables can snap in. Leave the list empty to accept any Draggable.")]
    public List<DraggableMove> acceptOnly = new List<DraggableMove>();

    // ───────────────────────────────────────────── LOOK
    [Header("3. Highlight")]
    [Tooltip("Tint this home while you're dragging a Draggable close enough to snap.")]
    public bool highlightWhenInRange = true;

    [Tooltip("The tint used when a dragged Draggable is in range.")]
    public Color highlightColor = new Color(0.4f, 1f, 0.5f, 0.8f);

    [Tooltip("The tint used while it's holding a Draggable.")]
    public Color filledColor = new Color(1f, 1f, 1f, 0.6f);

    // ───────────────────────────────────────────── EVENTS
    [Header("4. Events (optional)")]
    [Tooltip("Runs when a Draggable snaps in. Hook up sounds, score, etc. with the + button.")]
    public UnityEvent<DraggableMove> onDocked = new UnityEvent<DraggableMove>();

    [Tooltip("Runs when a Draggable is pulled back out.")]
    public UnityEvent<DraggableMove> onUndocked = new UnityEvent<DraggableMove>();

    // ───────────────────────────────────────────── DEBUG
    [Header("Live readings")]
    [Tooltip("How many Draggables are in this home right now.")]
    [ReadOnly] public int occupantCount;
    [Tooltip("Ticked while a dragged Draggable is close enough to snap.")]
    [ReadOnly] public bool somethingInRange;

    static readonly List<DraggableHome> allHomes = new List<DraggableHome>();
    readonly List<DraggableMove> occupants = new List<DraggableMove>();

    SpriteRenderer spriteRenderer;
    Color baseColor;

    public IReadOnlyList<DraggableMove> Occupants => occupants;
    public bool IsFull => capacity > 0 && occupants.Count >= capacity;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;
    }

    void OnEnable() => allHomes.Add(this);

    void OnDisable()
    {
        allHomes.Remove(this);
        for (int i = occupants.Count - 1; i >= 0; i--) Undock(occupants[i]);
    }

    void Update()
    {
        DraggableMove held = DraggableMove.Grabbed;
        somethingInRange = held != null && held.isDragging && CanAccept(held) && InRange(held);

        if (spriteRenderer == null) return;
        if (highlightWhenInRange && somethingInRange) spriteRenderer.color = highlightColor;
        else if (occupants.Count > 0) spriteRenderer.color = filledColor;
        else spriteRenderer.color = baseColor;
    }

    public bool CanAccept(DraggableMove d)
    {
        if (d == null || !isActiveAndEnabled) return false;
        if (occupants.Contains(d)) return true;
        if (IsFull) return false;
        return acceptOnly.Count == 0 || acceptOnly.Contains(d);
    }

    public bool InRange(DraggableMove d)
    {
        Vector2 a = transform.position, b = d.transform.position;
        return Vector2.Distance(a, b) <= snapRadius;
    }

    public void Dock(DraggableMove d)
    {
        if (!CanAccept(d)) return;
        if (d.CurrentHome != null && d.CurrentHome != this) d.CurrentHome.Undock(d);
        if (!occupants.Contains(d)) occupants.Add(d);
        occupantCount = occupants.Count;
        d.SetHome(this);
        onDocked.Invoke(d);
    }

    public void Undock(DraggableMove d)
    {
        if (!occupants.Remove(d)) return;
        occupantCount = occupants.Count;
        if (d.CurrentHome == this) d.SetHome(null);
        onUndocked.Invoke(d);
    }

    // Closest home that will take this Draggable right now.
    // onRelease = true when the player just let go; false while still dragging.
    public static DraggableHome FindHomeFor(DraggableMove d, bool onRelease)
    {
        DraggableHome best = null;
        float bestDist = float.MaxValue;
        foreach (var h in allHomes)
        {
            if (!onRelease && h.snapOnlyOnRelease) continue;
            if (!h.CanAccept(d) || !h.InRange(d)) continue;
            float dist = Vector2.Distance(h.transform.position, d.transform.position);
            if (dist < bestDist) { bestDist = dist; best = h; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
