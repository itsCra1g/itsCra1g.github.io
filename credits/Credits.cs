// Credits for Streamer.bot, free from itsCRA1G (itscra1g.github.io/credits)
// Collects everyone who took part this stream, and hands the list to the Credits overlay.
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
    private static readonly string[] Lists = { "raids", "subs", "gifts", "tips", "follows", "chat" };

    public bool Execute()
    {
        string mode = Arg("mode", "");
        if (mode == "get") { Send(); return true; }
        if (mode == "reset") { Reset(); return true; }

        string ev = EventName();
        string user = Arg("user", Arg("userName", ""));
        if (ev.Contains("StreamOnline")) { Reset(); return true; }
        if (user == "" || Ignored(user)) return true;

        if (ev.Contains("GiftBomb")) return true;   // each gift also arrives as its own Gift Sub
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
            if (id == "" || FirstTime(plat + id)) Bump("gifts", user, 1);
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
