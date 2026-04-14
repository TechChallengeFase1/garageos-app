using GarageOS.Application.DTOs.Servicos;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Servicos;

public class ObterServicoUseCase
{
    private readonly IServicoRepository _repository;

    public ObterServicoUseCase(IServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServicoResponse> ExecutarAsync(Guid id)
    {
        var servico = await _repository.ObterPorIdAsync(id)
            ?? throw new ServicoNaoEncontradoException(id);

        return new ServicoResponse
        {
            Id = servico.Id,
            NomeServico = servico.NomeServico,
            Preco = servico.Preco
        };
    }
}
