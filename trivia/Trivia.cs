// Trivia for Streamer.bot, free from itsCRA1G (itscra1g.github.io/trivia)
// !trivia asks a question and the first right answer in chat wins. Twitch and Kick.
// Works together with the "Trivia Timer" action, which gives the hint and reveals the answer.
using System;
using System.Collections.Generic;
using System.Text;

public class CPHInline
{
    // ===== Change these to suit your stream =====

    private const bool ModsOnly = false;         // true = only mods and you can start a question
    private const int CooldownSeconds = 120;     // wait between questions started with !trivia

    // Your questions: "Question|Answer|Other accepted answers...". Add as many as you like.
    private static readonly string[] Questions = new[]
    {
        "What is the capital of Australia?|Canberra",
        "How many sides does a hexagon have?|6|six",
        "Which planet is known as the Red Planet?|Mars",
        "What is the largest ocean on Earth?|Pacific|Pacific Ocean",
        "In what year did the Titanic sink?|1912",
        "What is the chemical symbol for gold?|Au",
        "How many players does a football (soccer) team have on the pitch?|11|eleven",
        "Which company makes the PlayStation?|Sony",
        "What is the best-selling video game of all time?|Minecraft",
        "What is the name of Mario's brother?|Luigi",
        "What colour is Pac-Man?|Yellow",
        "Which game series stars Master Chief?|Halo",
        "What does GG stand for in gaming?|Good game",
        "How many hearts does an octopus have?|3|three",
        "What is the smallest planet in our solar system?|Mercury",
        "Who painted the Mona Lisa?|Leonardo da Vinci|da Vinci|Leonardo",
        "How many minutes are there in a day?|1440|1,440",
        "What is the tallest mountain in the world?|Everest|Mount Everest",
        "Which band sang Bohemian Rhapsody?|Queen",
        "What is the longest river in Africa?|Nile|River Nile",
        "In which game do you build with blocks and run from Creepers?|Minecraft",
        "Which fruit is guacamole made from?|Avocado|Avocados",
        "How many strings does a standard guitar have?|6|six",
        "What is the freezing point of water in Celsius?|0|zero",
        "Which animal is called the King of the Jungle?|Lion",
        "In Fortnite, what do players jump out of at the start of a match?|Battle Bus|Battlebus|Bus",
        "What colour is Sonic the Hedgehog?|Blue",
        "How many continents are there?|7|seven",
        "Which gas do plants take in from the air?|Carbon dioxide|CO2",
        "What is the name of Call of Duty's free battle royale?|Warzone",
        "What is 12 times 12?|144",
        "Which planet is famous for its rings?|Saturn",
        "What is the largest mammal on Earth?|Blue whale|Whale",
        "How many bones are in the adult human body?|206",
        "In what year did the first iPhone come out?|2007",
        "Which language has the most native speakers?|Mandarin|Mandarin Chinese|Chinese",
        "What is the hardest natural substance?|Diamond|Diamonds",
        "Which Pokemon is number 25 in the Pokedex?|Pikachu",
        "What does the 'www' in a web address stand for?|World Wide Web",
        "How many days are there in a leap year?|366"
    };

    // {user} = winner, {answer}, {wins} = their total, like "3 trivia wins"
    private const string AskLine = "TRIVIA: {question}  (first right answer in chat wins)";
    private const string WinLine = "{user} got it! The answer was {answer}. That's {wins} for {user}.";

    // Post the winner as a Twitch announcement (the highlighted message). Kick doesn't have these, so it gets a normal message.
    // Colour: "primary", "blue", "green", "orange" or "purple". Your bot account needs to be a mod for announcements.
    private const bool AnnounceWinner = true;
    private const string AnnounceColour = "purple";

    // ===== Nothing to change below here =====

    public bool Execute()
    {
        string ev = EventName();
        string plat = Platform(ev);
        string user = Arg("user", Arg("userName", ""));
        string login = Arg("userName", user).ToLower();

        if (args.ContainsKey("command"))
        {
            string cmd = Arg("command", "").ToLower().TrimStart('!');
            if (cmd == "triviawins")
            {
                string who = Arg("input0", "").TrimStart('@').ToLower();
                if (who == "") who = login;
                long w = CPH.GetGlobalVar<long>("triviawins|" + plat + "|" + who, true);
                Reply(plat, (who == login ? "@" + user + " you have " : who + " has ") + w + (w == 1 ? " trivia win." : " trivia wins."));
                return true;
            }
            if (cmd == "triviatop") { Top(plat); return true; }
            if (cmd != "trivia") return true;
            if (ModsOnly && !IsMod()) return true;
            if (CPH.GetGlobalVar<bool>("trivia_open", false)) { Reply(plat, "There's already a question going! " + CPH.GetGlobalVar<string>("trivia_text", false)); return true; }
            long left = CPH.GetGlobalVar<long>("trivia_next", false) - Now();
            if (left > 0 && !IsMod()) { Reply(plat, "@" + user + " next question in " + left + "s."); return true; }
            Ask();
            return true;
        }

        if (ev.Contains("Chat") || ev.Contains("Message"))
        {
            if (!CPH.GetGlobalVar<bool>("trivia_open", false)) return true;
            string msg = Clean(Arg("rawInput", Arg("message", "")));
            if (msg == "") return true;
            bool right = false;
            foreach (string a in (CPH.GetGlobalVar<string>("trivia_answers", false) ?? "").Split('|'))
                if (a != "" && Clean(a) == msg) { right = true; break; }
            if (!right) return true;
            CPH.SetGlobalVar("trivia_open", false, false);           // first right answer only
            string key = "triviawins|" + plat + "|" + login;
            long wins = CPH.GetGlobalVar<long>(key, true) + 1;
            CPH.SetGlobalVar(key, wins, true);
            CPH.SetGlobalVar("triviawinname|" + plat + "|" + login, user, true);
            SayWin(WinLine.Replace("{user}", user).Replace("{answer}", CPH.GetGlobalVar<string>("trivia_answer", false)).Replace("{wins}", wins + (wins == 1 ? " trivia win" : " trivia wins")));
            return true;
        }

        if (!CPH.GetGlobalVar<bool>("trivia_open", false)) Ask();        // e.g. a Timed Action trigger
        return true;
    }

