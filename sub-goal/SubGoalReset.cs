// Sub Goal reset for Streamer.bot, free from itsCRA1G (itscra1g.github.io/sub-goal)
// Give this action Stream Offline AND Stream Online triggers for each platform you stream on.
// The count goes back to 0 when you start a new stream, but not when your stream drops and comes back
// within 10 minutes.
using System;

public class CPHInline
{
    public bool Execute()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string ev = (Arg("__source") + Arg("triggerName")).Replace(" ", "");
        try { var mi = CPH.GetType().GetMethod("GetEventType", Type.EmptyTypes); if (mi != null) { var r = mi.Invoke(CPH, null); if (r != null && r.ToString() != "None") ev = r.ToString(); } } catch (Exception) { }

        if (ev.Contains("Offline")) { CPH.SetGlobalVar("subgoal_offAt", now, true); return true; }

        long off = CPH.GetGlobalVar<long>("subgoal_offAt", true);
        CPH.SetGlobalVar("subgoal_offAt", 0L, true);
        if (ev.Contains("Online") && (off == 0 || now - off <= 10 * 60)) return true;   // same stream, or first go-live after an old version

        CPH.SetGlobalVar("subgoal_count", 0, true);
        int goal = CPH.GetGlobalVar<int>("subgoal_goal", true);
        if (goal <= 0) goal = 10;
        CPH.WebsocketBroadcastJson("{\"subgoal\":{\"count\":0,\"goal\":" + goal + ",\"event\":\"reset\",\"user\":\"\",\"added\":0}}");
        return true;
    }

    private string Arg(string key)
    {
        object v;
        return args.TryGetValue(key, out v) && v != null ? v.ToString() : "";
    }
}
