using GarageOS.Api.Seguranca;
using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>Controller de gerenciamento de ordens de serviço</summary>
/// <remarks>
/// Fase 4: orçamento e pagamento foram para o Billing Service, estoque e
/// execução para o Execução Service. Aqui ficam a abertura, os itens, o status
/// e o histórico da OS.
/// </remarks>
[Authorize(Policy = Politicas.Admin)]
[ApiController]
[Route("api/[controller]")]
public class OrdensDeServicoController : ControllerBase
{
    private readonly AbrirOrdemDeServicoCompletaUseCase _abrirCompletaUseCase;
    private readonly ListarOrdensDeServicoUseCase _listarUseCase;
    private readonly ObterOrdemDeServicoUseCase _obterUseCase;
    private readonly AdicionarServicoNaOSUseCase _adicionarServicoUseCase;
    private readonly AdicionarPecaNaOSUseCase _adicionarPecaUseCase;
    private readonly AlterarStatusOrdemDeServicoUseCase _alterarStatusUseCase;
    private readonly AcompanharOrdemDeServicoUseCase _acompanharUseCase;
    private readonly SolicitarOrcamentoUseCase _solicitarOrcamentoUseCase;
    private readonly ObterSagaUseCase _obterSagaUseCase;

    /// <summary>Inicializa o controller com os use cases de ordens de serviço</summary>
    public OrdensDeServicoController(
        AbrirOrdemDeServicoCompletaUseCase abrirCompletaUseCase,
        ListarOrdensDeServicoUseCase listarUseCase,
        ObterOrdemDeServicoUseCase obterUseCase,
        AdicionarServicoNaOSUseCase adicionarServicoUseCase,
        AdicionarPecaNaOSUseCase adicionarPecaUseCase,
        AlterarStatusOrdemDeServicoUseCase alterarStatusUseCase,
        AcompanharOrdemDeServicoUseCase acompanharUseCase,
        SolicitarOrcamentoUseCase solicitarOrcamentoUseCase,
        ObterSagaUseCase obterSagaUseCase)
    {
        _abrirCompletaUseCase = abrirCompletaUseCase;
        _listarUseCase = listarUseCase;
        _obterUseCase = obterUseCase;
        _adicionarServicoUseCase = adicionarServicoUseCase;
        _adicionarPecaUseCase = adicionarPecaUseCase;
        _alterarStatusUseCase = alterarStatusUseCase;
        _acompanharUseCase = acompanharUseCase;
        _solicitarOrcamentoUseCase = solicitarOrcamentoUseCase;
        _obterSagaUseCase = obterSagaUseCase;
    }

    /// <summary>Abre uma nova Ordem de Serviço completa</summary>
    /// <remarks>
    /// Cria uma nova Ordem de Serviço vinculando, em uma única chamada, cliente, veículo,
    /// serviço(s) e peça(s) (lista de peças pode ser vazia, mas deve estar presente).
    /// A OS é inicializada com status "Recebida" e um número sequencial único no formato OS-{ANO}-{SEQUENCIAL:D5}.
    ///
    /// Nome e preço de cada serviço e peça são copiados para a OS. As peças são
    /// conferidas no estoque do Execução Service (existência e quantidade disponível).
    /// Caso qualquer referência não seja encontrada, nada é persistido.
    /// </remarks>
    /// <param name="request">Dados para abertura completa da Ordem de Serviço (ClienteId, VeiculoId, ServicosIds, Pecas)</param>
    /// <response code="201">Ordem de Serviço criada com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Cliente, veículo, serviço ou peça não encontrado</response>
    /// <response code="409">Peça sem quantidade disponível</response>
    /// <response code="503">Execução Service indisponível para conferir as peças</response>
    [HttpPost("abertura-completa")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AbrirCompleta([FromBody] AbrirOrdemDeServicoCompletaRequest request)
    {
        var validation = await new AbrirOrdemDeServicoCompletaValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _abrirCompletaUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Lista as Ordens de Serviço em andamento</summary>
    /// <remarks>
    /// Entregues e canceladas ficam de fora. Ordem de atendimento: EmExecucao,
    /// AguardandoPagamento, AguardandoAprovacao, EmDiagnostico, Recebida; dentro de
    /// cada grupo, da mais antiga para a mais nova.
    /// </remarks>
    /// <response code="200">Lista de Ordens de Serviço retornada com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrdemDeServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _listarUseCase.ExecutarAsync());
    }

    /// <summary>Obtém os dados de uma Ordem de Serviço</summary>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <response code="200">Ordem de Serviço encontrada</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        return Ok(await _obterUseCase.ExecutarAsync(id));
    }

