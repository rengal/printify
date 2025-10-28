namespace Printify.Application.Exceptions;

public class BadRequestException(string msg) : Exception(msg);