    private void Ask()
    {
        int i = Next();
        string[] p = Questions[i].Split('|');
        string q = p[0].Trim();
        var ans = new List<string>();
        for (int k = 1; k < p.Length; k++) if (p[k].Trim() != "") ans.Add(p[k].Trim());
        if (ans.Count == 0) return;
        string id = Now() + "-" + i;
        CPH.SetGlobalVar("trivia_id", id, false);
        CPH.SetGlobalVar("trivia_answer", ans[0], false);
        CPH.SetGlobalVar("trivia_answers", string.Join("|", ans.ToArray()), false);
        CPH.SetGlobalVar("trivia_text", q, false);
        CPH.SetGlobalVar("trivia_open", true, false);
        CPH.SetGlobalVar("trivia_next", Now() + CooldownSeconds, false);
        Say(AskLine.Replace("{question}", q));
        CPH.RunAction("Trivia Timer", false);                          // hint, then reveal if nobody gets it
    }

    // Pick a question that hasn't been asked yet, until they've all been used.
    private int Next()
    {
        string used = CPH.GetGlobalVar<string>("trivia_used", true) ?? "";
        var done = new Dictionary<string, bool>();
        foreach (string u in used.Split(',')) done[u] = true;
        var free = new List<int>();
        for (int i = 0; i < Questions.Length; i++) if (!done.ContainsKey(i.ToString())) free.Add(i);
        if (free.Count == 0) { used = ""; for (int i = 0; i < Questions.Length; i++) free.Add(i); }
        int pick = free[new Random().Next(free.Count)];
        CPH.SetGlobalVar("trivia_used", (used == "" ? "" : used + ",") + pick, true);
        return pick;
    }

    private void Top(string plat)
    {
        var list = new List<KeyValuePair<string, long>>();
        try
        {
            var mi = CPH.GetType().GetMethod("GetGlobalVarValues");
            object res = mi == null ? null : mi.Invoke(CPH, mi.GetParameters().Length == 1 ? new object[] { true } : new object[0]);
            var items = res as System.Collections.IEnumerable;
            if (items != null)
                foreach (object it in items)
                {
                    string name = Prop(it, "VariableName") ?? Prop(it, "Name") ?? "";
                    if (!name.StartsWith("triviawins|")) continue;
                    long v; long.TryParse(Prop(it, "Value") ?? "0", out v);
                    string shown = CPH.GetGlobalVar<string>("triviawinname|" + name.Substring(11), true) ?? name.Substring(name.LastIndexOf('|') + 1);
                    list.Add(new KeyValuePair<string, long>(shown, v));
                }
        }
        catch (Exception) { }
        if (list.Count == 0) { Reply(plat, "No trivia winners yet. Type !trivia to play!"); return; }
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        var parts = new List<string>();
        for (int i = 0; i < list.Count && i < 5; i++) parts.Add((i + 1) + ". " + list[i].Key + " (" + list[i].Value + ")");
        Reply(plat, "Trivia champions: " + string.Join("  ", parts.ToArray()));
    }

    // ----- helpers -----

    // Lower case, no punctuation, no leading "the"/"a"/"an", so small differences still count.
    private static string Clean(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in (s ?? "").ToLower()) sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        string t = string.Join(" ", sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        foreach (string w in new[] { "the ", "a ", "an " }) if (t.StartsWith(w)) { t = t.Substring(w.Length); break; }
        return t;
    }

    private static string Prop(object o, string name)
    {
        var p = o.GetType().GetProperty(name);
        if (p == null) return null;
        object v = p.GetValue(o, null);
        return v == null ? null : v.ToString();
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

    // The winner: a Twitch announcement if switched on (falls back to a normal message), and a normal message on Kick.
    private void SayWin(string msg)
    {
        bool sent = false;
        if (AnnounceWinner)
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

    // Questions and winners go to both chats, so it works when you stream to Twitch and Kick at once.
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
