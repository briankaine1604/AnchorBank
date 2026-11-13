using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Application.Transfers.DTOs;
using AnchorBank.Application.Transfers.Models;
using Microsoft.AspNetCore.Mvc;

namespace AnchorBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITransferService _transferService;

    public AccountController(IAccountService accountService, ITransferService transferService)
    {
        _accountService = accountService;
        _transferService = transferService;
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

    [HttpPost("{accountId:guid}/deposit")]
    public async Task<IActionResult> Deposit(
        Guid accountId,
        [FromBody] AccountMovementRequest request)
    {
        var result = await _accountService.DepositAsync(accountId, request);

        return ToResult(result);
    }

    [HttpPost("{accountId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(
        Guid accountId,
        [FromBody] AccountMovementRequest request)
    {
        var result = await _accountService.WithdrawAsync(accountId, request);

        return ToResult(result);
    }

    [HttpGet("{accountId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _accountService.GetTransactionsAsync(accountId, page, pageSize);

        if (result is null)
            return NotFound("Account not found.");

        return Ok(result);
    }

    [HttpGet("{accountId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid accountId)
    {
        var balance = await _accountService.GetBalanceAsync(accountId);

        if (balance is null)
            return NotFound("Account not found.");

        return Ok(balance);
    }

    [HttpPost("{sourceAccountId:guid}/transfers")]
    public async Task<IActionResult> Transfer(
        Guid sourceAccountId,
        [FromBody] TransferRequest request)
    {
        var result = await _transferService.TransferAsync(sourceAccountId, request);

        return result.Outcome switch
        {
            TransferOutcome.Success => Ok(new
            {
                transferId = result.TransferId,
                sourceBalance = result.SourceBalance,
                destinationBalance = result.DestinationBalance
            }),
            TransferOutcome.SourceAccountNotFound => NotFound("Source account not found."),
            TransferOutcome.DestinationAccountNotFound => NotFound("Destination account not found."),
            TransferOutcome.SameAccount => BadRequest("Source and destination accounts must differ."),
            TransferOutcome.AccountInactive => Conflict("Account is not active."),
            TransferOutcome.InsufficientFunds => Conflict("Insufficient funds."),
            _ => BadRequest("Amount must be greater than zero.")
        };
    }

    private IActionResult ToResult(AccountMovementResult result)
    {
        return result.Outcome switch
        {
            AccountMovementOutcome.Success => Ok(new { balance = result.Balance }),
            AccountMovementOutcome.AccountNotFound => NotFound("Account not found."),
            AccountMovementOutcome.AccountInactive => Conflict("Account is not active."),
            AccountMovementOutcome.InsufficientFunds => Conflict("Insufficient funds."),
            _ => BadRequest("Amount must be greater than zero.")
        };
    }
}
