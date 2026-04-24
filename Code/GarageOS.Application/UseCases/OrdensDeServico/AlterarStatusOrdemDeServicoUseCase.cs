using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class AlterarStatusOrdemDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;

    public AlterarStatusOrdemDeServicoUseCase(IOrdemDeServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId, AlterarStatusRequest request)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(ordemDeServicoId);
        if (ordemDeServico == null)
            throw new OrdemDeServicoNaoEncontradaException();

        ordemDeServico.AlterarStatus(request.Status);
        await _repository.AtualizarAsync(ordemDeServico);

        return MapearParaResponse(ordemDeServico);
    }

    private OrdemDeServicoResponse MapearParaResponse(OrdemDeServico ordemDeServico)
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
