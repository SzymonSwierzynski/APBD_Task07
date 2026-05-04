using APBD_TASK7.DTOs;
using APBD_TASK7.Exceptions;
using Microsoft.Data.SqlClient;

namespace APBD_TASK7.Repository.Impl;

public class CustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")!;
    }


    public async Task<CustomerDTO?> GetCustomerWithRentalAsync(int customerId)
    {
        const string sql = """
                           SELECT 
                                c.first_name,
                                c.last_name,
                                r.rental_id,
                                r.rental_date,
                                r.return_date,
                                s.name AS status,
                                m.title,
                                ri.price_at_rental
                           FROM Customer c
                           JOIN Rental r ON c.customer_id = r.customer_id
                           JOIN Status s ON r.status_id = s.status_id
                           JOIN Rental_Item ri ON ri.rental_id = r.rental_id
                           JOIN Movie m ON m.movie_id = ri.movie_id
                           WHERE c.customer_id = @customer_id
                           ORDER BY r.rental_id
                           """;
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@customer_id", customerId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            return null;
        }

        CustomerDTO? customer = null;
        var rentalDict = new Dictionary<int, RentalDTO>();

        while (await reader.ReadAsync())
        {
            if (customer == null)
            {
                customer = new CustomerDTO
                {
                    FirstName = reader.GetString(0),
                    LastName = reader.GetString(1),
                    Rentals = new List<RentalDTO>()
                };
            }

            var rentalId = reader.GetInt32(2);
            if (!rentalDict.ContainsKey(rentalId))
            {
                var rental = new RentalDTO
                {
                    Id = rentalId,
                    RentalDate = reader.GetDateTime(3),
                    ReturnDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    Status = reader.GetString(5),
                    Movies = new List<MovieDTO>()
                };
                rentalDict[rentalId] = rental;
                customer.Rentals.Add(rental);
            }

            var movie = new MovieDTO
            {
                Title = reader.GetString(6),
                PriceAtRental = reader.GetDecimal(7)
            };
            rentalDict[rentalId].Movies.Add(movie);
        }

        return customer;
    }

    public async Task AddRentalAsync(int customerId, CreateRentalDTO dto)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

        try
        {
            const string customerCheck = "SELECT COUNT(1) FROM Customer WHERE customer_id = @customer_id";
            await using var customerCmd = new SqlCommand(customerCheck, conn, transaction);
            customerCmd.Parameters.AddWithValue("@customer_id", customerId);
            var customerCount = (int?)await customerCmd.ExecuteScalarAsync()!;

            if (customerCount == 0)
                throw new NotFoundException($"Customer with ID {customerId} was not found.");

            var resolvedMovies = new List<(int MovieId, decimal Price)>();
            foreach (var movie in dto.Movies)
            {
                const string movieSql = """
                                        SELECT movie_id 
                                        FROM Movie
                                        WHERE title = @title
                                        """;
                await using var movieCmd = new SqlCommand(movieSql, conn, transaction);
                movieCmd.Parameters.AddWithValue("@title", movie.Title);
                var movieId = await movieCmd.ExecuteScalarAsync();

                if (movieId == null)
                {
                    throw new NotFoundException($"Movie {movie.Title} not found");
                }

                resolvedMovies.Add(((int)movieId, movie.RentalPrice));
            }

            const string statusSql = """
                                     SELECT status_id
                                     FROM Status
                                     WHERE name = 'Rented'
                                     """;
            await using var statusCmd = new SqlCommand(statusSql, conn, transaction);
            var statusId = (int?)await statusCmd.ExecuteScalarAsync();

            const string insertRentalSql = """
                                           INSERT INTO Rental (rental_date, return_date, customer_id, status_id)
                                           OUTPUT INSERTED.rental_id
                                           VALUES  (@rental_date, NULL, @customer_id, @status_id)
                                           """;
            await using var rentalCmd = new SqlCommand(insertRentalSql, conn, transaction);
            rentalCmd.Parameters.AddWithValue("@rental_date", dto.RentalDate);
            rentalCmd.Parameters.AddWithValue("@customer_id", customerId);
            rentalCmd.Parameters.AddWithValue("@status_id", statusId);
            var newRentalId = (int?)await rentalCmd.ExecuteScalarAsync();

            foreach (var (movieId, price) in resolvedMovies)
            {
                const string insertItemSql = """
                                             INSERT INTO Rental_Item (rental_id, movie_id, price_at_rental)
                                             VALUES (@rental_id, @movie_id, @price)
                                             """;
                await using var itemCmd = new SqlCommand(insertItemSql, conn, transaction);
                itemCmd.Parameters.AddWithValue("@rental_id", newRentalId);
                itemCmd.Parameters.AddWithValue("@movie_id", movieId);
                itemCmd.Parameters.AddWithValue("@price", price);
                await itemCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}