// Sub Goal reset for Streamer.bot, free from itsCRA1G (itscra1g.github.io/sub-goal)
// Put this on Stream Offline triggers to start the count from 0 each stream.
using System;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SetGlobalVar("subgoal_count", 0, true);
        int goal = CPH.GetGlobalVar<int>("subgoal_goal", true);
        if (goal <= 0) goal = 10;
        CPH.WebsocketBroadcastJson("{\"subgoal\":{\"count\":0,\"goal\":" + goal + ",\"event\":\"reset\",\"user\":\"\",\"added\":0}}");
        return true;
    }
}
