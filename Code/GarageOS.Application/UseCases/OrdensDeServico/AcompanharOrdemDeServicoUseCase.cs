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
        var ordemDeServico = await _repository.ObterPorNumeroOSAsync(numeroOS)
            ?? throw new OrdemDeServicoNaoEncontradaException("Ordem de Serviço não encontrada.");

        // Sem origem nem correlationId: o acompanhamento é público.
        return new AcompanhamentoOSResponse
        {
            NumeroOS = ordemDeServico.NumeroOS,
            Status = ordemDeServico.Status.ToString(),
            Servicos = ordemDeServico.Servicos
                .Select(s => new AcompanhamentoServicoResponse { NomeServico = s.Nome })
                .ToList(),
            Historico = ordemDeServico.Historico
                .OrderBy(h => h.OcorridoEm)
                .Select(h => new AcompanhamentoEtapaResponse { Status = h.StatusNovo.ToString(), Em = h.OcorridoEm })
                .ToList()
        };
    }
}
