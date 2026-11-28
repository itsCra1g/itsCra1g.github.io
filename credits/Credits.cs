// Credits for Streamer.bot, free from itsCRA1G (itscra1g.github.io/credits)
// Collects everyone who took part this stream, and hands the list to the Credits overlay.
// v2 (Nov 2026): counts Twitch gift bombs, and keeps the list when the stream drops and comes back.
using System;
using System.Collections.Generic;
using System.Text;

public class CPHInline
{
    // ===== Change these to suit your stream =====

    // Accounts to leave out of the credits (bots, your own accounts). Lower case.
    private static readonly string[] Ignore = new[]
    {
        "streamelements", "streamlabs", "nightbot", "moobot", "fossabot", "wizebot",
        "soundalerts", "sery_bot", "botrixoficial", "kicbot", "streamerbot", "commanderroot"
    };

    // ===== Nothing to change below here =====

    private const string Var = "credits_";
    private const string LedgerVar = "credits_bombs";
    private static readonly string[] Lists = { "raids", "subs", "gifts", "tips", "follows", "chat" };

    public bool Execute()
    {
        string mode = Arg("mode", "");
        if (mode == "get") { Send(); return true; }
        if (mode == "reset") { Reset(); return true; }

        string ev = EventName();
        string user = Arg("user", Arg("userName", ""));
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (ev.Contains("StreamOffline")) { CPH.SetGlobalVar(Var + "offAt", now, true); return true; }
        if (ev.Contains("StreamOnline")) { if (NewStream(now)) Reset(); return true; }
        if (user == "" || Ignored(user)) return true;

        if (ev.Contains("GiftBomb"))
        {
            if (Arg("anonymous", "False").ToLower() == "true") user = "Anonymous";
            Bump("gifts", user, BombGifts(user, Num("gifts", Num("count"))));
            return true;
        }
        if (ev.Contains("MassGift"))
        {
            int added = 0;
            for (int i = 0; i < 500; i++)
            {
                string id = Arg("recipient." + i + ".userId", Arg("recipient." + i + ".userName", ""));
                if (id == "") break;
                if (FirstTime("kick:" + id)) added++;
            }
            if (added == 0) int.TryParse(Arg("count", Arg("gifts", "0")), out added);
            Bump("gifts", user, added);
            return true;
        }
        if (ev.Contains("GiftSub"))
        {
            string id = Arg("recipient.userId", Arg("recipientId", Arg("recipientUserName", "")));
            string plat = ev.Contains("Kick") ? "kick:" : "twitch:";
            if (Arg("anonymous", "False").ToLower() == "true") user = "Anonymous";
            if (id != "" && !FirstTime(plat + id)) return true;
            if (plat == "twitch:" && PartOfBomb(user)) return true;
            Bump("gifts", user, 1);
            return true;
        }
        if (ev.Contains("ReSub") || ev.Contains("Resub") || ev.Contains("Sub")) { Bump("subs", user, 1); return true; }
        if (ev.Contains("Cheer")) { Bump("tips", user, Num("bits")); return true; }
        if (ev.Contains("Kicks")) { Bump("tips", user, Num("kicks", Num("amount", Num("quantity")))); return true; }
        if (ev.Contains("Raid")) { Bump("raids", user, Math.Max(1, Num("viewers"))); return true; }
        if (ev.Contains("Follow")) { Bump("follows", user, 1); return true; }
        if (ev.Contains("Chat") || ev.Contains("Message")) { Bump("chat", user, 1); return true; }
        return true;
    }

    // Which trigger ran this action, e.g. "TwitchChatMessage" or "KickFollow".
    private string EventName()
    {
        foreach (string m in new[] { "GetEventType", "GetSource" })
        {
            try
            {
                var mi = CPH.GetType().GetMethod(m, Type.EmptyTypes);
                if (mi == null) continue;
                object r = mi.Invoke(CPH, null);
                string s = r == null ? "" : r.ToString();
                if (s != "" && s != "None" && s != "Unknown" && m == "GetEventType") return s;
            }
            catch (Exception) { }
        }
        return Arg("__source", Arg("triggerName", "")).Replace(" ", "");
    }

