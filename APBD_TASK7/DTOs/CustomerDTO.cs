namespace APBD_TASK7.DTOs;

public class CustomerDTO
{
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;

    public List<RentalDTO> Rentals { get; set; } = new();
}