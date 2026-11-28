// Sub Goal for Streamer.bot, free from itsCRA1G (itscra1g.github.io/sub-goal)
// Counts Twitch and Kick subs for the Sub Goal overlay, and handles the chat commands.
// v2 (Nov 2026): counts Twitch gift bombs.
using System;
using System.Collections.Generic;

public class CPHInline
{
    // ===== Change these to suit your stream =====

    // The goal to start with. Change it any time in chat with !setgoal 20
    private const int StartingGoal = 10;

    // Chat replies. {count} = subs so far, {goal} = the goal, {left} = subs still needed.
    private const string StatusReply = "Sub goal: {count}/{goal}. {left} to go!";
    private const string GoalReachedReply = "Sub goal: {count}/{goal}. Goal reached, thank you!";
    private const string SetReply = "Sub goal updated: {count}/{goal}.";

    // Post in chat when the goal is reached? Leave empty "" for no message.
    private const string GoalReachedChat = "WE DID IT! The sub goal of {goal} is complete. Thank you all!";

    // ===== Nothing to change below here =====

    private const string VarCount = "subgoal_count";
    private const string VarGoal = "subgoal_goal";
    private const string VarSeen = "subgoal_seen";
    private const string LedgerVar = "subgoal_bombs";

    public bool Execute()
    {
        string mode = Arg("mode", "");
        if (mode == "sync") { Broadcast("sync", "", 0, ""); return true; }
        if (mode == "reset") { SetCount(0); Broadcast("reset", "", 0, ""); return true; }

        if (args.ContainsKey("command")) return Command();

        // Kick mass gift: one event listing every recipient.
        if (args.ContainsKey("recipient.0.userId") || args.ContainsKey("recipient.0.userName"))
        {
            int added = 0;
            for (int i = 0; i < 500; i++)
            {
                string id = Arg("recipient." + i + ".userId", Arg("recipient." + i + ".userName", ""));
                if (id == "") break;
                if (FirstTime("kick:" + id)) added++;
            }
            Add(added, Arg("user", "Someone"), "gift", "kick");
            return true;
        }

        // Kick single gift.
        if (args.ContainsKey("recipient.userId") || args.ContainsKey("recipient.userName"))
        {
            string id = Arg("recipient.userId", Arg("recipient.userName", ""));
            if (id == "" || FirstTime("kick:" + id)) Add(1, Arg("user", "Someone"), "gift", "kick");
            return true;
        }

        // Twitch gift sub.
        if (args.ContainsKey("recipientId") || args.ContainsKey("recipientUserName"))
        {
            string id = Arg("recipientId", Arg("recipientUserName", ""));
            string gifter = Arg("anonymous", "False").ToLower() == "true" ? "Anonymous" : Arg("user", "Someone");
            if (id != "" && !FirstTime("twitch:" + id)) return true;
            if (PartOfBomb(gifter)) return true;
            Add(1, gifter, "gift", "twitch");
            return true;
        }

        // Twitch gift bomb: one event for the whole bomb.
        if (args.ContainsKey("gifts") || args.ContainsKey("bonusGifts"))
        {
            string gifter = Arg("anonymous", "False").ToLower() == "true" ? "Anonymous" : Arg("user", "Someone");
            int gifts; int.TryParse(Arg("gifts", "0"), out gifts);
            Add(BombGifts(gifter, gifts), gifter, "gift", "twitch");
            return true;
        }

        // New sub or resub, on either platform.
        Add(1, Arg("user", "Someone"), "sub", Platform());
        return true;
    }

    private bool Command()
    {
        string cmd = Arg("command", "").ToLower().TrimStart('!');
        int n;
        bool hasNumber = int.TryParse(Arg("input0", ""), out n) && n >= 0;

        if (cmd == "subgoal") { Reply(Fill(Count() >= Goal() ? GoalReachedReply : StatusReply)); return true; }
        if (!IsMod()) return true;

        if (cmd == "setgoal" && hasNumber && n > 0) { CPH.SetGlobalVar(VarGoal, n, true); }
        else if (cmd == "setsubs" && hasNumber) { SetCount(n); }
        else if (cmd == "resetsubs") { SetCount(0); }
        else { Reply("Try !setgoal 20, !setsubs 5 or !resetsubs"); return true; }

        Broadcast("set", "", 0, "");
        Reply(Fill(SetReply));
        return true;
    }

    private void Add(int added, string user, string kind, string platform)
    {
        if (added <= 0) return;
        int before = Count();
        SetCount(before + added);
        Broadcast(kind, user, added, platform);
        if (before < Goal() && Count() >= Goal() && GoalReachedChat != "")
        {
            CPH.SendMessage(Fill(GoalReachedChat));
            try { CPH.SendKickMessage(Fill(GoalReachedChat)); } catch (Exception) { }
        }
    }

