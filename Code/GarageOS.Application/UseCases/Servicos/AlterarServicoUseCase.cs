using GarageOS.Application.DTOs.Servicos;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Servicos;

public class AlterarServicoUseCase
{
    private readonly IServicoRepository _repository;

    public AlterarServicoUseCase(IServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServicoResponse> ExecutarAsync(Guid id, AtualizarServicoRequest request)
    {
        var servico = await _repository.ObterPorIdAsync(id)
            ?? throw new ServicoNaoEncontradoException(id);

        servico.Atualizar(request.NomeServico, request.Preco);

        await _repository.AtualizarAsync(servico);

        return new ServicoResponse
        {
            Id = servico.Id,
            NomeServico = servico.NomeServico,
            Preco = servico.Preco
        };
    }
}
