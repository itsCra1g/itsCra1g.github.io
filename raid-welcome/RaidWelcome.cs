// Raid Welcome for Streamer.bot, free from itsCRA1G (itscra1g.github.io/raid-welcome)
// Trigger: Twitch > Channel > Raid. Sub-action: Core > C# > Execute C# Code.
using System;

public class CPHInline
{
    // ===== Change these to suit your stream =====

    // Welcome messages. One is picked at random each raid, never the same twice in a row.
    // {user} = the raider's name, {viewers} = how many they brought.
    private static readonly string[] Welcomes = new[]
    {
        "Welcome in, raiders! Huge thanks to {user} for bringing {viewers} people over. Grab a seat and say hi!",
        "RAID! {user} just landed with {viewers} viewers. Make yourselves at home, everyone!",
        "Look who it is! {user} and {viewers} friends have arrived. Thanks so much for the raid!"
    };

    // Shoutout lines, posted a few seconds after the welcome. {link} = the raider's channel.
    private static readonly string[] Shoutouts = new[]
    {
        "Go show {user} some love, they were live just now: {link}",
        "{user} is a top streamer, give them a follow: {link}",
        "Check out {user} over at {link}"
    };

    // Raids smaller than this are ignored. 1 = welcome every raid.
    private const int MinViewers = 1;

    // Seconds to wait between the welcome and the shoutout line.
    private const int ShoutoutDelaySeconds = 3;

    // Also send Twitch's own /shoutout? Your bot account must be a mod for this.
    private const bool NativeShoutout = true;

    // ===== Nothing to change below here =====

    public bool Execute()
    {
        string user = Arg("user", Arg("userName", "someone"));
        string login = Arg("userName", user).ToLower();
        int viewers = 0;
        int.TryParse(Arg("viewers", "0"), out viewers);

        if (viewers < MinViewers)
            return true;

        string welcome = Pick(Welcomes, "raidwelcome_lastWelcome")
            .Replace("{user}", user)
            .Replace("{viewers}", viewers.ToString());
        CPH.SendMessage(welcome);

        CPH.Wait(ShoutoutDelaySeconds * 1000);

        string shout = Pick(Shoutouts, "raidwelcome_lastShout")
            .Replace("{user}", user)
            .Replace("{link}", "https://twitch.tv/" + login);
        CPH.SendMessage(shout);

        if (NativeShoutout)
        {
            try { CPH.TwitchSendShoutoutByLogin(login); }
            catch (Exception e) { CPH.LogInfo("Raid Welcome: Twitch shoutout skipped (" + e.Message + ")"); }
        }
        return true;
    }

    private string Arg(string key, string fallback)
    {
        object v;
        if (args.TryGetValue(key, out v) && v != null && v.ToString().Length > 0)
            return v.ToString();
        return fallback;
    }

    private string Pick(string[] list, string varName)
    {
        if (list.Length == 1) return list[0];
        int last = CPH.GetGlobalVar<int>(varName, false);
        int i;
        do { i = new Random().Next(list.Length); } while (i == last - 1);
        CPH.SetGlobalVar(varName, i + 1, false);
        return list[i];
    }
}
