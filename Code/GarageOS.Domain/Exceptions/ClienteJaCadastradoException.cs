using System;
using System.Collections.Generic;
using System.Text;

namespace GarageOS.Domain.Exceptions
{
    public class ClienteJaCadastradoException : Exception
    {
        public ClienteJaCadastradoException(string campo, string valor)
        : base($"Já existe um cliente cadastrado com o {campo}: {valor}.") { }
    }
}
