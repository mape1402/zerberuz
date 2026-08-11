using Microsoft.AspNetCore.Mvc.RazorPages;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Pages.Zerberuz;

public sealed class IndexModel : PageModel
{
    private readonly IProfileRuleStore store;

    public IndexModel(IProfileRuleStore store)
    {
        this.store = store;
    }

    public IList<ProfileSummary> Profiles { get; } = new List<ProfileSummary>();

    public int ProfileCount => Profiles.Count;

    public int VersionCount => Profiles.Sum(profile => profile.Versions.Count);

    public int LatestVersionCount => Profiles.Count(profile => profile.Versions.Count > 0);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var profiles = await store.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var profile in profiles)
        {
            var versions = await store.GetVersionsAsync(profile, cancellationToken).ConfigureAwait(false);
            Profiles.Add(new ProfileSummary(profile, versions.ToArray()));
        }
    }

    public sealed record ProfileSummary(string Name, IReadOnlyCollection<string> Versions);
}
