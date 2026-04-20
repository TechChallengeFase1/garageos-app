using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Application.Validators.Veiculos;

namespace GarageOS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VeiculosController : ControllerBase
{
    private readonly CriarVeiculoValidator _validator;
    private readonly ListarVeiculosUseCase _listarVeiculosUseCase;
    private readonly ObterVeiculoUseCase _obterVeiculoUseCase;
    private readonly CadastrarVeiculoUseCase _cadastrarVeiculoUseCase; 
    private readonly AlterarVeiculoUseCase _alterarVeiculoUseCase;
    //private readonly VincularVeiculoClienteUseCase _vincularVeiculoClienteUseCase;

    public VeiculosController(
        CriarVeiculoValidator validator,
        ListarVeiculosUseCase listarVeiculosUseCase,
        ObterVeiculoUseCase obterVeiculoUseCase,
        CadastrarVeiculoUseCase cadastrarVeiculoUseCase,
        AlterarVeiculoUseCase alterarVeiculoUseCase
        //VincularVeiculoClienteUseCase vincularVeiculoClienteUseCase
        )
    {
        _validator = validator;
        _listarVeiculosUseCase = listarVeiculosUseCase;
        _obterVeiculoUseCase = obterVeiculoUseCase;
        _cadastrarVeiculoUseCase = cadastrarVeiculoUseCase;
        _alterarVeiculoUseCase = alterarVeiculoUseCase;
        //_vincularVeiculoClienteUseCase = vincularVeiculoClienteUseCase;
    }

    /// <summary>Lista todos os veículos </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VeiculoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarVeiculosUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Obtém um veículo pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterVeiculoUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Cadastra um novo veículo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastrar([FromBody] CriarVeiculoRequest request)
    {
        var validator = new CriarVeiculoValidator();
        var validation = await validator.ValidateAsync(request);

         if (!validation.IsValid)
             return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarVeiculoUseCase.ExecutarAsyncCadastrarVeiculo(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Altera os dados de um veículo.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarVeiculoRequest request)
    {
        try
        {
            var resultado = await _alterarVeiculoUseCase.ExecutarAsyncAlterarVeiculo(id, request);
            return Ok(resultado);
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Vincula o veículo a um cliente.</summary>
    // [HttpPatch("{id:guid}/vincular-cliente")]
    // [ProducesResponseType(StatusCodes.Status204NoContent)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // public async Task<IActionResult> VincularAoCliente(Guid id, [FromBody] VincularClienteRequest request)
    // {
    //     try
    //     {
    //         await _vincularVeiculoClienteUseCase.ExecutarAsync(id, request.ClienteId);
    //         return NoContent();
    //     }
    //     catch (VeiculoNaoEncontradoException ex)
    //     {
    //         return NotFound(new { mensagem = ex.Message });
    //     }
    // }
}