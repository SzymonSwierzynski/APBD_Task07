namespace APBD_TASK7.DTOs;

public class RentalDTO
{
    public int Id { get; set; }
    
    public DateTime RentalDate { get; set; }
    
    public DateTime? ReturnDate { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public List<MovieDTO> Movies { get; set; } = new();
}