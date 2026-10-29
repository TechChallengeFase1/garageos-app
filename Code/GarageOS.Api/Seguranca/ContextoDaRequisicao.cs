using GarageOS.Api.Middlewares;
using GarageOS.Application.Abstractions;

namespace GarageOS.Api.Seguranca;

/// <summary>Lê da requisição HTTP o que os use cases precisam</summary>
public class ContextoDaRequisicao : IContextoDaRequisicao
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Inicializa com o acesso ao HttpContext atual</summary>
    public ContextoDaRequisicao(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    public string? CorrelationId =>
        _accessor.HttpContext?.Items.TryGetValue(CorrelationIdMiddleware.LogPropertyName, out var id) == true
            ? id as string
            : null;
}
