using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class AcompanharOrdemDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;

    public AcompanharOrdemDeServicoUseCase(IOrdemDeServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<AcompanhamentoOSResponse> ExecutarAsync(string numeroOS)
    {
        var ordemDeServico = await _repository.ObterPorNumeroOSAsync(numeroOS);
        if (ordemDeServico == null)
            throw new OrdemDeServicoNaoEncontradaException("Ordem de Serviço não encontrada.");

        return new AcompanhamentoOSResponse
        {
            NumeroOS = ordemDeServico.NumeroOS,
            Status = ordemDeServico.Status.ToString(),
            Servicos = ordemDeServico.Servicos
                .Select(s => new AcompanhamentoServicoResponse
                {
                    NomeServico = s.Servico?.NomeServico ?? string.Empty,
                    StatusExecucao = s.Status.ToString()
                })
                .ToList()
        };
    }
}
