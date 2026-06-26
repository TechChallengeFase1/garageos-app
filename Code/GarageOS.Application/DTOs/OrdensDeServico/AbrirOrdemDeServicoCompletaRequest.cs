namespace GarageOS.Application.DTOs.OrdensDeServico;

public class AbrirOrdemDeServicoCompletaRequest
{
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }
    public List<Guid> ServicosIds { get; set; } = new();
    public List<PecaRequest>? Pecas { get; set; }
}

public class PecaRequest
{
    public Guid EstoqueId { get; set; }
    public int Quantidade { get; set; }
}
