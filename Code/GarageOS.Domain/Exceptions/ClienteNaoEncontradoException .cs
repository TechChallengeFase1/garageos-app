namespace GarageOS.Domain.Exceptions
{
    public class ClienteNaoEncontradoException : Exception
    {
        public ClienteNaoEncontradoException(Guid id)
            : base($"Cliente com ID {id} não foi encontrado.") { }
    }
}
