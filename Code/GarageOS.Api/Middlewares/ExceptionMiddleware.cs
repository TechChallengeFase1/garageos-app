using System.Net;
using System.Text.Json;
using GarageOS.Domain.Exceptions;

namespace GarageOS.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Erro: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (ServicoNaoEncontradoException ex)
        {
            _logger.LogWarning(ex, "Não encontrado: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ClienteNaoEncontradoException ex)
        {
            _logger.LogWarning(ex, "Não encontrado: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (EstoqueNaoEncontradoException ex)
        {
            _logger.LogWarning(ex, "Não encontrado: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ClienteJaCadastradoException ex)
        {
            _logger.LogWarning(ex, "Conflito: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (VeiculoNaoEncontradoException ex)
        {
            _logger.LogWarning(ex, "Não encontrado: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno. Tente novamente mais tarde.");
        }
    }

    private static async Task EscreverRespostaAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string mensagem)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var resposta = new
        {
            status = (int)statusCode,
            erro = mensagem,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(resposta, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}