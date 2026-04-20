using GarageOS.Application.DTOs.Servicos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Servicos;

public class CadastrarServicoUseCase
{
    private readonly IServicoRepository _repository;

    public CadastrarServicoUseCase(IServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServicoResponse> ExecutarAsync(CriarServicoRequest request)
    {
        var servico = new Servico(request.NomeServico, request.Preco);

        await _repository.AdicionarAsync(servico);

        return new ServicoResponse
        {
            Id = servico.Id,
            NomeServico = servico.NomeServico,
            Preco = servico.Preco
        };
    }
}
