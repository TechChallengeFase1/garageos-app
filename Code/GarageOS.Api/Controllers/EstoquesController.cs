using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Application.Validators.Estoques;
using GarageOS.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
/// <summary>Controller de gerenciamento de estoque</summary>
public class EstoquesController : ControllerBase
{
    private readonly ListarEstoquesUseCase _listarEstoquesUseCase;
    private readonly CadastrarEstoqueUseCase _cadastrarEstoqueUseCase;
    private readonly ObterEstoqueUseCase _obterEstoqueUseCase;
    private readonly AlterarEstoqueUseCase _alterarEstoqueUseCase;
    private readonly DeletarEstoqueUseCase _deletarEstoqueUseCase;

    /// <summary>Inicializa o controller com os use cases de estoque</summary>
    public EstoquesController(
        ListarEstoquesUseCase listarEstoquesUseCase,
        CadastrarEstoqueUseCase cadastrarEstoqueUseCase,
        ObterEstoqueUseCase obterEstoqueUseCase,
        AlterarEstoqueUseCase alterarEstoqueUseCase,
        DeletarEstoqueUseCase deletarEstoqueUseCase)
    {
        _listarEstoquesUseCase = listarEstoquesUseCase;
        _cadastrarEstoqueUseCase = cadastrarEstoqueUseCase;
        _obterEstoqueUseCase = obterEstoqueUseCase;
        _alterarEstoqueUseCase = alterarEstoqueUseCase;
        _deletarEstoqueUseCase = deletarEstoqueUseCase;
    }

    /// <summary>Lista todos os itens do estoque (peças e insumos)</summary>
    /// <remarks>
    /// Retorna uma lista de todos os itens de estoque cadastrados no sistema.
    /// Estoque inclui peças, insumos e materiais utilizados nas Ordens de Serviço.
    /// Cada item mostra:
    /// - Nome do item
    /// - Quantidade disponível
    /// - Valor unitário
    /// - Status (Disponível/Indisponível) - calculado automaticamente (quantidade > 0 = Disponível)
    /// - Fornecedor
    /// - Data de entrada e saída (se aplicável)
    /// </remarks>
    /// <returns>Lista completa de itens do estoque</returns>
    /// <response code="200">Lista retornada com sucesso</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EstoqueResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar()
    {
        var resultado = await _listarEstoquesUseCase.ExecutarAsync();
        return Ok(resultado);
    }

    /// <summary>Cadastra um novo item no estoque</summary>
    /// <remarks>
    /// Adiciona um novo item de estoque (peça, insumo ou material) ao sistema.
    /// O status do item é definido automaticamente:
    /// - Disponível: se quantidade > 0
    /// - Indisponível: se quantidade = 0
    ///
    /// Nota importante: A quantidade em estoque é DECREMENTADA quando:
    /// 1. Um orçamento é APROVADO em uma Ordem de Serviço
    /// 2. A OS está usando esse item
    ///
    /// Validações:
    /// - Nome: obrigatório, máximo 150 caracteres
    /// - Quantidade: obrigatório, maior ou igual a zero
    /// - Valor: obrigatório, maior que zero
    /// - DataEntrada: obrigatório
    /// - Fornecedor: obrigatório
    /// </remarks>
    /// <param name="request">Dados do novo item (Nome, Quantidade, Valor, DataEntrada, Fornecedor, DataSaida opcional)</param>
    /// <returns>Item de estoque criado com sucesso</returns>
    /// <response code="201">Item criado com sucesso</response>
    /// <response code="400">Dados inválidos (validação de campos falhou)</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPost]
    [ProducesResponseType(typeof(EstoqueResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cadastrar([FromBody] CriarEstoqueRequest request)
    {
        var validator = new CriarEstoqueValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var resultado = await _cadastrarEstoqueUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id = resultado.Id }, resultado);
    }

    /// <summary>Obtém os dados de um item específico do estoque</summary>
    /// <remarks>
    /// Retorna os detalhes completos de um item do estoque incluindo:
    /// - Nome e fornecedor
    /// - Quantidade atual disponível
    /// - Valor unitário
    /// - Status atual (Disponível/Indisponível)
    /// - Histórico de datas (entrada e saída se aplicável)
    /// </remarks>
    /// <param name="id">ID único do item de estoque (GUID)</param>
    /// <returns>Dados completos do item de estoque</returns>
    /// <response code="200">Item encontrado - dados retornados com sucesso</response>
    /// <response code="404">Item de estoque não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EstoqueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        try
        {
            var resultado = await _obterEstoqueUseCase.ExecutarAsync(id);
            return Ok(resultado);
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Remove permanentemente um item do estoque</summary>
    /// <remarks>
    /// Deleta um item de estoque do sistema (exclusão física/hard delete).
    /// Diferente de clientes e outros recursos que usam soft delete,
    /// o estoque é removido permanentemente.
    ///
    /// AVISO: Esta é uma operação irreversível.
    /// Verifique se o item não está vinculado a Ordens de Serviço ativas.
    /// </remarks>
    /// <param name="id">ID único do item a remover (GUID)</param>
    /// <returns>Nenhum conteúdo (operação bem-sucedida)</returns>
    /// <response code="204">Item removido com sucesso - sem conteúdo na resposta</response>
    /// <response code="404">Item de estoque não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(Guid id)
    {
        try
        {
            await _deletarEstoqueUseCase.ExecutarAsync(id);
            return NoContent();
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    /// <summary>Atualiza os dados de um item do estoque</summary>
    /// <remarks>
    /// Permite alterar informações de um item do estoque como nome, quantidade, valor, fornecedor, etc.
    ///
    /// O status do item é recalculado automaticamente após atualização:
    /// - Disponível: se quantidade > 0
    /// - Indisponível: se quantidade = 0
    ///
    /// IMPORTANTE: Alterações na quantidade NÃO afetam:
    /// - Ordens de Serviço já criadas com esse item
    /// - Quantidades já reservadas em OSs não aprovadas
    /// - O valor já calculado em orçamentos gerados
    /// </remarks>
    /// <param name="id">ID único do item a atualizar (GUID)</param>
    /// <param name="request">Novos dados do item (Nome, Quantidade, Valor, Fornecedor, etc)</param>
    /// <returns>Item de estoque atualizado com os novos dados</returns>
    /// <response code="200">Item atualizado com sucesso</response>
    /// <response code="400">Dados inválidos (validação de campos falhou)</response>
    /// <response code="404">Item de estoque não encontrado</response>
    /// <response code="401">Não autorizado - token JWT ausente ou inválido</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EstoqueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Alterar(Guid id, [FromBody] AtualizarEstoqueRequest request)
    {
        var validator = new AtualizarEstoqueValidator();
        var validation = await validator.ValidateAsync(request);

        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        try
        {
            var resultado = await _alterarEstoqueUseCase.ExecutarAsync(id, request);
            return Ok(resultado);
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
