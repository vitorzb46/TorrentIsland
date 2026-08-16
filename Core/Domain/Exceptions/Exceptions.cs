namespace Core.Domain.Exceptions
{
    public class InvalidMagnetLinkException : Exception
    {
        public InvalidMagnetLinkException() : base("A URL fornecida não é um Link Magnetico válido.") {}
    }
    public class ParamaterException : Exception
    {
        public ParamaterException() : base("Parâmetro fornecido não pode ser vazio.") {}
    }
    public class FolderException : Exception
    {
        public FolderException() : base("Pasta fornecida está vazia ou inexistente!") {}
    }
}
