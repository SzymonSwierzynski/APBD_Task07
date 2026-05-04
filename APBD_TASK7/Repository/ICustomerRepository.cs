using APBD_TASK7.DTOs;

namespace APBD_TASK7.Repository;

public interface ICustomerRepository
{
    Task<CustomerDTO?> GetCustomerWithRentalAsync(int customerId);

    Task AddRentalAsync(int customerId, CreateRentalDTO dto);
}