    /// <summary>Histórico de status da Ordem de Serviço</summary>
    /// <remarks>
    /// Cada transição com status anterior, novo, origem (Sistema, Manual e, a partir
    /// da etapa 5, Saga), horário e o X-Correlation-Id da requisição que a causou.
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <response code="200">Histórico, do mais antigo para o mais novo</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    [HttpGet("{id:guid}/historico")]
    [ProducesResponseType(typeof(IEnumerable<HistoricoStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Historico(Guid id)
    {
        return Ok(await _obterUseCase.ObterHistoricoAsync(id));
    }

    /// <summary>Inclui um serviço do catálogo na Ordem de Serviço</summary>
    /// <remarks>
    /// Permitido até o diagnóstico. Uma OS em "Recebida" passa a "EmDiagnostico".
    /// </remarks>
    /// <response code="200">Serviço incluído</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Ordem de Serviço ou serviço não encontrado</response>
    /// <response code="409">A OS já passou do diagnóstico</response>
    [HttpPost("{id:guid}/servicos")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdicionarServico(Guid id, [FromBody] AdicionarServicoRequest request)
    {
        var validation = await new AdicionarServicoValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        return Ok(await _adicionarServicoUseCase.ExecutarAsync(id, request));
    }

    /// <summary>Inclui uma peça do estoque na Ordem de Serviço</summary>
    /// <remarks>
    /// A peça é conferida no Execução Service (existência e quantidade disponível) e
    /// entra com o valor do momento. Não reserva: a reserva acontece depois da aprovação
    /// do orçamento. Permitido até o diagnóstico.
    ///
    /// Substitui a rota POST /{id}/estoques da Fase 3.
    /// </remarks>
    /// <response code="200">Peça incluída</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Ordem de Serviço ou peça não encontrada</response>
    /// <response code="409">Sem quantidade disponível, ou a OS já passou do diagnóstico</response>
    /// <response code="503">Execução Service indisponível</response>
    [HttpPost("{id:guid}/pecas")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AdicionarPeca(Guid id, [FromBody] AdicionarPecaRequest request)
    {
        var validation = await new AdicionarPecaValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        return Ok(await _adicionarPecaUseCase.ExecutarAsync(id, request));
    }

    /// <summary>Fecha o diagnóstico e pede o orçamento: inicia o fluxo da saga</summary>
    /// <remarks>
    /// Publica o pedido com os serviços e peças da OS. Daí em diante a saga conduz:
    /// o Billing gera e envia o orçamento (OS em AguardandoAprovacao), o cliente
    /// responde no Billing, o Execução reserva as peças, o Billing cobra (OS em
    /// AguardandoPagamento), o Execução executa (EmExecucao) e conclui (Finalizada).
    /// Qualquer falha compensa os passos já feitos e cancela a OS.
    ///
    /// Assíncrono: responde 202. Acompanhe por GET /{id}/saga ou GET /{id}/historico.
    /// </remarks>
    /// <response code="202">Pedido aceito; a saga segue em segundo plano</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="409">A OS já passou do diagnóstico ou não tem serviço</response>
    [HttpPost("{id:guid}/orcamento/solicitar")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SolicitarOrcamento(Guid id)
    {
        var resultado = await _solicitarOrcamentoUseCase.ExecutarAsync(id);
        return AcceptedAtAction(nameof(Saga), new { id }, resultado);
    }

    /// <summary>Estado da saga da Ordem de Serviço</summary>
    /// <remarks>
    /// Etapa atual (Aberta, AguardandoOrcamento, AguardandoAprovacao, ReservandoPecas,
    /// AguardandoPagamento, EmExecucao, Finalizada ou Cancelada), IDs do orçamento e do
    /// pagamento nos outros serviços e, se cancelada, o motivo.
    /// </remarks>
    /// <response code="200">Estado da saga</response>
    /// <response code="404">A OS não tem saga</response>
    [HttpGet("{id:guid}/saga")]
    [ProducesResponseType(typeof(EstadoDaSaga), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Saga(Guid id)
    {
        return Ok(await _obterSagaUseCase.ExecutarAsync(id));
    }

    /// <summary>Registra a entrega do veículo</summary>
    /// <remarks>
    /// Da solicitação do orçamento até a finalização, quem move a OS é a saga. Por esta
    /// rota a equipe registra só a entrega: Finalizada → Entregue.
    /// </remarks>
    /// <response code="200">Status alterado</response>
    /// <response code="400">Status inválido</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="409">Transição não permitida a partir do status atual</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusRequest request)
    {
        var validation = await new AlterarStatusValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        return Ok(await _alterarStatusUseCase.ExecutarAsync(id, request));
    }

    /// <summary>Acompanha uma Ordem de Serviço pelo número (público)</summary>
    /// <remarks>
    /// Endpoint PÚBLICO (sem autenticação) que permite qualquer pessoa acompanhar o status
    /// e o andamento de uma Ordem de Serviço usando apenas seu número único.
    ///
    /// Retorna informações simplificadas (sem dados sensíveis):
    /// - Número da OS (ex: OS-2026-00001)
    /// - Status atual
    /// - Serviços incluídos
    /// - Etapas já percorridas, com data e hora
    /// </remarks>
    /// <param name="numeroOS">Número único da Ordem de Serviço no formato OS-{ANO}-{SEQUENCIAL} (ex: OS-2026-00001)</param>
    /// <response code="200">Ordem de Serviço encontrada</response>
    /// <response code="404">Ordem de Serviço não encontrada com este número</response>
    [AllowAnonymous]
    [HttpGet("acompanhar/{numeroOS}")]
    [ProducesResponseType(typeof(AcompanhamentoOSResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acompanhar(string numeroOS)
    {
        return Ok(await _acompanharUseCase.ExecutarAsync(numeroOS));
    }
}
