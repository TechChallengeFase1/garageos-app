using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class AlterarStatusServicoNaOSUseCase
{
    private readonly IOrdemDeServicoRepository _repository;

    public AlterarStatusServicoNaOSUseCase(IOrdemDeServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServicoItemResponse> ExecutarAsync(
        string numeroOS,
        Guid servicoItemId,
        AlterarStatusServicoNaOSRequest request)
    {
        var os = await _repository.ObterPorNumeroOSComTrackingAsync(numeroOS);
        if (os == null)
            throw new OrdemDeServicoNaoEncontradaException();

        var item = os.Servicos.FirstOrDefault(s => s.Id == servicoItemId);
        if (item == null)
            throw new ServicoNaOSNaoEncontradoException(servicoItemId);

        if (request.Status == StatusExecucaoServico.Iniciado)
            item.IniciarExecucao();
        else
            item.FinalizarExecucao();

        await _repository.AtualizarAsync(os);

        return new ServicoItemResponse
        {
            Id = item.Id,
            ServicoId = item.ServicoId,
            ServicoNome = item.Servico?.NomeServico ?? string.Empty,
            Status = item.Status,
            CriadoEm = item.CriadoEm,
            IniciadaEm = item.IniciadaEm,
            FinalizadaEm = item.FinalizadaEm
        };
    }
}
