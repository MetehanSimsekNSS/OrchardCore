using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Email.Services;
using OrchardCore.Email.Smtp.Drivers;
using OrchardCore.Email.Smtp.Services;
using OrchardCore.Email.Smtp.ViewModels;

namespace OrchardCore.Email.Smtp.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly INotifier _notifier;
        private readonly IEmailService _emailService;
        protected readonly IHtmlLocalizer H;

        public AdminController(
            IHtmlLocalizer<AdminController> h,
            IAuthorizationService authorizationService,
            INotifier notifier,
            SmtpEmailService smtpEmailService)
        {
            H = h;
            _authorizationService = authorizationService;
            _notifier = notifier;
            _emailService = smtpEmailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSmtpEmailSettings))
            {
                return Forbid();
            }

            return View();
        }

        [HttpPost, ActionName(nameof(Index))]
        public async Task<IActionResult> IndexPost(SmtpEmailSettingsViewModel model)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ManageSmtpEmailSettings))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var message = CreateMessageFromViewModel(model);

                var result = await _emailService.SendAsync(message);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("*", error.ToString());
                    }
                }
                else
                {
                    await _notifier.SuccessAsync(H["Message sent successfully."]);

                    return Redirect(Url.Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = SmtpSettingsDisplayDriver.GroupId }));
                }
            }

            return View(model);
        }

        private static MailMessage CreateMessageFromViewModel(SmtpEmailSettingsViewModel testSettings)
        {
            var message = new MailMessage
            {
                To = testSettings.To,
                Bcc = testSettings.Bcc,
                Cc = testSettings.Cc,
                ReplyTo = testSettings.ReplyTo
            };

            if (!string.IsNullOrWhiteSpace(testSettings.Sender))
            {
                message.Sender = testSettings.Sender;
            }

            if (!string.IsNullOrWhiteSpace(testSettings.Subject))
            {
                message.Subject = testSettings.Subject;
            }

            if (!string.IsNullOrWhiteSpace(testSettings.Body))
            {
                message.Body = testSettings.Body;
            }

            return message;
        }
    }
}