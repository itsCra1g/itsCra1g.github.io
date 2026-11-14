// Poll for Streamer.bot, free from itsCRA1G (itscra1g.github.io/poll)
// One poll for Twitch and Kick chat together. Viewers vote by typing the option's number.
// Works with the "Poll Timer" action (closes the poll when time's up) and the Poll overlay.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class CPHInline
{
    // ===== Change these to suit your stream =====

    private const int DefaultSeconds = 60;        // how long a poll runs if you don't give a time
    private const int MaxOptions = 6;             // most options a poll can have
    private const bool ModsOnly = true;           // true = only mods and you can start, end or cancel polls
    private const bool AllowChangeVote = true;    // true = viewers can change their vote by typing another number
    private const bool VoteByName = true;         // true = typing an option's name exactly also counts as a vote

    // {question}, {options} (like "1 Rebirth  2 Verdansk"), {seconds}
    private const string StartLine = "POLL: {question}  Type the number to vote: {options}  ({seconds}s to vote)";
    // {winner}, {votes}, {percent}, {total}
    private const string WinLine = "Poll closed: {winner} wins with {votes} of {total} votes ({percent}%).";
    private const string TieLine = "Poll closed: it's a tie between {winner} with {votes} votes each.";
    private const string NoVotesLine = "Poll closed with no votes.";
    private const string CancelLine = "Poll cancelled.";

    // Post the result as a Twitch announcement (the highlighted message). Kick gets a normal message.
    // Colour: "primary", "blue", "green", "orange" or "purple". Your bot account needs to be a mod for announcements.
    private const bool AnnounceResult = true;
    private const string AnnounceColour = "purple";

    // ===== Nothing to change below here =====

    private const char SEP = '\u001F';
    private static readonly object Gate = new object();   // chat messages can arrive at the same moment; count them one at a time

    public bool Execute()
    {
        lock (Gate) { return Run(); }
    }

    private bool Run()
    {
        string mode = Arg("mode", "").ToLower();
        if (mode == "sync") { Broadcast(); return true; }                 // the overlay asking what's happening

        string id = Get("poll_id");
        if (Get("poll_timeup") != "" && Get("poll_timeup") == id)        // the Poll Timer says time's up
        {
            CPH.SetGlobalVar("poll_timeup", "", false);
            if (IsOpen()) Close();
            return true;
        }

        string ev = EventName();
        string plat = Platform(ev);
        string user = Arg("user", Arg("userName", ""));
        string login = Arg("userName", user).ToLower();

        if (args.ContainsKey("command"))
        {
            string cmd = Arg("command", "").ToLower().TrimStart('!');
            string raw = Arg("rawInput", "").Trim();
            if (cmd == "vote") { if (IsOpen()) Vote(plat, login, raw); return true; }
            if (cmd == "pollresults") { Results(plat); return true; }
            if (ModsOnly && !IsMod()) return true;
            if (cmd == "poll") { Start(plat, raw); return true; }
            if (cmd == "endpoll") { if (IsOpen()) Close(); else Reply(plat, "There's no poll running."); return true; }
            if (cmd == "cancelpoll")
            {
                if (!IsOpen()) { Reply(plat, "There's no poll running."); return true; }
                CPH.SetGlobalVar("poll_state", "cancelled", false);
                CPH.SetGlobalVar("poll_closed_at", Now(), false);
                Say(CancelLine);
                Broadcast();
                return true;
            }
            return true;
        }

        if ((ev.Contains("Chat") || ev.Contains("Message")) && IsOpen())
            Vote(plat, login, Arg("rawInput", Arg("message", "")).Trim());
        return true;
    }

    // "!poll 90 Best map? | Rebirth | Verdansk" (the time is optional: 90, 90s, 2m)
    private void Start(string plat, string raw)
    {
        if (IsOpen()) { Reply(plat, "There's already a poll running. Type !endpoll to close it first."); return; }
        int seconds = DefaultSeconds;
        string first = raw.Split(' ')[0].ToLower();
        int t;
        if (first.Length > 0)
        {
            string n = first.TrimEnd('s', 'm');
            if (int.TryParse(n, out t) && t > 0)
            {
                seconds = first.EndsWith("m") ? t * 60 : t;
                raw = raw.Substring(first.Length).Trim();
            }
        }
        seconds = Math.Max(10, Math.Min(seconds, 3600));
        string[] parts = raw.Split('|');
        var opts = new List<string>();
        for (int i = 1; i < parts.Length && opts.Count < MaxOptions; i++) if (parts[i].Trim() != "") opts.Add(parts[i].Trim());
        string question = parts[0].Trim();
        if (question == "" || opts.Count < 2)
        {
            Reply(plat, "To start a poll: !poll Question? | Option 1 | Option 2  (add a time first if you like: !poll 2m Question? | A | B)");
            return;
        }
        string id = Now() + "-" + new Random().Next(1000, 9999);
        CPH.SetGlobalVar("poll_id", id, false);
        CPH.SetGlobalVar("poll_question", question, false);
        CPH.SetGlobalVar("poll_options", string.Join(SEP.ToString(), opts.ToArray()), false);
        CPH.SetGlobalVar("poll_votes", "", false);
        CPH.SetGlobalVar("poll_ends", Now() + seconds, false);
        CPH.SetGlobalVar("poll_state", "open", false);
        CPH.SetGlobalVar("poll_timeup", "", false);
        var list = new List<string>();
        for (int i = 0; i < opts.Count; i++) list.Add((i + 1) + " " + opts[i]);
        Say(StartLine.Replace("{question}", question).Replace("{options}", string.Join("  ", list.ToArray())).Replace("{seconds}", seconds.ToString()));
        Broadcast();
        CPH.RunAction("Poll Timer", false);
    }

    private void Vote(string plat, string login, string text)
    {
        if (login == "" || text == "") return;
        string[] opts = Options();
        int pick = -1, n;
        string t = text.Trim().TrimStart('#');
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) { if (n >= 1 && n <= opts.Length) pick = n - 1; }
        else if (VoteByName)
        {
            string c = Clean(t);
            for (int i = 0; i < opts.Length; i++) if (c != "" && Clean(opts[i]) == c) { pick = i; break; }
        }
        if (pick < 0) return;
        var votes = Votes();
        string key = plat + "|" + login;
        if (votes.ContainsKey(key) && (!AllowChangeVote || votes[key] == pick)) return;
        votes[key] = pick;
        var sb = new StringBuilder();
        foreach (var kv in votes) sb.Append(kv.Key).Append('=').Append(kv.Value).Append('\n');
        CPH.SetGlobalVar("poll_votes", sb.ToString(), false);
        Broadcast();
    }

    private void Close()
    {
        CPH.SetGlobalVar("poll_state", "closed", false);
        CPH.SetGlobalVar("poll_closed_at", Now(), false);
        string line = ResultLine();
        CPH.SetGlobalVar("poll_last", line, false);
        Announce(line);
        Broadcast();
    }

    private string ResultLine()
    {
        string[] opts = Options();
        int[] tw, kk;
        int total = Count(opts.Length, out tw, out kk);
        if (total == 0) return NoVotesLine;
        int best = 0;
        for (int i = 0; i < opts.Length; i++) best = Math.Max(best, tw[i] + kk[i]);
        var win = new List<string>();
        for (int i = 0; i < opts.Length; i++) if (tw[i] + kk[i] == best) win.Add(opts[i]);
        int pct = (int)Math.Round(best * 100.0 / total);
        string tpl = win.Count > 1 ? TieLine : WinLine;
        string line = tpl.Replace("{winner}", string.Join(" and ", win.ToArray())).Replace("{votes}", best.ToString())
                  .Replace("{percent}", pct.ToString()).Replace("{total}", total.ToString());
        return line.Replace(" 1 votes", " 1 vote");
    }

    private void Results(string plat)
    {
        if (!IsOpen())
        {
            string last = Get("poll_last");
            Reply(plat, last != "" ? last : "There's no poll running.");
            return;
        }
        string[] opts = Options();
        int[] tw, kk;
        int total = Count(opts.Length, out tw, out kk);
        var list = new List<string>();
        for (int i = 0; i < opts.Length; i++) list.Add((i + 1) + " " + opts[i] + ": " + (tw[i] + kk[i]));
        long left = Math.Max(0, CPH.GetGlobalVar<long>("poll_ends", false) - Now());
        Reply(plat, Get("poll_question") + "  " + string.Join("  ", list.ToArray()) + "  (" + total + (total == 1 ? " vote, " : " votes, ") + left + "s left)");
    }

    // Tell the overlay everything it needs to draw the poll.
    private void Broadcast()
    {
        string state = Get("poll_state");
        if (state == "") state = "none";
        string[] opts = Options();
        int[] tw, kk;
        int total = Count(opts.Length, out tw, out kk);
        var sb = new StringBuilder();
        sb.Append("{\"poll\":{\"id\":\"").Append(Esc(Get("poll_id"))).Append("\",\"state\":\"").Append(state)
          .Append("\",\"question\":\"").Append(Esc(Get("poll_question"))).Append("\",\"ends\":").Append(CPH.GetGlobalVar<long>("poll_ends", false))
          .Append(",\"closedAt\":").Append(CPH.GetGlobalVar<long>("poll_closed_at", false))
          .Append(",\"now\":").Append(Now()).Append(",\"total\":").Append(total).Append(",\"options\":[");
        for (int i = 0; i < opts.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append("{\"text\":\"").Append(Esc(opts[i])).Append("\",\"twitch\":").Append(tw[i]).Append(",\"kick\":").Append(kk[i]).Append('}');
        }
        sb.Append("]}}");
        CPH.WebsocketBroadcastJson(sb.ToString());
    }

    // ----- helpers -----

    private bool IsOpen() { return Get("poll_state") == "open"; }
    private string Get(string k) { return CPH.GetGlobalVar<string>(k, false) ?? ""; }

    private string[] Options()
    {
        string o = Get("poll_options");
        return o == "" ? new string[0] : o.Split(SEP);
    }

    private Dictionary<string, int> Votes()
    {
        var d = new Dictionary<string, int>();
        foreach (string line in Get("poll_votes").Split('\n'))
        {
            int eq = line.LastIndexOf('=');
            int v;
            if (eq > 0 && int.TryParse(line.Substring(eq + 1), out v)) d[line.Substring(0, eq)] = v;
        }
        return d;
    }

    private int Count(int n, out int[] tw, out int[] kk)
    {
        tw = new int[n]; kk = new int[n];
        int total = 0;
        foreach (var kv in Votes())
        {
            if (kv.Value < 0 || kv.Value >= n) continue;
            if (kv.Key.StartsWith("kick|")) kk[kv.Value]++; else tw[kv.Value]++;
            total++;
        }
        return total;
    }

    // Lower case, letters and numbers only, so "Verdansk!" matches "verdansk".
    private static string Clean(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in (s ?? "").ToLower()) if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    private static string Esc(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s ?? "")
        {
            if (c == '"' || c == '\\') sb.Append('\\').Append(c);
            else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private string EventName()
    {
        try
        {
            var mi = CPH.GetType().GetMethod("GetEventType", Type.EmptyTypes);
            if (mi != null) { object r = mi.Invoke(CPH, null); string s = r == null ? "" : r.ToString(); if (s != "" && s != "None" && s != "Unknown") return s; }
        }
        catch (Exception) { }
        return Arg("__source", Arg("triggerName", "")).Replace(" ", "");
    }

    private string Platform(string ev)
    {
        string t = (Arg("userType", "") + Arg("commandSource", "") + Arg("broadcastingService", "") + ev).ToLower();
        return t.Contains("kick") ? "kick" : "twitch";
    }

    // The result: a Twitch announcement if switched on (falls back to a normal message), and a normal message on Kick.
    private void Announce(string msg)
    {
        bool sent = false;
        if (AnnounceResult)
        {
            try
            {
                foreach (var m in CPH.GetType().GetMethods())
                {
                    if (m.Name != "TwitchAnnounce") continue;
                    var ps = m.GetParameters();
                    var call = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (i == 0) call[i] = msg;
                        else if (ps[i].ParameterType == typeof(string)) call[i] = AnnounceColour;
                        else if (ps[i].ParameterType == typeof(bool)) call[i] = ps[i].Name.ToLower().Contains("fallback") || ps[i].Name.ToLower().Contains("bot");
                        else call[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : null;
                    }
                    m.Invoke(CPH, call);
                    sent = true;
                    break;
                }
            }
            catch (Exception) { sent = false; }
        }
        if (!sent) CPH.SendMessage(msg);
        try { CPH.SendKickMessage(msg); } catch (Exception) { }
    }

    private void Say(string msg) { CPH.SendMessage(msg); try { CPH.SendKickMessage(msg); } catch (Exception) { } }
    private void Reply(string plat, string msg) { if (plat == "kick") CPH.SendKickMessage(msg); else CPH.SendMessage(msg); }

    private bool IsMod()
    {
        if (Arg("isModerator", "False").ToLower() == "true" || Arg("isBroadcaster", "False").ToLower() == "true") return true;
        string me = Arg("broadcastUserName", Arg("broadcastUser", "")).ToLower();
        return me != "" && me == Arg("userName", Arg("user", "")).ToLower();
    }

    private static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }

    private string Arg(string key, string fallback)
    {
        object v;
        if (args.TryGetValue(key, out v) && v != null && v.ToString().Length > 0) return v.ToString();
        return fallback;
    }
}
