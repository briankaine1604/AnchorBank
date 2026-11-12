using System;
using System.Collections.Generic;
using System.Text;
using AnchorBank.Application.Customers.DTOs;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<Customer> CreateAsync(CreateCustomerRequest request);

        Task<Customer?> GetByIdAsync(Guid id);
    }
}
