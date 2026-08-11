namespace Zerberuz.Server.Contracts;

public sealed class ProfileVersionsResponse
{
    public string Profile { get; set; } = string.Empty;

    public IList<string> Versions { get; set; } = new List<string>();
}
