namespace APBD_TASK7.DTOs;

public class ErrorResponseDTO
{
    public string Message { get; set; } = string.Empty;

    public ErrorResponseDTO() { }

    public ErrorResponseDTO(string message)
    {
        Message = message;
    }
}