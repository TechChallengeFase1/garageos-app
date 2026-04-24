using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

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

    public OrdensDeServicoController(
        CriarOrdemDeServicoUseCase criarUseCase,
        ListarOrdensDeServicoUseCase listarUseCase,
        ObterOrdemDeServicoUseCase obterUseCase,
        AdicionarServicoNaOSUseCase adicionarServicoUseCase,
        AdicionarEstoqueNaOSUseCase adicionarEstoqueUseCase,
        AlterarStatusOrdemDeServicoUseCase alterarStatusUseCase,
        AcompanharOrdemDeServicoUseCase acompanharUseCase)
    {
        _criarUseCase = criarUseCase;
        _listarUseCase = listarUseCase;
        _obterUseCase = obterUseCase;
        _adicionarServicoUseCase = adicionarServicoUseCase;
        _adicionarEstoqueUseCase = adicionarEstoqueUseCase;
        _alterarStatusUseCase = alterarStatusUseCase;
        _acompanharUseCase = acompanharUseCase;
    }

    /// <summary>Cria uma nova Ordem de Serviço.</summary>
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

    /// <summary>Lista todas as Ordens de Serviço.</summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrdemDeServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Obtém uma Ordem de Serviço pelo ID.</summary>
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

    /// <summary>Adiciona um serviço à Ordem de Serviço.</summary>
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

    /// <summary>Adiciona um item de estoque à Ordem de Serviço.</summary>
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

    /// <summary>Altera o status da Ordem de Serviço (apenas Finalizada e Entregue).</summary>
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

    /// <summary>Acompanha uma Ordem de Serviço pelo número (público, sem autenticação).</summary>
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
