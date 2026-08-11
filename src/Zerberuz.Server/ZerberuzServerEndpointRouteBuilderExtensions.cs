using Microsoft.AspNetCore.Routing;
using Zerberuz.Server.Profiles;

namespace Zerberuz.Server;

public static class ZerberuzServerEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapZerberuzServer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRuleProfileEndpoints();
        endpoints.MapDiagnosticHelpEndpoints();
        endpoints.MapRuleValidationEndpoints();
        return endpoints;
    }
}
