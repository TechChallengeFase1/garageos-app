using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

/// <summary>Mudança de status feita pela equipe</summary>
/// <remarks>
/// Segue a mesma tabela de transições que a saga vai usar na etapa 5. Até lá,
/// é por aqui que a OS avança; depois, a operação manual fica para exceções
/// (cancelar, entregar).
/// </remarks>
public class AlterarStatusOrdemDeServicoUseCase
{
    /// <summary>Origem registrada no histórico para mudanças feitas pela API</summary>
    public const string OrigemManual = "Manual";

    private readonly IOrdemDeServicoRepository _repository;
    private readonly IMetricasDeNegocio _metricas;
    private readonly IContextoDaRequisicao _contexto;

    public AlterarStatusOrdemDeServicoUseCase(
        IOrdemDeServicoRepository repository,
        IMetricasDeNegocio metricas,
        IContextoDaRequisicao contexto)
    {
        _repository = repository;
        _metricas = metricas;
        _contexto = contexto;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId, AlterarStatusRequest request)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(ordemDeServicoId)
            ?? throw new OrdemDeServicoNaoEncontradaException();

        // Capturado ANTES da mudanca: depois do AlterarStatus o estado
        // anterior nao existe mais em lugar nenhum.
        var statusAnterior = ordemDeServico.Status.ToString();
        var minutosNoStatusAnterior =
            (DateTime.UtcNow - ordemDeServico.AtualizadoEm.ToUniversalTime()).TotalMinutes;

        try
        {
            ordemDeServico.AlterarStatus(request.Status, OrigemManual, _contexto.CorrelationId);
            await _repository.AtualizarAsync(ordemDeServico);
        }
        catch (Exception ex)
        {
            // Falha no processamento: tanto violacao de regra (transicao de
            // status invalida) quanto erro de infraestrutura. E o sinal que
            // alimenta o alerta - por isso e emitido antes de propagar.
            _metricas.FalhaNoProcessamento(ordemDeServico.NumeroOS, ex.GetType().Name);
            throw;
        }

        // Este evento e a unica origem do dashboard "tempo medio por status".
        // Nenhuma ferramenta deduz isso de latencia de requisicao.
        _metricas.StatusAlterado(
            ordemDeServico.NumeroOS,
            statusAnterior,
            ordemDeServico.Status.ToString(),
            minutosNoStatusAnterior);

        return ordemDeServico.ParaResponse();
    }
}
