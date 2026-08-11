using Microsoft.AspNetCore.Mvc.RazorPages;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Pages.Zerberuz;

public sealed class VersionModel : PageModel
{
    private readonly IProfileRuleStore store;

    public VersionModel(IProfileRuleStore store)
    {
        this.store = store;
    }

    public RuleSetDefinition? RuleSet { get; private set; }

    public async Task OnGetAsync(string profile, string version, CancellationToken cancellationToken)
    {
        RuleSet = await store.GetRuleSetAsync(profile, version, cancellationToken).ConfigureAwait(false);
    }
}
