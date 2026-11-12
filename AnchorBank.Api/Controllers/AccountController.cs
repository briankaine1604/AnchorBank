using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnchorBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request)
    {
        var account = await _accountService.CreateAsync(request);

        if (account is null)
            return NotFound("Customer not found.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = account.Id },
            account);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);

        if (account is null)
            return NotFound();

        return Ok(account);
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId(Guid customerId)
    {
        var accounts = await _accountService.GetByCustomerIdAsync(customerId);

        return Ok(accounts);
    }
}
