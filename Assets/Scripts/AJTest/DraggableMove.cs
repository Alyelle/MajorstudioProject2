using UnityEngine;
using UnityEngine.InputSystem;

// Moves in a straight line and ricochets off the edges of a bounds area, like the old DVD screensaver logo.
// Hold left click while the FAKE cursor (e.g. the red dot) is over it to grab it, drag it, and let go to throw it.
// The object's own edges bounce (not its center), using its Collider2D/SpriteRenderer size.
// Hover any field name in the Inspector to see what it does.
public class DraggableMove : MonoBehaviour
{
    public enum BoundsMode
    {
        CameraView,   // bounce off the edges of what the camera sees
        BoundsObject, // bounce inside another object's Collider2D / Renderer
        CustomRect    // bounce inside a rectangle you type in
    }

    // ───────────────────────────────────────────── MOVEMENT
    [Header("1. Movement")]
    [Tooltip("How fast it travels on its own, in world units per second.")]
    [Range(0f, 30f)] public float speed = 3f;

    [Tooltip("Starting direction. It's normalized, so only the angle matters. Example: (1, 1) = diagonal up-right.")]
    public Vector2 startDirection = new Vector2(1f, 1f);

    [Tooltip("Ignore Start Direction and pick a random diagonal-ish angle each time it spawns.")]
    public bool randomStartDirection = true;

    // ───────────────────────────────────────────── DRAGGING
    [Header("2. Dragging")]
    [Tooltip("Let the player click and hold to grab it.")]
    public bool draggable = true;

    [Tooltip("The FAKE cursor that does the grabbing (e.g. MouseCenter, the red dot). Hold left click while the fake cursor is over this object to grab it.\nLeave empty to automatically use the object with SongMouseController in the scene.")]
    public Transform cursor;

    [Tooltip("Extra grab distance around this object, in world units. Raise it if the fake cursor moves too fast to land on it.")]
    [Range(0f, 2f)] public float grabPadding = 0.1f;

    [Tooltip("Ticked: when you let go it flies off in the direction (and speed) you flicked it.\nUnticked: it carries on in the direction it was going before you grabbed it.")]
    public bool throwOnRelease = true;

    [Tooltip("How much of your flick speed it keeps. 1 = exactly your flick speed, 0.5 = half.")]
    [Range(0f, 2f)] public float throwStrength = 1f;

    [Tooltip("The fastest a throw can send it, in world units per second.")]
    [Range(0f, 100f)] public float maxThrowSpeed = 25f;

    [Tooltip("After a throw, how quickly it eases back to its normal Speed.\nLow = a hard throw keeps flying fast for a while. High = settles back almost immediately.\n0 = never settles, it keeps the throw speed.")]
    [Range(0f, 10f)] public float settleRate = 1.5f;

    [Tooltip("Keep it inside the bounds while you're dragging it.")]
    public bool keepInBoundsWhileDragging = true;

    [Tooltip("Size multiplier while held, so it's clear you've grabbed it. 1 = no change.")]
    [Range(0.5f, 2f)] public float grabbedScale = 1.15f;

    // ───────────────────────────────────────────── BOUNDS
    [Header("3. Bounds (where it bounces)")]
    [Tooltip("CameraView: bounces off the screen edges.\nBoundsObject: bounces inside the object in 'Bounds Object' (uses its Collider2D, else its Renderer).\nCustomRect: bounces inside 'Custom Bounds'.")]
    public BoundsMode boundsMode = BoundsMode.CameraView;

    [Tooltip("Only for BoundsObject mode. Any object with a Collider2D or a SpriteRenderer, e.g. a big box sprite. Its outline becomes the walls.")]
    public Transform boundsObject;

    [Tooltip("Only for CustomRect mode. X/Y = bottom-left corner, W/H = width and height, in world units. Shown as a yellow box in the Scene view.")]
    public Rect customBounds = new Rect(-8f, -4.5f, 16f, 9f);