    private int Count() { return CPH.GetGlobalVar<int>(VarCount, true); }
    private void SetCount(int n) { CPH.SetGlobalVar(VarCount, n, true); }
    private int Goal()
    {
        int g = CPH.GetGlobalVar<int>(VarGoal, true);
        return g > 0 ? g : StartingGoal;
    }

    private void Broadcast(string kind, string user, int added, string platform)
    {
        string json = "{\"subgoal\":{\"count\":" + Count() + ",\"goal\":" + Goal() +
            ",\"event\":\"" + Esc(kind) + "\",\"user\":\"" + Esc(user) + "\",\"added\":" + added +
            ",\"platform\":\"" + Esc(platform) + "\"}}";
        CPH.WebsocketBroadcastJson(json);
    }

    // Each gifted sub can arrive twice on Kick (single + mass), so count each recipient once a minute.
    private bool FirstTime(string id)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string seen = CPH.GetGlobalVar<string>(VarSeen, false) ?? "";
        var keep = new List<string>();
        bool found = false;
        foreach (string entry in seen.Split(';'))
        {
            int at = entry.LastIndexOf('@');
            if (at < 1) continue;
            long t;
            if (!long.TryParse(entry.Substring(at + 1), out t) || now - t > 60) continue;
            if (entry.Substring(0, at) == id) found = true;
            keep.Add(entry);
        }
        if (!found) keep.Add(id + "@" + now);
        CPH.SetGlobalVar(VarSeen, string.Join(";", keep), false);
        return !found;
    }

    private bool IsMod()
    {
        if (Arg("isModerator", "False").ToLower() == "true") return true;
        if (Arg("isBroadcaster", "False").ToLower() == "true") return true;
        string me = Arg("broadcastUserName", Arg("broadcastUser", "")).ToLower();
        return me != "" && me == Arg("userName", Arg("user", "")).ToLower();
    }

    // Which platform a sub came from: "twitch" or "kick".
    private string Platform()
    {
        string where = (Arg("userType", "") + Arg("broadcastingService", "") + Arg("__source", "") + Arg("triggerName", "")).ToLower();
        return where.Contains("kick") ? "kick" : "twitch";
    }

    private void Reply(string msg)
    {
        string where = (Arg("userType", "") + Arg("commandSource", "") + Arg("broadcastingService", "")).ToLower();
        if (where.Contains("kick")) CPH.SendKickMessage(msg);
        else CPH.SendMessage(msg);
    }

    private string Fill(string s)
    {
        int c = Count(), g = Goal();
        return s.Replace("{count}", c.ToString()).Replace("{goal}", g.ToString()).Replace("{left}", Math.Max(0, g - c).ToString());
    }

    private string Arg(string key, string fallback)
    {
        object v;
        if (args.TryGetValue(key, out v) && v != null && v.ToString().Length > 0) return v.ToString();
        return fallback;
    }

    private static string Esc(string s)
    {
        return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
    }

    // ----- Twitch gift bombs -----
    // Some setups get every gift in a bomb as its own Gift Sub as well, others only get the bomb.
    // Count the bomb once, then skip that many single gifts from the same gifter for the next 2 minutes
    // (and take off any that arrived just before the bomb), so nothing is lost or counted twice.

    private int BombGifts(string gifter, int gifts)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var led = LoadLedger(now);
        long[] e; if (!led.TryGetValue(gifter, out e)) e = new long[] { 0, 0, now };
        int already = (int)e[0];
        int add = Math.Max(0, gifts - already);
        led[gifter] = new long[] { 0, add, now };
        SaveLedger(led);
        return add;
    }

    // True when this single Twitch gift was already counted as part of a bomb.
    private bool PartOfBomb(string gifter)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var led = LoadLedger(now);
        long[] e; if (!led.TryGetValue(gifter, out e)) e = new long[] { 0, 0, now };
        bool covered = e[1] > 0;
        if (covered) e[1]--; else e[0]++;
        e[2] = now;
        led[gifter] = e;
        SaveLedger(led);
        return covered;
    }

    private Dictionary<string, long[]> LoadLedger(long now)
    {
        var d = new Dictionary<string, long[]>(StringComparer.OrdinalIgnoreCase);
        string raw = CPH.GetGlobalVar<string>(LedgerVar, false) ?? "";
        foreach (string line in raw.Split('\n'))
        {
            string[] p = line.Split('\t');
            if (p.Length != 4) continue;
            long a, b, t;
            if (!long.TryParse(p[1], out a) || !long.TryParse(p[2], out b) || !long.TryParse(p[3], out t)) continue;
            if (now - t > 120) continue;
            d[p[0]] = new long[] { a, b, t };
        }
        return d;
    }

    private void SaveLedger(Dictionary<string, long[]> d)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in d) sb.Append(kv.Key.Replace("\t", " ").Replace("\n", " ")).Append('\t').Append(kv.Value[0]).Append('\t').Append(kv.Value[1]).Append('\t').Append(kv.Value[2]).Append('\n');
        CPH.SetGlobalVar(LedgerVar, sb.ToString(), false);
    }
}
