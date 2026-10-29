using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
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
    private readonly IEstoqueClient _estoque;
    private readonly IMetricasDeNegocio _metricas;
    private readonly IContextoDaRequisicao _contexto;

    public AbrirOrdemDeServicoCompletaUseCase(
        IOrdemDeServicoRepository repository,
        IClienteRepository clienteRepository,
        IVeiculoRepository veiculoRepository,
        IServicoRepository servicoRepository,
        IEstoqueClient estoque,
        IMetricasDeNegocio metricas,
        IContextoDaRequisicao contexto)
    {
        _repository = repository;
        _clienteRepository = clienteRepository;
        _veiculoRepository = veiculoRepository;
        _servicoRepository = servicoRepository;
        _estoque = estoque;
        _metricas = metricas;
        _contexto = contexto;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(AbrirOrdemDeServicoCompletaRequest request)
    {
        _ = await _clienteRepository.ObterPorIdAsync(request.ClienteId)
            ?? throw new ClienteNaoEncontradoException(request.ClienteId);

        _ = await _veiculoRepository.ObterPorIdAsync(request.VeiculoId)
            ?? throw new VeiculoNaoEncontradoException(request.VeiculoId);

        // Tudo validado ANTES de gravar: se uma referência não existir, nada é
        // persistido.
        var servicos = new List<(Guid, string, decimal)>();
        foreach (var servicoId in request.ServicosIds)
        {
            var servico = await _servicoRepository.ObterPorIdAsync(servicoId)
                ?? throw new ServicoNaoEncontradoException(servicoId);

            servicos.Add((servico.Id, servico.NomeServico, servico.Preco));
        }

        var pecas = new List<(Guid, string, decimal, int)>();
        foreach (var pedido in request.Pecas ?? [])
        {
            var peca = await _estoque.ObterDisponivelAsync(pedido.EstoqueId, pedido.Quantidade);
            pecas.Add((peca.Id, peca.Nome, peca.Valor, pedido.Quantidade));
        }

        var ano = BrasiliaTime.Agora.Year;
        var sequencial = await _repository.ObterProximoSequencialDoAnoAsync(ano);
        var numeroOS = $"OS-{ano}-{sequencial:D5}";

        var ordemDeServico = new OrdemDeServico(
            numeroOS, request.ClienteId, request.VeiculoId, servicos, pecas, _contexto.CorrelationId);

        try
        {
            await _repository.AdicionarAsync(ordemDeServico);
        }
        catch (Exception ex)
        {
            _metricas.FalhaNoProcessamento(ordemDeServico.NumeroOS, ex.GetType().Name);
            throw;
        }

        // Origem do dashboard "volume diario de ordens de servico". As
        // transacoes do APM contariam a REQUISICAO; este evento conta a OS.
        _metricas.OrdemDeServicoCriada(ordemDeServico.NumeroOS, ordemDeServico.ClienteId);

        return ordemDeServico.ParaResponse();
    }
}