    [Tooltip("Shrinks the bounds inward by this amount on every side. Use a negative number to let it go past the edge a bit.")]
    public float padding = 0f;

    [Tooltip("Ticked: uses the object's own size so its edges touch the walls.\nUnticked: bounces when its center reaches the wall.")]
    public bool useOwnSize = true;

    // ───────────────────────────────────────────── BOUNCE
    [Header("4. On bounce")]
    [Tooltip("Speed multiplier applied on every bounce. 1 = constant speed, 1.05 = speeds up a little each hit.")]
    [Range(0.5f, 1.5f)] public float speedMultiplierOnBounce = 1f;

    [Tooltip("Never cruise faster than this, no matter how many bounces.")]
    [Range(0f, 100f)] public float maxSpeed = 20f;

    [Tooltip("Random angle wobble (degrees) added on each bounce, so it doesn't repeat the exact same path forever. 0 = perfect reflection.")]
    [Range(0f, 45f)] public float bounceAngleJitter = 0f;

    // ───────────────────────────────────────────── DEBUG
    [Header("Live readings")]
    [Tooltip("Ticked while you're holding it.")]
    [ReadOnly] public bool isDragging;
    [Tooltip("How fast it's moving right now.")]
    [ReadOnly] public float currentSpeed;
    [Tooltip("Number of wall hits so far.")]
    [ReadOnly] public int bounceCount;
    [Tooltip("Number of times it hit a corner exactly (both walls in the same frame).")]
    [ReadOnly] public int cornerHits;

    [Tooltip("Ticked while it's sitting in a DraggableHome.")]
    [ReadOnly] public bool isHome;

    /// The DraggableHome it's currently snapped to (null if it's free).
    public DraggableHome CurrentHome { get; private set; }

    // Only one Draggable can be grabbed per click (the first one under the cursor).
    static DraggableMove grabbed;

    /// The Draggable currently being held (null if none).
    public static DraggableMove Grabbed => grabbed;

    // The home it was just pulled out of. Ignored until it's dragged out of range,
    // so a "snap while dragging" home doesn't instantly grab it back.
    DraggableHome justLeft;

    Vector2 velocity;
    float cruiseSpeed;      // normal speed (grows with Speed Multiplier On Bounce)
    Vector3 grabOffset;
    Vector2 dragVelocity;   // smoothed mouse velocity while dragging, used for the throw
    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Start()
    {
        Vector2 dir = randomStartDirection ? RandomDiagonal() : startDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.one;
        cruiseSpeed = speed;
        velocity = dir.normalized * speed;

        if (cursor == null)
        {
            var fake = FindAnyObjectByType<SongMouseController>();
            if (fake != null) cursor = fake.transform;
            else Debug.LogWarning($"{name}: no fake cursor assigned or found, dragging will use the real mouse.");
        }
    }

    void OnDisable()
    {
        if (grabbed == this) grabbed = null;
        if (CurrentHome != null) CurrentHome.Undock(this);
    }

