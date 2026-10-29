using Microsoft.AspNetCore.Http;

namespace GarageOS.Infrastructure.ExternalServices;

/// <summary>Repassa Authorization e X-Correlation-Id da requisição atual às chamadas de saída</summary>
public class PropagarCabecalhosHandler : DelegatingHandler
{
    private static readonly string[] Cabecalhos = ["Authorization", "X-Correlation-Id"];

    private readonly IHttpContextAccessor _accessor;

    public PropagarCabecalhosHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var entrada = _accessor.HttpContext?.Request.Headers;
        if (entrada is not null)
        {
            foreach (var nome in Cabecalhos)
            {
                if (entrada.TryGetValue(nome, out var valor) && !request.Headers.Contains(nome))
                    request.Headers.TryAddWithoutValidation(nome, valor.ToString());
            }

            // O middleware gera um correlation id quando o chamador não manda;
            // esse gerado também precisa seguir adiante.
            if (!request.Headers.Contains("X-Correlation-Id")
                && _accessor.HttpContext!.Items.TryGetValue("CorrelationId", out var gerado) && gerado is string id)
                request.Headers.TryAddWithoutValidation("X-Correlation-Id", id);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
