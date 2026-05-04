using APBD_TASK7.DTOs;
using APBD_TASK7.Repository;

namespace APBD_TASK7.Service;

public class CustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerDTO> GetCustomerAsync(int id)
        => await _repository.GetCustomerWithRentalAsync(id);

    public async Task AddRentalAsync(int customerId, CreateRentalDTO dto)
        => await _repository.AddRentalAsync(customerId, dto);
}