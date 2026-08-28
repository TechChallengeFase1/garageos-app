using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class AlterarStatusOrdemDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IMetricasDeNegocio _metricas;

    public AlterarStatusOrdemDeServicoUseCase(
        IOrdemDeServicoRepository repository,
        IMetricasDeNegocio metricas)
    {
        _repository = repository;
        _metricas = metricas;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId, AlterarStatusRequest request)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(ordemDeServicoId);
        if (ordemDeServico == null)
            throw new OrdemDeServicoNaoEncontradaException();

        // Capturado ANTES da mudanca: depois do AlterarStatus o estado
        // anterior nao existe mais em lugar nenhum.
        var statusAnterior = ordemDeServico.Status.ToString();
        var minutosNoStatusAnterior =
            (DateTime.UtcNow - ordemDeServico.AtualizadoEm.ToUniversalTime()).TotalMinutes;

        try
        {
            ordemDeServico.AlterarStatus(request.Status);
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

        return MapearParaResponse(ordemDeServico);
    }

    private static OrdemDeServicoResponse MapearParaResponse(OrdemDeServico ordemDeServico)
    {
        return new OrdemDeServicoResponse
        {
            Id = ordemDeServico.Id,
            NumeroOS = ordemDeServico.NumeroOS,
            Status = ordemDeServico.Status,
            CriadoEm = ordemDeServico.CriadoEm,
            FinalizadaEm = ordemDeServico.FinalizadaEm,
            AtualizadoEm = ordemDeServico.AtualizadoEm,
            ClienteId = ordemDeServico.ClienteId,
            VeiculoId = ordemDeServico.VeiculoId,
            Servicos = ordemDeServico.Servicos
                .Select(s => new ServicoItemResponse
                {
                    Id = s.Id,
                    ServicoId = s.ServicoId,
                    ServicoNome = s.Servico?.NomeServico ?? string.Empty,
                    Status = s.Status,
                    CriadoEm = s.CriadoEm,
                    IniciadaEm = s.IniciadaEm,
                    FinalizadaEm = s.FinalizadaEm
                })
                .ToList(),
            Estoques = ordemDeServico.Estoques
                .Select(e => new EstoqueItemResponse
                {
                    Id = e.Id,
                    EstoqueId = e.EstoqueId,
                    EstoqueNome = e.Estoque?.Nome ?? string.Empty,
                    Quantidade = e.Quantidade
                })
                .ToList(),
            Orcamento = ordemDeServico.Orcamento != null
                ? new OrcamentoResponse
                {
                    Id = ordemDeServico.Orcamento.Id,
                    Status = ordemDeServico.Orcamento.Status,
                    Preco = ordemDeServico.Orcamento.Preco,
                    CriadoEm = ordemDeServico.Orcamento.CriadoEm,
                    AtualizadoEm = ordemDeServico.Orcamento.AtualizadoEm
                }
                : null
        };
    }
}
