namespace APBD_TASK7.DTOs;

public class CreateRentalDTO
{
    public DateTime RentalDate { get; set; }

    public List<CreateMovieDTO> Movies { get; set; } = new();
}