    // A stream that drops and comes back keeps its credits. Only clear the list when you've been
    // offline for more than 10 minutes (or, without Stream Offline triggers, 3 hours since the last clear).
    private bool NewStream(long now)
    {
        long off = CPH.GetGlobalVar<long>(Var + "offAt", true);
        long last = CPH.GetGlobalVar<long>(Var + "resetAt", true);
        bool fresh = off > 0 ? now - off > 10 * 60 : now - last > 3 * 3600;
        CPH.SetGlobalVar(Var + "offAt", 0L, true);
        if (fresh) CPH.SetGlobalVar(Var + "resetAt", now, true);
        return fresh;
    }

    // ----- the lists, stored as "name<TAB>number" lines -----

    private Dictionary<string, int> Load(string list)
    {
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string raw = CPH.GetGlobalVar<string>(Var + list, true) ?? "";
        foreach (string line in raw.Split('\n'))
        {
            int t = line.LastIndexOf('\t');
            if (t < 1) continue;
            int n; int.TryParse(line.Substring(t + 1), out n);
            d[line.Substring(0, t)] = n;
        }
        return d;
    }

    private void Save(string list, Dictionary<string, int> d)
    {
        var sb = new StringBuilder();
        foreach (var kv in d) sb.Append(kv.Key.Replace("\t", " ").Replace("\n", " ")).Append('\t').Append(kv.Value).Append('\n');
        CPH.SetGlobalVar(Var + list, sb.ToString(), true);
    }

    private void Bump(string list, string user, int by)
    {
        if (by <= 0) return;
        var d = Load(list);
        int n; d.TryGetValue(user, out n);
        d[user] = n + by;
        Save(list, d);
    }

    private void Reset()
    {
        foreach (string l in Lists) CPH.SetGlobalVar(Var + l, "", true);
        CPH.SetGlobalVar(Var + "seen", "", false);
    }

    private void Send()
    {
        var sb = new StringBuilder("{\"credits\":{");
        sb.Append("\"streamer\":").Append(J(Arg("broadcastUser", Arg("broadcastUserName", ""))));
        foreach (string l in Lists)
        {
            sb.Append(",\"").Append(l).Append("\":[");
            bool first = true;
            foreach (var kv in Load(l))
            {
                if (!first) sb.Append(',');
                sb.Append('[').Append(J(kv.Key)).Append(',').Append(kv.Value).Append(']');
                first = false;
            }
            sb.Append(']');
        }
        sb.Append("}}");
        CPH.WebsocketBroadcastJson(sb.ToString());
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

    // ----- helpers -----

    private bool Ignored(string user)
    {
        string u = user.ToLower();
        foreach (string s in Ignore) if (u == s) return true;
        string me = Arg("broadcastUserName", Arg("broadcastUser", "")).ToLower();
        return me != "" && u == me;
    }

    // Kick can report a gifted sub twice (single and mass), so count each recipient once a minute.
    private bool FirstTime(string id)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string seen = CPH.GetGlobalVar<string>(Var + "seen", false) ?? "";
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
        CPH.SetGlobalVar(Var + "seen", string.Join(";", keep), false);
        return !found;
    }

    private int Num(string key, int fallback = 0)
    {
        int n;
        return int.TryParse(Arg(key, ""), out n) ? n : fallback;
    }

    private string Arg(string key, string fallback)
    {
        object v;
        if (args.TryGetValue(key, out v) && v != null && v.ToString().Length > 0) return v.ToString();
        return fallback;
    }

    private static string J(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s ?? "")
        {
            if (c == '"') sb.Append("\\\"");
            else if (c == '\\') sb.Append("\\\\");
            else if (c < 32) sb.Append(' ');
            else sb.Append(c);
        }
        return sb.Append("\"").ToString();
    }
}
