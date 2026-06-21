using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/orgs/{orgId:guid}/billing")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;

        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(Guid orgId, [FromBody] CheckoutRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _billingService.CreateCheckoutAsync(
                    orgId,
                    userId,
                    new CreateBillingCheckoutInput(request.Plan, request.BillingCycle)
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("transactions/{transactionId:guid}")]
        public async Task<IActionResult> GetTransaction(Guid orgId, Guid transactionId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _billingService.GetTransactionAsync(orgId, userId, transactionId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("~/api/billing/payos/webhook")]
        public async Task<IActionResult> PayOSWebhook([FromBody] PayOSWebhookRequest request)
        {
            try
            {
                await _billingService.HandlePayOSWebhookAsync(request);
                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private Guid GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("Invalid access token.");
            return userId;
        }
    }

    public class CheckoutRequest
    {
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
    }
}
