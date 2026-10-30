namespace GarageOS.Domain.Exceptions;

/// <summary>A peça existe, mas não há quantidade disponível</summary>
public class EstoqueInsuficienteException : Exception
{
    public EstoqueInsuficienteException(string nome, int disponivel, int solicitado)
        : base($"Estoque insuficiente para '{nome}'. Disponível: {disponivel}, Solicitado: {solicitado}.") { }
}
