using System;
using System.IO;
using System.Globalization;

// Alerts Sound: plays a sound file from your PC when the itsCRA1G Alerts overlay shows an alert.
// The overlay runs this action with alertType, soundFolder and soundVolume. No triggers needed.
// Name your files after the alert: follow, sub, resub, gift, bomb, cheer, kicks, raid (.mp3 or .wav).
// all.mp3 (or all.wav) plays for any alert without its own file.
public class CPHInline
{
    public bool Execute()
    {
        string type = Arg("alertType").Trim().ToLowerInvariant();
        string folder = Arg("soundFolder").Trim().Trim('"');
        double vol;
        if (!double.TryParse(Arg("soundVolume"), NumberStyles.Float, CultureInfo.InvariantCulture, out vol)) vol = 1;
        if (vol <= 0) return true;
        if (vol > 1) vol = 1;

        foreach (char c in type) if (!char.IsLetter(c)) return true;   // plain names only
        if (folder == "" || !Directory.Exists(folder))
        {
            CPH.LogWarn("Alerts Sound: can't find the folder \"" + folder + "\"");
            return true;
        }

        string file = Find(folder, type);
        if (file == null) file = Find(folder, "all");
        if (file == null) return true;

        CPH.PlaySound(file, (float)vol, false);
        return true;
    }

    string Find(string folder, string name)
    {
        if (name == "") return null;
        string[] exts = { ".mp3", ".wav" };
        foreach (string ext in exts)
        {
            string path = Path.Combine(folder, name + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    string Arg(string key)
    {
        object v;
        if (args != null && args.TryGetValue(key, out v) && v != null) return v.ToString();
        return "";
    }
}
