using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Application.Validators.Clientes;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ListarClientesUseCase _listarClientesUseCase;
    private readonly CadastrarClienteUseCase _cadastrarClienteUseCase;
    private readonly ObterClienteUseCase _obterClienteUseCase;
    private readonly AlterarClienteUseCase _alterarClienteUseCase;
    private readonly DeletarClienteUseCase _deletarClienteUseCase;

    public ClientesController(
        ListarClientesUseCase listarClientesUseCase,
        CadastrarClienteUseCase cadastrarClienteUseCase,
        ObterClienteUseCase obterClienteUseCase,
        AlterarClienteUseCase alterarClienteUseCase,
        DeletarClienteUseCase deletarClienteUseCase)
    {
        _listarClientesUseCase = listarClientesUseCase;
        _cadastrarClienteUseCase = cadastrarClienteUseCase;
        _obterClienteUseCase = obterClienteUseCase;
        _alterarClienteUseCase = alterarClienteUseCase;
        _deletarClienteUseCase = deletarClienteUseCase;
    }

    /// <summary>Lista todos os clientes ativos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarClientesUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Cadastra um novo cliente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] 
    public async Task<IActionResult> Cadastrar([FromBody] CriarClienteRequest request)
    {
        var validator = new CriarClienteValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarClienteUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Obtém um cliente pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] 
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterClienteUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Altera os dados de um cliente existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarClienteRequest request)
    {
        var validator = new AtualizarClienteValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarClienteUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Desativa um cliente (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(Guid id)
    {
        try
        {
            await _deletarClienteUseCase.ExecutarAsync(id);
            return NoContent();
        }
        catch (ClienteNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}