// Trivia Timer for Streamer.bot, free from itsCRA1G (itscra1g.github.io/trivia)
// Started by the Trivia action: gives a hint, then reveals the answer if nobody gets it.
using System;
using System.Text;

public class CPHInline
{
    // ===== Change these to suit your stream =====

    private const int HintAfterSeconds = 15;
    private const int TimeUpAfterSeconds = 30;
    private const string HintLine = "Hint: {hint}";
    private const string TimeUpLine = "Time's up! The answer was {answer}.";

    // ===== Nothing to change below here =====

    public bool Execute()
    {
        string id = CPH.GetGlobalVar<string>("trivia_id", false);
        if (!CPH.GetGlobalVar<bool>("trivia_open", false)) return true;

        CPH.Wait(HintAfterSeconds * 1000);
        if (!StillOpen(id)) return true;
        Say(HintLine.Replace("{hint}", Hint(CPH.GetGlobalVar<string>("trivia_answer", false) ?? "")));

        CPH.Wait(Math.Max(1, TimeUpAfterSeconds - HintAfterSeconds) * 1000);
        if (!StillOpen(id)) return true;
        CPH.SetGlobalVar("trivia_open", false, false);
        Say(TimeUpLine.Replace("{answer}", CPH.GetGlobalVar<string>("trivia_answer", false) ?? ""));
        return true;
    }

    private bool StillOpen(string id)
    {
        return CPH.GetGlobalVar<bool>("trivia_open", false) && CPH.GetGlobalVar<string>("trivia_id", false) == id;
    }

    // "Canberra" -> "C _ _ _ _ _ _ _", keeping spaces and numbers' first digit.
    private static string Hint(string answer)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (char c in answer)
        {
            if (c == ' ') { sb.Append("   "); first = true; continue; }
            if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
            sb.Append(first || !char.IsLetterOrDigit(c) ? c : '_');
            first = false;
        }
        return sb.ToString();
    }

    private void Say(string msg) { CPH.SendMessage(msg); try { CPH.SendKickMessage(msg); } catch (Exception) { } }
}
