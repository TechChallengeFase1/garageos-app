using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

/// <summary>Ordem de serviço: o agregado raiz do ciclo de vida do atendimento</summary>
/// <remarks>
/// Máquina de estados explícita. Até a Fase 3 o status era só um campo com
/// setters, e qualquer status podia virar qualquer outro. Agora só as
/// transições da tabela são aceitas, e cada uma fica no histórico.
///
///   Recebida            -> EmDiagnostico       | Cancelada
///   EmDiagnostico       -> AguardandoAprovacao | Cancelada
///   AguardandoAprovacao -> AguardandoPagamento | Cancelada
///   AguardandoPagamento -> EmExecucao          | Cancelada
///   EmExecucao          -> Finalizada          | Cancelada
///   Finalizada          -> Entregue
///   Entregue e Cancelada são finais.
///
/// Na etapa 5 a saga passa a conduzir as transições do meio; esta tabela é a
/// mesma para ela e para a operação manual.
/// </remarks>
public class OrdemDeServico
{
    private static readonly Dictionary<StatusOrdemDeServico, StatusOrdemDeServico[]> TransicoesPermitidas = new()
    {
        [StatusOrdemDeServico.Recebida] = [StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.Cancelada],
        [StatusOrdemDeServico.EmDiagnostico] = [StatusOrdemDeServico.AguardandoAprovacao, StatusOrdemDeServico.Cancelada],
        [StatusOrdemDeServico.AguardandoAprovacao] = [StatusOrdemDeServico.AguardandoPagamento, StatusOrdemDeServico.Cancelada],
        [StatusOrdemDeServico.AguardandoPagamento] = [StatusOrdemDeServico.EmExecucao, StatusOrdemDeServico.Cancelada],
        [StatusOrdemDeServico.EmExecucao] = [StatusOrdemDeServico.Finalizada, StatusOrdemDeServico.Cancelada],
        [StatusOrdemDeServico.Finalizada] = [StatusOrdemDeServico.Entregue],
        [StatusOrdemDeServico.Entregue] = [],
        [StatusOrdemDeServico.Cancelada] = []
    };

    /// <summary>Origem das transições feitas pela própria OS (abertura, inclusão de itens)</summary>
    public const string OrigemSistema = "Sistema";

    private readonly List<OrdemDeServicoServico> _servicos = [];
    private readonly List<OrdemDeServicoPeca> _pecas = [];
    private readonly List<HistoricoStatusOS> _historico = [];

    public Guid Id { get; private set; }
    public string NumeroOS { get; private set; } = null!;
    public StatusOrdemDeServico Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? FinalizadaEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public IReadOnlyCollection<OrdemDeServicoServico> Servicos => _servicos.AsReadOnly();
    public IReadOnlyCollection<OrdemDeServicoPeca> Pecas => _pecas.AsReadOnly();
    public IReadOnlyCollection<HistoricoStatusOS> Historico => _historico.AsReadOnly();

    /// <summary>Soma dos serviços e peças pelos preços copiados. O valor oficial é o do orçamento.</summary>
    public decimal ValorEstimado => _servicos.Sum(s => s.PrecoUnitario) + _pecas.Sum(p => p.Subtotal);

    protected OrdemDeServico() { }

    /// <summary>Abre a OS em Recebida</summary>
    /// <remarks>
    /// Os itens da abertura entram sem mudar o status. No monólito cada item
    /// adicionado empurrava a OS para EmDiagnostico, e a abertura completa
    /// terminava em EmDiagnostico, ao contrário do que a documentação dizia.
    /// </remarks>
    public OrdemDeServico(
        string numeroOS,
        Guid clienteId,
        Guid veiculoId,
        IEnumerable<(Guid ServicoId, string Nome, decimal Preco)> servicos,
        IEnumerable<(Guid EstoqueId, string Nome, decimal Valor, int Quantidade)> pecas,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(numeroOS))
            throw new ArgumentException("Número da OS é obrigatório.", nameof(numeroOS));

