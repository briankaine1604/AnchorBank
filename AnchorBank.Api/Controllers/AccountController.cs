using AnchorBank.Api.Extensions;
using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Airtime;
using AnchorBank.Application.Airtime.DTOs;
using AnchorBank.Application.Airtime.Models;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Application.Transfers.DTOs;
using AnchorBank.Application.Transfers.Models;
using AnchorBank.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnchorBank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITransferService _transferService;

    private readonly IAirtimeService _airtimeService;

    public AccountController(IAccountService accountService, ITransferService transferService, IAirtimeService airtimeService)
    {
        _accountService = accountService;
        _transferService = transferService;
        _airtimeService = airtimeService;
        
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        if (!Enum.IsDefined(request.AccountType))
            return BadRequest("Account type is not supported.");

        var account = await _accountService.CreateAsync(customerId.Value, request);

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
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var account = await _accountService.GetByIdAsync(id, customerId.Value);

        if (account is null)
            return NotFound();

        return Ok(account);
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var accounts = await _accountService.GetByCustomerIdAsync(customerId.Value);

        return Ok(accounts);
    }

    [HttpPost("{accountId:guid}/deposit")]
    public async Task<IActionResult> Deposit(
        Guid accountId,
        [FromBody] AccountMovementRequest request)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var result = await _accountService.DepositAsync(accountId, customerId.Value, request);

        return ToResult(result);
    }

    [HttpPost("{accountId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(
        Guid accountId,
        [FromBody] AccountMovementRequest request)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var result = await _accountService.WithdrawAsync(accountId, customerId.Value, request);

        return ToResult(result);
    }

    [HttpGet("{accountId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var result = await _accountService.GetTransactionsAsync(accountId, customerId.Value, page, pageSize);

        if (result is null)
            return NotFound("Account not found.");

        return Ok(result);
    }

    [HttpGet("{accountId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid accountId)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var balance = await _accountService.GetBalanceAsync(accountId, customerId.Value);

        if (balance is null)
            return NotFound("Account not found.");

        return Ok(balance);
    }

    [HttpPost("{sourceAccountId:guid}/transfers")]
    public async Task<IActionResult> Transfer(
        Guid sourceAccountId,
        [FromBody] TransferRequest request)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var result = await _transferService.TransferAsync(sourceAccountId, customerId.Value, request);

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

    [HttpPost("{accountId:guid}/airtime")]
    public async Task<IActionResult> BuyAirtime(Guid accountId, [FromBody] BuyAirtimeRequest request)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
            return Unauthorized();

        var result = await _airtimeService.BuyAsync(accountId, customerId.Value, request);

        return result.Outcome switch
        {
            BuyAirtimeOutcome.Success => result.Status switch
            {
                AirtimePurchaseStatus.Success => Ok(new
                { purchaseId = result.PurchaseId, status = "Success", balance = result.Balance }),
                AirtimePurchaseStatus.Refunded => UnprocessableEntity(new
                {
                    purchaseId = result.PurchaseId,
                    status = "Refunded",
                    balance = result.Balance,
                    message = "The purchase failed and your money has been refunded."
                }),
                _ => Accepted(new
                {
                    purchaseId = result.PurchaseId,
                    status = "Processing",
                    balance = result.Balance,
                    message = "Your purchase is being processed."
                })
            },
            BuyAirtimeOutcome.AccountNotFound => NotFound("Account not found."),
            BuyAirtimeOutcome.AccountInactive => Conflict("Account is not active."),
            BuyAirtimeOutcome.InsufficientFunds => Conflict("Insufficient funds."),
            BuyAirtimeOutcome.InvalidPhoneNumber => BadRequest("Phone number must be 11 digits starting with 0."),
            BuyAirtimeOutcome.InvalidNetwork => BadRequest("Network is not supported."),
            _ => BadRequest("Amount must be greater than zero.")
        };
    }
}
