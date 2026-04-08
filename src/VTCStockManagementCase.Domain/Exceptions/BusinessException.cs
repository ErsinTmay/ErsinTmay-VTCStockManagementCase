namespace VTCStockManagementCase.Domain.Exceptions;

public class BusinessException : Exception
{
    public string Code { get; }

    public BusinessException(string code, string message, object? details = null)
        : base(message)
    {
        Code = code;
        Details = details;
    }

    public object? Details { get; }
}