        if (clienteId == Guid.Empty)
            throw new ArgumentException("Cliente é obrigatório.", nameof(clienteId));

        if (veiculoId == Guid.Empty)
            throw new ArgumentException("Veículo é obrigatório.", nameof(veiculoId));

        Id = Guid.NewGuid();
        NumeroOS = numeroOS;
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOrdemDeServico.Recebida;
        CriadoEm = BrasiliaTime.Agora;
        AtualizadoEm = CriadoEm;

        foreach (var (servicoId, nome, preco) in servicos)
            _servicos.Add(new OrdemDeServicoServico(Id, servicoId, nome, preco));

        foreach (var (estoqueId, nome, valor, quantidade) in pecas)
            _pecas.Add(new OrdemDeServicoPeca(Id, estoqueId, nome, valor, quantidade));

        _historico.Add(new HistoricoStatusOS(Id, null, Status, OrigemSistema, correlationId));
    }

    public static bool TransicaoPermitida(StatusOrdemDeServico de, StatusOrdemDeServico para) =>
        TransicoesPermitidas[de].Contains(para);

    /// <summary>Inclui um serviço. Uma OS recém-recebida passa a EmDiagnostico.</summary>
    public OrdemDeServicoServico AdicionarServico(Guid servicoId, string nome, decimal preco, string? correlationId = null)
    {
        GarantirQueAceitaItens();

        var item = new OrdemDeServicoServico(Id, servicoId, nome, preco);
        _servicos.Add(item);
        IniciarDiagnosticoSeRecebida(correlationId);
        return item;
    }

    /// <summary>Inclui uma peça. Uma OS recém-recebida passa a EmDiagnostico.</summary>
    public OrdemDeServicoPeca AdicionarPeca(Guid estoqueId, string nome, decimal valor, int quantidade, string? correlationId = null)
    {
        GarantirQueAceitaItens();

        var item = new OrdemDeServicoPeca(Id, estoqueId, nome, valor, quantidade);
        _pecas.Add(item);
        IniciarDiagnosticoSeRecebida(correlationId);
        return item;
    }

    /// <summary>Move a OS para outro status, respeitando a tabela de transições</summary>
    /// <param name="novoStatus">Status de destino</param>
    /// <param name="origem">Quem pediu: "Manual", "Saga", ...</param>
    /// <param name="correlationId">Identificador da requisição ou mensagem que causou a mudança</param>
    public void AlterarStatus(StatusOrdemDeServico novoStatus, string origem, string? correlationId = null)
    {
        if (!TransicaoPermitida(Status, novoStatus))
            throw new OrdemDeServicoStatusInvalidoException(
                $"Transição de status inválida: {Status} -> {novoStatus}.");

        var anterior = Status;
        Status = novoStatus;
        AtualizadoEm = BrasiliaTime.Agora;

        if (novoStatus is StatusOrdemDeServico.Finalizada or StatusOrdemDeServico.Cancelada)
            FinalizadaEm = AtualizadoEm;

        _historico.Add(new HistoricoStatusOS(Id, anterior, novoStatus, origem, correlationId));
    }

    private void GarantirQueAceitaItens()
    {
        // Depois de pedido o orçamento, mudar os itens mudaria o que o cliente
        // está aprovando.
        if (Status is not (StatusOrdemDeServico.Recebida or StatusOrdemDeServico.EmDiagnostico))
            throw new OrdemDeServicoStatusInvalidoException(
                $"A OS está {Status}: serviços e peças só podem ser incluídos até o diagnóstico.");
    }

    private void IniciarDiagnosticoSeRecebida(string? correlationId)
    {
        if (Status == StatusOrdemDeServico.Recebida)
            AlterarStatus(StatusOrdemDeServico.EmDiagnostico, OrigemSistema, correlationId);
        else
            AtualizadoEm = BrasiliaTime.Agora;
    }
}
