// Poll Timer for Streamer.bot, free from itsCRA1G (itscra1g.github.io/poll)
// Started by the Poll action. Waits until the poll's time is up, then asks the Poll action to close it.
using System;

public class CPHInline
{
    public bool Execute()
    {
        string id = CPH.GetGlobalVar<string>("poll_id", false) ?? "";
        while (true)
        {
            if ((CPH.GetGlobalVar<string>("poll_state", false) ?? "") != "open") return true;   // ended or cancelled early
            if ((CPH.GetGlobalVar<string>("poll_id", false) ?? "") != id) return true;         // a new poll took over
            long ends = CPH.GetGlobalVar<long>("poll_ends", false);
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= ends) break;
            CPH.Wait(1000);
        }
        CPH.SetGlobalVar("poll_timeup", id, false);
        CPH.RunAction("Poll", false);
        return true;
    }
}
