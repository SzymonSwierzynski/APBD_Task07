using APBD_TASK7.DTOs;
using APBD_TASK7.Exceptions;
using APBD_TASK7.Service;
using Microsoft.AspNetCore.Mvc;

namespace APBD_TASK7.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;

    public CustomersController(CustomerService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}/rentals")]
    public async Task<IActionResult> GetCustomerRentalsById([FromRoute] int id)
    {
        var result = await _service.GetCustomerAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{id:int}/rentals")]
    public async Task<IActionResult> AddRental
    (
        [FromRoute] int id, 
        [FromBody] CreateRentalDTO dto
    )
    {
        if (dto.Movies == null || dto.Movies.Count == 0)
        {
            return BadRequest("At least one movie is required");
        }

        try
        {
            await _service.AddRentalAsync(id, dto);
            return Created($"api/customers/{id}/rentals", null);
        }
        catch (NotFoundException e)
        {
            return NotFound(new ErrorResponseDTO(e.Message));
        }
    }
    
    
}