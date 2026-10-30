using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class AdicionarServicoNaOSUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IContextoDaRequisicao _contexto;

    public AdicionarServicoNaOSUseCase(
        IOrdemDeServicoRepository repository,
        IServicoRepository servicoRepository,
        IContextoDaRequisicao contexto)
    {
        _repository = repository;
        _servicoRepository = servicoRepository;
        _contexto = contexto;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId, AdicionarServicoRequest request)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(ordemDeServicoId)
            ?? throw new OrdemDeServicoNaoEncontradaException();

        var servico = await _servicoRepository.ObterPorIdAsync(request.ServicoId)
            ?? throw new ServicoNaoEncontradoException(request.ServicoId);

        ordemDeServico.AdicionarServico(servico.Id, servico.NomeServico, servico.Preco, _contexto.CorrelationId);

        await _repository.AtualizarAsync(ordemDeServico);

        return ordemDeServico.ParaResponse();
    }
}
