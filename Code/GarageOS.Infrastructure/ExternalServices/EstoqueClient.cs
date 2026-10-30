using System.Net;
using System.Net.Http.Json;
using GarageOS.Application.Abstractions;
using GarageOS.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace GarageOS.Infrastructure.ExternalServices;

/// <summary>Cliente HTTP do estoque do Execução Service</summary>
/// <remarks>
/// Typed HttpClient com o handler de resiliência padrão (timeout, retry com
/// backoff, circuit breaker), configurado no registro. O token do atendente e
/// o X-Correlation-Id seguem junto pelo PropagarCabecalhosHandler: o Execução
/// autoriza a chamada como se fosse o próprio atendente, e o rastro da
/// requisição continua do outro lado.
/// </remarks>
public class EstoqueClient : IEstoqueClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EstoqueClient> _logger;

    public EstoqueClient(HttpClient http, ILogger<EstoqueClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PecaDoEstoque?> ObterPecaAsync(Guid estoqueId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage resposta;
        try
        {
            resposta = await _http.GetAsync($"api/estoques/{estoqueId}", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException
                                       or Polly.CircuitBreaker.BrokenCircuitException)
        {
            _logger.LogWarning(ex, "Execucao Service indisponivel ao consultar a peca {EstoqueId}", estoqueId);
            throw new ServicoExternoIndisponivelException("Execução Service", ex);
        }

        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!resposta.IsSuccessStatusCode)
        {
            _logger.LogWarning("Execucao Service respondeu {Status} ao consultar a peca {EstoqueId}", (int)resposta.StatusCode, estoqueId);
            throw new ServicoExternoIndisponivelException("Execução Service");
        }

        var peca = await resposta.Content.ReadFromJsonAsync<EstoqueDoExecucao>(cancellationToken)
            ?? throw new ServicoExternoIndisponivelException("Execução Service");

        return new PecaDoEstoque(peca.Id, peca.Nome, peca.Valor, peca.QuantidadeDisponivel);
    }

    /// <summary>Recorte do contrato GET /api/estoques/{id} do Execução Service</summary>
    private sealed class EstoqueDoExecucao
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int QuantidadeDisponivel { get; set; }
    }
}
