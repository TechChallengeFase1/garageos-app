using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

/// <summary>Inclui uma peça do estoque na OS, com o valor do momento</summary>
public class AdicionarPecaNaOSUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IEstoqueClient _estoque;
    private readonly IContextoDaRequisicao _contexto;

    public AdicionarPecaNaOSUseCase(
        IOrdemDeServicoRepository repository,
        IEstoqueClient estoque,
        IContextoDaRequisicao contexto)
    {
        _repository = repository;
        _estoque = estoque;
        _contexto = contexto;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId, AdicionarPecaRequest request)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(ordemDeServicoId)
            ?? throw new OrdemDeServicoNaoEncontradaException();

        var peca = await _estoque.ObterDisponivelAsync(request.EstoqueId, request.Quantidade);

        ordemDeServico.AdicionarPeca(peca.Id, peca.Nome, peca.Valor, request.Quantidade, _contexto.CorrelationId);

        await _repository.AtualizarAsync(ordemDeServico);

        return ordemDeServico.ParaResponse();
    }
}
