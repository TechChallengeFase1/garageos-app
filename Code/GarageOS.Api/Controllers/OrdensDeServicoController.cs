using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

/// <summary>Controller de gerenciamento de ordens de serviço</summary>
[ApiController]
[Route("api/[controller]")]
public class OrdensDeServicoController : ControllerBase
{
    private readonly CriarOrdemDeServicoUseCase _criarUseCase;
    private readonly ListarOrdensDeServicoUseCase _listarUseCase;
    private readonly ObterOrdemDeServicoUseCase _obterUseCase;
    private readonly AdicionarServicoNaOSUseCase _adicionarServicoUseCase;
    private readonly AdicionarEstoqueNaOSUseCase _adicionarEstoqueUseCase;
    private readonly AlterarStatusOrdemDeServicoUseCase _alterarStatusUseCase;
    private readonly AcompanharOrdemDeServicoUseCase _acompanharUseCase;
    private readonly GerarOrcamentoUseCase _gerarOrcamentoUseCase;
    private readonly EnviarOrcamentoUseCase _enviarOrcamentoUseCase;
    private readonly ResponderOrcamentoUseCase _responderOrcamentoUseCase;
    private readonly AlterarStatusServicoNaOSUseCase _alterarStatusServicoUseCase;
    private readonly CalcularAgingServicosUseCase _calcularAgingUseCase;

    /// <summary>Inicializa o controller com os use cases de ordens de serviço</summary>
    public OrdensDeServicoController(
        CriarOrdemDeServicoUseCase criarUseCase,
        ListarOrdensDeServicoUseCase listarUseCase,
        ObterOrdemDeServicoUseCase obterUseCase,
        AdicionarServicoNaOSUseCase adicionarServicoUseCase,
        AdicionarEstoqueNaOSUseCase adicionarEstoqueUseCase,
        AlterarStatusOrdemDeServicoUseCase alterarStatusUseCase,
        AcompanharOrdemDeServicoUseCase acompanharUseCase,
        GerarOrcamentoUseCase gerarOrcamentoUseCase,
        EnviarOrcamentoUseCase enviarOrcamentoUseCase,
        ResponderOrcamentoUseCase responderOrcamentoUseCase,
        AlterarStatusServicoNaOSUseCase alterarStatusServicoUseCase,
        CalcularAgingServicosUseCase calcularAgingUseCase)
    {
        _criarUseCase = criarUseCase;
        _listarUseCase = listarUseCase;
        _obterUseCase = obterUseCase;
        _adicionarServicoUseCase = adicionarServicoUseCase;
        _adicionarEstoqueUseCase = adicionarEstoqueUseCase;
        _alterarStatusUseCase = alterarStatusUseCase;
        _acompanharUseCase = acompanharUseCase;
        _gerarOrcamentoUseCase = gerarOrcamentoUseCase;
        _enviarOrcamentoUseCase = enviarOrcamentoUseCase;
        _responderOrcamentoUseCase = responderOrcamentoUseCase;
        _alterarStatusServicoUseCase = alterarStatusServicoUseCase;
        _calcularAgingUseCase = calcularAgingUseCase;
    }