    void Update()
    {
        HandleDrag();
        if (isDragging) return;

        // Sitting in a home: glide to (and stay on) its position, no bouncing.
        if (CurrentHome != null)
        {
            Vector3 target = CurrentHome.transform.position;
            target.z = transform.position.z;
            float s = CurrentHome.snapSpeed;
            transform.position = s <= 0f
                ? target
                : Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-s * Time.deltaTime));
            currentSpeed = 0f;
            return;
        }

        // Cruise speed follows the Speed slider live, unless bounces are speeding it up.
        if (Mathf.Approximately(speedMultiplierOnBounce, 1f)) cruiseSpeed = speed;

        // Ease back toward cruise speed (matters after a throw).
        float mag = velocity.magnitude;
        if (settleRate > 0f)
            mag = Mathf.Lerp(mag, cruiseSpeed, 1f - Mathf.Exp(-settleRate * Time.deltaTime));
        if (velocity.sqrMagnitude < 0.0001f) velocity = RandomDiagonal();
        velocity = velocity.normalized * mag;
        currentSpeed = mag;

        Vector3 pos = transform.position + (Vector3)(velocity * Time.deltaTime);
        bool hitX, hitY;
        pos = ClampToBounds(pos, out hitX, out hitY, true);
        transform.position = pos;

        if (hitX || hitY) OnBounce(hitX && hitY);
    }

    void HandleDrag()
    {
        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (mouse == null || cam == null) return;

        Vector3 pointerWorld = PointerWorld(mouse, cam);

        bool lockedInHome = CurrentHome != null && !CurrentHome.allowPullOut;
        if (draggable && !lockedInHome && !isDragging && grabbed == null && mouse.leftButton.wasPressedThisFrame && ContainsPoint(pointerWorld))
        {
            justLeft = CurrentHome;
            if (CurrentHome != null) CurrentHome.Undock(this);
            grabbed = this;
            isDragging = true;
            grabOffset = transform.position - pointerWorld;
            dragVelocity = Vector2.zero;
            transform.localScale = baseScale * grabbedScale;
        }

        if (!isDragging) return;

        if (mouse.leftButton.isPressed && draggable)
        {
            Vector3 prev = transform.position;
            Vector3 target = pointerWorld + grabOffset;
            target.z = prev.z;
            if (keepInBoundsWhileDragging)
                target = ClampToBounds(target, out _, out _, false);
            transform.position = target;

            if (Time.deltaTime > 0f)
            {
                Vector2 frameVel = (target - prev) / Time.deltaTime;
                dragVelocity = Vector2.Lerp(dragVelocity, frameVel, 0.5f);
            }
            currentSpeed = dragVelocity.magnitude;

            // Homes set to "snap while dragging" grab it out of your hand as soon as it's close.
            if (justLeft != null && !justLeft.InRange(this)) justLeft = null;
            DraggableHome home = DraggableHome.FindHomeFor(this, false);
            if (home != null && home != justLeft) { EndDrag(); home.Dock(this); }
        }
        else
        {
            Release();
        }
    }

    void EndDrag()
    {
        isDragging = false;
        if (grabbed == this) grabbed = null;
        transform.localScale = baseScale;
    }

    void Release()
    {
        EndDrag();
        justLeft = null;

        // Dropped close enough to a home? Snap into it instead of flying off.
        DraggableHome home = DraggableHome.FindHomeFor(this, true);
        if (home != null) { home.Dock(this); return; }

        if (throwOnRelease && dragVelocity.magnitude * throwStrength > 0.5f)
            velocity = Vector2.ClampMagnitude(dragVelocity * throwStrength, maxThrowSpeed);
        // otherwise keep the old velocity and carry on.
    }

    // Called by DraggableHome. Don't call these yourself; use home.Dock(...) / home.Undock(...).
    internal void SetHome(DraggableHome home)
    {
        CurrentHome = home;
        isHome = home != null;
        if (home == null)
        {
            // Leaving a home: start moving again in a fresh direction.
            if (velocity.sqrMagnitude < 0.0001f) velocity = RandomDiagonal() * cruiseSpeed;
        }
    }

    // Keeps pos inside the bounds. If reflect is true, flips velocity on the axis that hit.
    Vector3 ClampToBounds(Vector3 pos, out bool hitX, out bool hitY, bool reflect)
    {
        Rect area = GetBounds();
        Vector2 half = useOwnSize ? GetHalfSize() : Vector2.zero;
        float minX = area.xMin + half.x, maxX = area.xMax - half.x;
        float minY = area.yMin + half.y, maxY = area.yMax - half.y;

        hitX = hitY = false;
        if (pos.x < minX) { pos.x = minX; hitX = true; if (reflect) velocity.x = Mathf.Abs(velocity.x); }
        else if (pos.x > maxX) { pos.x = maxX; hitX = true; if (reflect) velocity.x = -Mathf.Abs(velocity.x); }
        if (pos.y < minY) { pos.y = minY; hitY = true; if (reflect) velocity.y = Mathf.Abs(velocity.y); }
        else if (pos.y > maxY) { pos.y = maxY; hitY = true; if (reflect) velocity.y = -Mathf.Abs(velocity.y); }

        // If the bounds are smaller than the object, just center it on that axis.
        if (minX > maxX) pos.x = area.center.x;
        if (minY > maxY) pos.y = area.center.y;
        return pos;
    }

    void OnBounce(bool corner)
    {
        bounceCount++;
        if (corner) cornerHits++;

        if (bounceAngleJitter > 0f)
        {
            Vector2 v = Quaternion.Euler(0f, 0f, Random.Range(-bounceAngleJitter, bounceAngleJitter)) * velocity;
            // Keep the reflected direction (don't let jitter send it back into the wall).
            if (Mathf.Sign(v.x) == Mathf.Sign(velocity.x) && Mathf.Sign(v.y) == Mathf.Sign(velocity.y))
                velocity = v;
        }

        if (!Mathf.Approximately(speedMultiplierOnBounce, 1f))
            cruiseSpeed = Mathf.Min(cruiseSpeed * speedMultiplierOnBounce, maxSpeed);
    }

    bool ContainsPoint(Vector3 worldPoint)
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            return grabPadding <= 0f
                ? col.OverlapPoint(worldPoint)
                : Vector2.Distance(col.ClosestPoint(worldPoint), worldPoint) <= grabPadding;

        var rend = GetComponent<Renderer>();
        if (rend == null) return false;
        Bounds b = rend.bounds;
        // Treat it as an ellipse so round sprites aren't grabbable by their empty corners.
        Vector2 d = (Vector2)(worldPoint - b.center);
        Vector2 e = (Vector2)b.extents + Vector2.one * grabPadding;
        if (e.x <= 0f || e.y <= 0f) return false;
        return (d.x * d.x) / (e.x * e.x) + (d.y * d.y) / (e.y * e.y) <= 1f;
    }

    // Where the pointer is: the fake cursor if there is one, otherwise the real mouse.
    Vector3 PointerWorld(Mouse mouse, Camera cam)
    {
        if (cursor != null)
        {
            Vector3 p = cursor.position;
            p.z = transform.position.z;
            return p;
        }
        Vector3 screen = mouse.position.ReadValue();
        screen.z = transform.position.z - cam.transform.position.z;
        return cam.ScreenToWorldPoint(screen);
    }

    Rect GetBounds()
    {
        Rect r;
        switch (boundsMode)
        {
            case BoundsMode.BoundsObject when boundsObject != null:
                Bounds b;
                var col = boundsObject.GetComponent<Collider2D>();
                var rend = boundsObject.GetComponent<Renderer>();
                if (col != null) b = col.bounds;
                else if (rend != null) b = rend.bounds;
                else b = new Bounds(boundsObject.position, boundsObject.lossyScale);
                r = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
                break;

            case BoundsMode.CustomRect:
                r = customBounds;
                break;

            default: // CameraView (also the fallback if BoundsObject is empty)
                Camera cam = Camera.main;
                if (cam == null) return customBounds;
                float z = transform.position.z - cam.transform.position.z;
                Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
                Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));
                r = Rect.MinMaxRect(bl.x, bl.y, tr.x, tr.y);
                break;
        }
        return Rect.MinMaxRect(r.xMin + padding, r.yMin + padding, r.xMax - padding, r.yMax - padding);
    }

    Vector2 GetHalfSize()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) return col.bounds.extents;
        var rend = GetComponent<Renderer>();
        if (rend != null) return rend.bounds.extents;
        return Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        Rect r = GetBounds();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(r.center.x, r.center.y, transform.position.z), new Vector3(r.width, r.height, 0f));
    }

    Vector2 RandomDiagonal()
    {
        // Avoid nearly horizontal/vertical angles so it actually bounces around.
        float angle = Random.Range(20f, 70f) + 90f * Random.Range(0, 4);
        return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}
