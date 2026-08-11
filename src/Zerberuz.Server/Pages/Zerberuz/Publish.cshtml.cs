using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Zerberuz.Analyzers.Rules;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server.Pages.Zerberuz;

public sealed class PublishModel : PageModel
{
    private readonly IProfileRuleStore store;

    public PublishModel(IProfileRuleStore store)
    {
        this.store = store;
    }

    [BindProperty]
    public string RuleSetJson { get; set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public bool Succeeded { get; private set; }

    public void OnGet()
    {
        RuleSetJson = """
        {
          "schemaVersion": "1.0",
          "rulesVersion": "2026.10.01",
          "profile": "backend",
          "rules": [],
          "help": []
        }
        """;
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        RuleSetDefinition? ruleSet;
        try
        {
            ruleSet = RuleSetJsonSerializer.Deserialize(RuleSetJson);
        }
        catch (Exception exception)
        {
            Succeeded = false;
            Message = "Invalid JSON: " + exception.Message;
            return;
        }

        var validation = new RuleSetValidator().Validate(ruleSet);
        if (!validation.IsValid)
        {
            Succeeded = false;
            Message = "Validation failed: " + string.Join(" ", validation.Errors.Select(error => $"{error.Code}: {error.Message}"));
            return;
        }

        var result = await store.PublishRuleSetAsync(ruleSet!, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            Succeeded = false;
            Message = result.ErrorMessage;
            return;
        }

        Succeeded = true;
        Message = $"Published {ruleSet!.Profile}@{ruleSet.RulesVersion}.";
    }
}