    /// <summary>Cria uma nova Ordem de Serviço</summary>
    /// <remarks>
    /// Cria uma nova Ordem de Serviço vinculada a um cliente e um veículo.
    /// A OS é inicializada com status "Recebida" e um número sequencial único no formato OS-{ANO}-{SEQUENCIAL:D5}.
    ///
    /// Exemplo de resposta:
    /// - NumeroOS: OS-2026-00001
    /// - Status: Recebida
    /// - ClienteId e VeiculoId devem existir no sistema
    /// </remarks>
    /// <param name="request">Dados para criação da Ordem de Serviço (ClienteId, VeiculoId)</param>
    /// <returns>Ordem de Serviço criada com sucesso (201)</returns>
    /// <response code="201">Ordem de Serviço criada com sucesso</response>
    /// <response code="400">Cliente ou Veículo não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarOrdemDeServicoRequest request)
    {
        var validator = new CriarOrdemDeServicoValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _criarUseCase.ExecutarAsync(request);
            return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Lista todas as Ordens de Serviço cadastradas</summary>
    /// <remarks>
    /// Retorna uma lista completa de todas as Ordens de Serviço do sistema.
    /// Cada OS inclui seus serviços, itens de estoque, orçamento (se existente) e informações de cliente/veículo.
    /// </remarks>
    /// <returns>Lista de Ordens de Serviço</returns>
    /// <response code="200">Lista de Ordens de Serviço retornada com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrdemDeServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Obtém uma Ordem de Serviço específica pelo ID</summary>
    /// <remarks>
    /// Retorna os detalhes completos de uma Ordem de Serviço específica,
    /// incluindo todos os seus serviços associados, itens de estoque e orçamento.
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <returns>Dados completos da Ordem de Serviço</returns>
    /// <response code="200">Ordem de Serviço encontrada e retornada com sucesso</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Adiciona um serviço à Ordem de Serviço</summary>
    /// <remarks>
    /// Adiciona um serviço já cadastrado à Ordem de Serviço.
    /// Ao adicionar o primeiro serviço, o status da OS muda de "Recebida" para "EmDiagnostico".
    /// O serviço deve existir no sistema e ser válido.
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <param name="request">Dados do serviço a adicionar (ServicoId)</param>
    /// <returns>Ordem de Serviço atualizada com o novo serviço</returns>
    /// <response code="200">Serviço adicionado com sucesso</response>
    /// <response code="400">Serviço não encontrado ou inválido</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPost("{id:guid}/servicos")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdicionarServico(Guid id, [FromBody] AdicionarServicoRequest request)
    {
        var validator = new AdicionarServicoValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _adicionarServicoUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ServicoNaoEncontradoException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Adiciona um item de estoque (peça/insumo) à Ordem de Serviço</summary>
    /// <remarks>
    /// Adiciona um item de estoque já cadastrado à Ordem de Serviço com uma quantidade específica.
    /// Ao adicionar o primeiro item, o status da OS muda de "Recebida" para "EmDiagnostico".
    /// A quantidade deve ser maior que zero e o item de estoque deve existir no sistema.
    ///
    /// Nota: O decremento da quantidade em estoque ocorre apenas na aprovação do orçamento,
    /// não quando o item é adicionado à OS.
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <param name="request">Dados do item de estoque (EstoqueId, Quantidade)</param>
    /// <returns>Ordem de Serviço atualizada com o novo item de estoque</returns>
    /// <response code="200">Item de estoque adicionado com sucesso</response>
    /// <response code="400">Item de estoque não encontrado, quantidade inválida ou Ordem de Serviço não encontrada</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPost("{id:guid}/estoques")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdicionarEstoque(Guid id, [FromBody] AdicionarEstoqueRequest request)
    {
        var validator = new AdicionarEstoqueValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _adicionarEstoqueUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Altera manualmente o status da Ordem de Serviço</summary>
    /// <remarks>
    /// Permite alterar manualmente o status de uma Ordem de Serviço para estados finais.
    /// Apenas os status "Finalizada" e "Entregue" podem ser definidos via este endpoint.
    /// Outros status são definidos automaticamente pelo fluxo de negócio:
    /// - Recebida: Status inicial ao criar a OS
    /// - EmDiagnostico: Definido automaticamente ao adicionar serviços/peças
    /// - AguardandoAprovacao: Definido ao gerar orçamento
    /// - EmExecucao: Definido ao aprovar orçamento
    /// - Finalizada/Entregue: Definidos via este endpoint
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <param name="request">Novo status desejado (apenas Finalizada ou Entregue)</param>
    /// <returns>Ordem de Serviço atualizada com o novo status</returns>
    /// <response code="200">Status alterado com sucesso</response>
    /// <response code="400">Status inválido (apenas Finalizada e Entregue são permitidos)</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusRequest request)
    {
        var validator = new AlterarStatusValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarStatusUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Gera o orçamento de uma Ordem de Serviço</summary>
    /// <remarks>
    /// Calcula o preço total da OS somando os valores dos serviços e das peças utilizadas,
    /// criando o orçamento com status "Pendente". Deve ser chamado após adicionar todos os
    /// serviços e peças à OS.
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <returns>Ordem de Serviço com o orçamento gerado</returns>
    /// <response code="200">Orçamento gerado com sucesso</response>
    /// <response code="404">Ordem de Serviço não encontrada</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPost("{id:guid}/orcamento")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GerarOrcamento(Guid id)
    {
        try
        {
            var resultado = await _gerarOrcamentoUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Envia o orçamento ao cliente para aprovação</summary>
    /// <remarks>
    /// Avança o status da OS para "AguardandoAprovacao", indicando que o orçamento foi
    /// enviado ao cliente e aguarda sua resposta. A OS deve ter um orçamento gerado previamente.
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <returns>Ordem de Serviço com status atualizado para AguardandoAprovacao</returns>
    /// <response code="200">Orçamento enviado com sucesso</response>
    /// <response code="404">Ordem de Serviço ou orçamento não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPost("{id:guid}/orcamento/enviar")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnviarOrcamento(Guid id)
    {
        try
        {
            var resultado = await _enviarOrcamentoUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (OrcamentoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Registra a resposta do cliente ao orçamento (aprovação ou rejeição)</summary>
    /// <remarks>
    /// Endpoint PÚBLICO (sem autenticação) que permite qualquer pessoa alterar o status do orçamento de uma Ordem de Serviço para aprovado ou rejeitado.
    /// Ao aprovar: o status da OS avança para "EmExecucao" e as peças cadastradas na OS
    /// têm suas quantidades decrementadas no estoque.
    /// Ao reprovar: o orçamento é rejeitado e o status da OS vai para "Finalizada".
    /// </remarks>
    /// <param name="id">ID único da Ordem de Serviço (GUID)</param>
    /// <param name="request">Resposta do cliente: Aprovado = true para aprovar, false para reprovar</param>
    /// <returns>Ordem de Serviço com o status atualizado conforme a resposta</returns>
    /// <response code="200">Resposta registrada com sucesso</response>
    /// <response code="400">Estoque insuficiente para alguma peça</response>
    /// <response code="404">Ordem de Serviço ou orçamento não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPatch("{id:guid}/orcamento/resposta")]
    [ProducesResponseType(typeof(OrdemDeServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResponderOrcamento(Guid id, [FromBody] ResponderOrcamentoRequest request)
    {
        try
        {
            var resultado = await _responderOrcamentoUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (OrcamentoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Altera o status de execução de um serviço dentro de uma OS</summary>
    /// <remarks>
    /// Permite marcar um serviço como Iniciado ou Finalizado.
    /// O serviço deve estar com status Criada para ser iniciado, e Iniciado para ser finalizado.
    /// Os timestamps IniciadaEm e FinalizadaEm são registrados automaticamente.
    ///
    /// Use o campo 'Id' (não 'ServicoId') retornado na lista de serviços da OS como servicoItemId.
    /// </remarks>
    /// <param name="numeroOS">Número da Ordem de Serviço (ex: OS-2026-00001)</param>
    /// <param name="servicoItemId">ID do item de serviço na OS (OrdemDeServicoServico.Id)</param>
    /// <param name="request">Status desejado: Iniciado (1) ou Finalizado (2)</param>
    /// <returns>Dados atualizados do item de serviço</returns>
    /// <response code="200">Status alterado com sucesso</response>
    /// <response code="400">Status inválido ou transição de status não permitida</response>
    /// <response code="404">OS ou serviço não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpPatch("{numeroOS}/servicos/{servicoItemId:guid}/status")]
    [ProducesResponseType(typeof(ServicoItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatusServico(
        string numeroOS,
        Guid servicoItemId,
        [FromBody] AlterarStatusServicoNaOSRequest request)
    {
        var validator = new AlterarStatusServicoNaOSValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarStatusServicoUseCase.ExecutarAsync(numeroOS, servicoItemId, request);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ServicoNaOSNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>Calcula o aging (tempo médio de execução) por tipo de serviço</summary>
    /// <remarks>
    /// Retorna o tempo médio de execução de cada serviço com base nas OSs finalizadas.
    /// Considera apenas serviços que possuem IniciadaEm e FinalizadaEm preenchidos.
    ///
    /// O campo TempoMedioFormatado exibe o tempo de forma legível (ex: "2h 30min", "45min").
    /// </remarks>
    /// <returns>Lista de serviços com tempo médio de execução</returns>
    /// <response code="200">Aging calculado com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [Authorize]
    [HttpGet("aging")]
    [ProducesResponseType(typeof(IEnumerable<AgingServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalcularAging()
    {
        var resultado = await _calcularAgingUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Acompanha o progresso de uma Ordem de Serviço (público)</summary>
    /// <remarks>
    /// Endpoint PÚBLICO (sem autenticação) que permite qualquer pessoa acompanhar o status
    /// e progresso de uma Ordem de Serviço usando apenas seu número único.
    ///
    /// Este endpoint é útil para:
    /// - Clientes acompanharem o status de seus serviços
    /// - Consulta rápida sem necessidade de login
    /// - Verificar quais serviços estão sendo executados
    ///
    /// Retorna informações simplificadas (sem dados sensíveis) incluindo:
    /// - Número da OS (ex: OS-2026-00001)
    /// - Status atual (Recebida, EmDiagnostico, AguardandoAprovacao, EmExecucao, Finalizada, Entregue)
    /// - Lista de serviços com seus status de execução
    /// </remarks>
    /// <param name="numeroOS">Número único da Ordem de Serviço no formato OS-{ANO}-{SEQUENCIAL} (ex: OS-2026-00001)</param>
    /// <returns>Informações simplificadas de acompanhamento da Ordem de Serviço</returns>
    /// <response code="200">Ordem de Serviço encontrada - status retornado com sucesso</response>
    /// <response code="404">Ordem de Serviço não encontrada com este número</response>
    [AllowAnonymous]
    [HttpGet("acompanhar/{numeroOS}")]
    [ProducesResponseType(typeof(AcompanhamentoOSResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acompanhar(string numeroOS)
    {
        try
        {
            var resultado = await _acompanharUseCase.ExecutarAsync(numeroOS);
            return Ok(resultado);
        }
        catch (OrdemDeServicoNaoEncontradaException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
