using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.Utils;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class AbrirOrdemDeServicoCompletaUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IVeiculoRepository _veiculoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IEstoqueRepository _estoqueRepository;

    public AbrirOrdemDeServicoCompletaUseCase(
        IOrdemDeServicoRepository repository,
        IClienteRepository clienteRepository,
        IVeiculoRepository veiculoRepository,
        IServicoRepository servicoRepository,
        IEstoqueRepository estoqueRepository)
    {
        _repository = repository;
        _clienteRepository = clienteRepository;
        _veiculoRepository = veiculoRepository;
        _servicoRepository = servicoRepository;
        _estoqueRepository = estoqueRepository;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(AbrirOrdemDeServicoCompletaRequest request)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(request.ClienteId);
        if (cliente == null)
            throw new ClienteNaoEncontradoException(request.ClienteId);

        var veiculo = await _veiculoRepository.ObterPorIdAsync(request.VeiculoId);
        if (veiculo == null)
            throw new VeiculoNaoEncontradoException(request.VeiculoId);

        var servicosIdsValidados = new List<Guid>();
        foreach (var servicoId in request.ServicosIds)
        {
            var servico = await _servicoRepository.ObterPorIdAsync(servicoId);
            if (servico == null)
                throw new ServicoNaoEncontradoException(servicoId);

            servicosIdsValidados.Add(servicoId);
        }

        var pecas = request.Pecas ?? [];
        var pecasValidadas = new List<PecaRequest>();
        foreach (var peca in pecas)
        {
            var estoque = await _estoqueRepository.ObterPorIdAsync(peca.EstoqueId);
            if (estoque == null)
                throw new EstoqueNaoEncontradoException(peca.EstoqueId);

            pecasValidadas.Add(peca);
        }

        var ano = BrasiliaTime.Agora.Year;
        var ultimoSequencial = await _repository.ObterUltimoSequencialDoAnoAsync(ano);
        var novoSequencial = ultimoSequencial + 1;
        var numeroOS = $"OS-{ano}-{novoSequencial:D5}";

        var ordemDeServico = new OrdemDeServico(numeroOS, request.ClienteId, request.VeiculoId);

        foreach (var servicoId in servicosIdsValidados)
            ordemDeServico.AdicionarServico(new OrdemDeServicoServico(ordemDeServico.Id, servicoId));

        foreach (var peca in pecasValidadas)
            ordemDeServico.AdicionarEstoque(new OrdemDeServicoEstoque(ordemDeServico.Id, peca.EstoqueId, peca.Quantidade));

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
