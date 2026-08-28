namespace GarageOS.Application.Abstractions;

/// <summary>
/// Publica eventos de negócio para a ferramenta de observabilidade.
/// </summary>
/// <remarks>
/// Existe como interface na camada de Application para que os use cases não
/// conheçam o fornecedor. A implementação com New Relic vive na Infrastructure;
/// trocar de ferramenta não toca em regra de negócio.
///
/// Estes eventos não são deriváveis de latência de requisição: quanto tempo uma
/// OS permaneceu em Diagnóstico é dado do domínio, e só a aplicação sabe emitir.
/// </remarks>
public interface IMetricasDeNegocio
{
    /// <summary>Registra a abertura de uma ordem de serviço</summary>
    void OrdemDeServicoCriada(string numeroOS, Guid clienteId);

    /// <summary>Registra a transição de status e quanto tempo durou o anterior</summary>
    void StatusAlterado(string numeroOS, string statusAnterior, string statusNovo, double minutosNoStatusAnterior);

    /// <summary>Registra uma falha no processamento de uma ordem de serviço</summary>
    void FalhaNoProcessamento(string numeroOS, string motivo);
}
