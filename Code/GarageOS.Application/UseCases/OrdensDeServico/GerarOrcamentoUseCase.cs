using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class GerarOrcamentoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IOrcamentoRepository _orcamentoRepository;

    public GerarOrcamentoUseCase(
        IOrdemDeServicoRepository repository,
        IOrcamentoRepository orcamentoRepository)
    {
        _repository = repository;
        _orcamentoRepository = orcamentoRepository;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId)
    {
        var os = await _repository.ObterPorIdAsync(ordemDeServicoId);
        if (os == null)
            throw new OrdemDeServicoNaoEncontradaException();

        var totalServicos = os.Servicos.Sum(s => s.Servico?.Preco ?? 0);
        var totalEstoques = os.Estoques.Sum(e => (e.Estoque?.Valor ?? 0) * e.Quantidade);
        var preco = totalServicos + totalEstoques;

        var orcamento = new Orcamento(os.Id, preco);
        os.VincularOrcamento(orcamento);

        await _orcamentoRepository.AdicionarAsync(orcamento);
        await _repository.AtualizarAsync(os);

        return MapearParaResponse(os);
    }

    private static OrdemDeServicoResponse MapearParaResponse(OrdemDeServico os) =>
        new()
        {
            Id = os.Id,
            NumeroOS = os.NumeroOS,
            Status = os.Status,
            CriadoEm = os.CriadoEm,
            FinalizadaEm = os.FinalizadaEm,
            AtualizadoEm = os.AtualizadoEm,
            ClienteId = os.ClienteId,
            VeiculoId = os.VeiculoId,
            Servicos = os.Servicos
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
            Estoques = os.Estoques
                .Select(e => new EstoqueItemResponse
                {
                    Id = e.Id,
                    EstoqueId = e.EstoqueId,
                    EstoqueNome = e.Estoque?.Nome ?? string.Empty,
                    Quantidade = e.Quantidade
                })
                .ToList(),
            Orcamento = os.Orcamento != null
                ? new OrcamentoResponse
                {
                    Id = os.Orcamento.Id,
                    Status = os.Orcamento.Status,
                    Preco = os.Orcamento.Preco,
                    CriadoEm = os.Orcamento.CriadoEm,
                    AtualizadoEm = os.Orcamento.AtualizadoEm
                }
                : null
        };
}
