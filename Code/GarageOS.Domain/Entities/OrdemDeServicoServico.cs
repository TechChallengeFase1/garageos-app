using GarageOS.Domain.Enums;
using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

public class OrdemDeServicoServico
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public Guid ServicoId { get; private set; }
    public StatusExecucaoServico Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? IniciadaEm { get; private set; }
    public DateTime? FinalizadaEm { get; private set; }
    public Servico? Servico { get; private set; } // NOSONAR - EF Core usa via reflection

    protected OrdemDeServicoServico() { }

    public OrdemDeServicoServico(Guid ordemDeServicoId, Guid servicoId)
    {
        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        ServicoId = servicoId;
        Status = StatusExecucaoServico.Criada;
        CriadoEm = BrasiliaTime.Agora;
    }

    public void IniciarExecucao()
    {
        if (IniciadaEm.HasValue)
            throw new ArgumentException("Execução já foi iniciada.");

        Status = StatusExecucaoServico.Iniciado;
        IniciadaEm = BrasiliaTime.Agora;
    }

    public void FinalizarExecucao()
    {
        if (!IniciadaEm.HasValue)
            throw new ArgumentException("Execução não foi iniciada.");

        if (FinalizadaEm.HasValue)
            throw new ArgumentException("Execução já foi finalizada.");

        Status = StatusExecucaoServico.Finalizado;
        FinalizadaEm = BrasiliaTime.Agora;
    }
}
