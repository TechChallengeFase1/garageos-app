using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
using GarageOS.Contracts.V1;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

/// <summary>Fecha o diagnóstico e pede o orçamento: a saga assume daqui</summary>
/// <remarks>
/// Publica OrcamentoSolicitado com os itens e os preços copiados. Daqui em
/// diante quem move a OS é a saga (OrdemDeServicoStateMachine): Billing gera e
/// envia o orçamento, o cliente responde, o Execução reserva as peças, o
/// Billing cobra, o Execução executa.
///
/// Assíncrono: a resposta é 202 e o status muda quando os outros serviços
/// responderem. O cliente acompanha por GET /historico ou GET /saga.
/// </remarks>
public class SolicitarOrcamentoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;
    private readonly IPublicadorDeEventos _publicador;

    public SolicitarOrcamentoUseCase(IOrdemDeServicoRepository repository, IPublicadorDeEventos publicador)
    {
        _repository = repository;
        _publicador = publicador;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid ordemDeServicoId)
    {
        var os = await _repository.ObterPorIdAsync(ordemDeServicoId)
            ?? throw new OrdemDeServicoNaoEncontradaException();

        if (os.Status is not (StatusOrdemDeServico.Recebida or StatusOrdemDeServico.EmDiagnostico))
            throw new OrdemDeServicoStatusInvalidoException(
                $"A OS está {os.Status}: o orçamento só pode ser pedido até o diagnóstico.");

        if (os.Servicos.Count == 0)
            throw new OrdemDeServicoStatusInvalidoException("Inclua ao menos um serviço antes de pedir o orçamento.");

        var itens = os.Servicos
            .Select(s => new ItemDoPedido("Servico", s.ServicoId, s.Nome, 1, s.PrecoUnitario))
            .Concat(os.Pecas.Select(p => new ItemDoPedido("Peca", p.EstoqueId, p.Nome, p.Quantidade, p.ValorUnitario)))
            .ToList();

        await _publicador.PublicarAsync(new OrcamentoSolicitado(
            os.Id,
            os.NumeroOS,
            os.ClienteId,
            itens,
            os.Pecas.Select(p => new PecaDoPedido(p.EstoqueId, p.Quantidade)).ToList(),
            os.Servicos.Select(s => new ServicoDoPedido(s.ServicoId, s.Nome)).ToList()));

        // Grava o outbox (a OS em si não mudou).
        await _repository.AtualizarAsync(os);

        return os.ParaResponse();
    }
}
