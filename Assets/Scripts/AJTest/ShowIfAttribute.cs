using UnityEngine;

// Only shows a field in the Inspector while the named checkbox(es) (bool fields) are ticked.
//   [ShowIf("useJitter")]                 shown while Use Jitter is ticked
//   [ShowIf("usePitch", false)]           shown while Use Pitch is UNticked
//   [ShowIf("useJitter, twitchMode")]     shown while BOTH are ticked
//   [ShowIf("useJitter, !twitchMode")]    shown while Use Jitter is ticked AND Twitch Mode is not
public class ShowIfAttribute : PropertyAttribute
{
    public readonly string condition;
    public readonly bool showWhen;
    public ShowIfAttribute(string condition, bool showWhen = true)
    {
        this.condition = condition;
        this.showWhen = showWhen;
    }
}
