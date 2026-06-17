using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vertex.Entities.AuditLogs;
using Vertex.Entities.Notifications;
using Vertex.Repositories;

namespace Vertex_web_BE.Controllers
{
    [ApiController]
    [Route("api/orgs/{orgId:guid}/billing")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BillingController(AppDbContext context)
        {
            _context = context;
        }

        private Guid GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("Invalid access token.");
            return userId;
        }

        /// <summary>
        /// Khởi tạo phiên thanh toán giả lập với gói và chu kỳ đã chọn.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(Guid orgId, [FromBody] CheckoutRequest request)
        {
            try
            {
                var userId = GetUserId();

                // Kiểm tra quyền: Phải là owner hoặc admin của tổ chức
                var member = await _context.OrganizationMembers
                    .FirstOrDefaultAsync(m => m.OrgId == orgId && m.UserId == userId);

                if (member == null)
                    return StatusCode(403, new { message = "Bạn không phải thành viên của tổ chức này." });

                if (member.Role != "owner" && member.Role != "admin")
                    return StatusCode(403, new { message = "Chỉ có Chủ sở hữu hoặc Quản trị viên của tổ chức mới được phép nâng cấp gói." });

                var planLower = request.Plan.ToLower().Trim();
                if (planLower != "pro" && planLower != "business")
                    return BadRequest(new { message = "Gói nâng cấp không hợp lệ. Chỉ hỗ trợ gói Pro hoặc Business." });

                // Tính toán số tiền theo Việt Nam Đồng (VND)
                long amount = 0;
                if (planLower == "pro")
                {
                    amount = request.BillingCycle == "yearly" ? 948000 : 99000;
                }
                else if (planLower == "business")
                {
                    amount = request.BillingCycle == "yearly" ? 2388000 : 249000;
                }

                var transactionId = "VTX_" + DateTimeOffset.UtcNow.Ticks;

                return Ok(new
                {
                    TransactionId = transactionId,
                    Amount = amount,
                    Plan = planLower,
                    BillingCycle = request.BillingCycle,
                    Message = "Khởi tạo phiên thanh toán thành công."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xác nhận thanh toán giả lập thành công, thực hiện nâng cấp các giới hạn thực tế trong database.
        /// </summary>
        [HttpPost("simulate-success")]
        public async Task<IActionResult> SimulateSuccess(Guid orgId, [FromBody] SimulateSuccessRequest request)
        {
            try
            {
                var userId = GetUserId();

                // Kiểm tra quyền
                var member = await _context.OrganizationMembers
                    .FirstOrDefaultAsync(m => m.OrgId == orgId && m.UserId == userId);

                if (member == null)
                    return StatusCode(403, new { message = "Bạn không phải thành viên của tổ chức này." });

                if (member.Role != "owner" && member.Role != "admin")
                    return StatusCode(403, new { message = "Chỉ có Chủ sở hữu hoặc Quản trị viên của tổ chức mới được phép nâng cấp gói." });

                var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
                if (org == null)
                    return NotFound(new { message = "Tổ chức không tìm thấy." });

                var planLower = request.Plan.ToLower().Trim();
                if (planLower != "pro" && planLower != "business")
                    return BadRequest(new { message = "Gói nâng cấp không hợp lệ." });

                // Cập nhật cấu hình gói mới
                org.Plan = planLower;
                org.UpdatedAt = DateTimeOffset.UtcNow;

                if (planLower == "pro")
                {
                    org.MaxMembers = 20;
                    org.MaxProjects = 15;
                    org.AiQuota = 200;
                    org.StorageLimit = 10737418240; // 10 GB
                }
                else if (planLower == "business")
                {
                    org.MaxMembers = 200;
                    org.MaxProjects = 100;
                    org.AiQuota = 1000;
                    org.StorageLimit = 53687091200; // 50 GB
                }

                // Ghi nhận lịch sử quản lý (Audit Log)
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    AdminId = userId,
                    Action = "change_price", // Dùng mã hành động có sẵn trong DB Schema
                    TargetUserId = null,
                    Detail = $"Nâng cấp tổ chức '{org.Name}' lên gói {planLower.ToUpper()} qua mã VietQR: {request.TransactionId}.",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _context.AuditLogs.Add(auditLog);

                // Gửi thông báo đến toàn bộ thành viên trong tổ chức
                var members = await _context.OrganizationMembers
                    .Where(m => m.OrgId == orgId)
                    .ToListAsync();

                foreach (var m in members)
                {
                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = m.UserId,
                        Type = "info",
                        Message = $"Tổ chức '{org.Name}' đã được nâng cấp lên gói {planLower.ToUpper()} thành công! 🌟 Các giới hạn thành viên và bộ nhớ mới đã được kích hoạt.",
                        IsRead = false,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = $"Nâng cấp tổ chức lên gói {planLower.ToUpper()} thành công!",
                    Org = new
                    {
                        org.Id,
                        org.Name,
                        org.Plan,
                        org.MaxMembers,
                        org.MaxProjects,
                        org.AiQuota,
                        org.StorageLimit
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class CheckoutRequest
    {
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
    }

    public class SimulateSuccessRequest
    {
        public string Plan { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
    }
}
