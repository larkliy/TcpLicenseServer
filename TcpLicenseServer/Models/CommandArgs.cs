namespace TcpLicenseServer.Models;

public class CommandArgs(string[] args)
{
    private int _index = 0;

    public bool HasNext() => _index < args.Length;

    public string PopString()
    {
        if (!HasNext()) throw new ArgumentException("Too few arguments.");
        return args[_index++];
    }

    public int PopInt()
    {
        string val = PopString();
        if (!int.TryParse(val, out var result))
            throw new ArgumentException($"Invalid number format: '{val}'");
        return result;
    }

    public DateTime PopDate()
    {
        string val = PopString();
        if (!DateTime.TryParse(val, out var result))
            throw new ArgumentException($"Invalid date format: '{val}'");
        return result;
    }

    public string RemainingText
    {
        get
        {
            if (!HasNext()) throw new ArgumentException("Missing content.");
            return string.Join(" ", args.Skip(_index));
        }
    }

    public void EnsureCount(int count)
    {
        if (args.Length < count) throw new ArgumentException("Too few arguments");
    }
}