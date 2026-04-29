using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.Utils;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class CriarOrdemDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IVeiculoRepository _veiculoRepository;

    public CriarOrdemDeServicoUseCase(
        IOrdemDeServicoRepository repository,
        IClienteRepository clienteRepository,
        IVeiculoRepository veiculoRepository)
    {
        _repository = repository;
        _clienteRepository = clienteRepository;
        _veiculoRepository = veiculoRepository;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(CriarOrdemDeServicoRequest request)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(request.ClienteId);
        if (cliente == null)
            throw new ClienteNaoEncontradoException(request.ClienteId);

        var veiculo = await _veiculoRepository.ObterPorIdAsync(request.VeiculoId);
        if (veiculo == null)
            throw new VeiculoNaoEncontradoException(request.VeiculoId);

        var ano = BrasiliaTime.Agora.Year;
        var ultimoSequencial = await _repository.ObterUltimoSequencialDoAnoAsync(ano);
        var novoSequencial = ultimoSequencial + 1;
        var numeroOS = $"OS-{ano}-{novoSequencial:D5}";

        var ordemDeServico = new OrdemDeServico(numeroOS, request.ClienteId, request.VeiculoId);
        await _repository.AdicionarAsync(ordemDeServico);

        return MapearParaResponse(ordemDeServico);
    }

    private static OrdemDeServicoResponse MapearParaResponse(OrdemDeServico ordemDeServico)
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
