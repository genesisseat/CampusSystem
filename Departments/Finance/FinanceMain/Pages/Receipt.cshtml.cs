using FinanceMain.Contracts;
using FinanceMain.Contracts.Dtos;
using FinanceMain.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinanceMain.Pages;

public class ReceiptModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly IAssessmentService _assessmentService;
    private readonly CurrentUserContext _currentUser;

    public ReceiptModel(
        IPaymentService paymentService,
        IAssessmentService assessmentService,
        CurrentUserContext currentUser)
    {
        _paymentService = paymentService;
        _assessmentService = assessmentService;
        _currentUser = currentUser;
    }

    public PaymentRowDto? Payment { get; private set; }
    public FeeAssessmentDto? Assessment { get; private set; }
    new public bool NotFound { get; private set; }

    public async Task OnGetAsync(string? paymentNumber)
    {
        if (!string.IsNullOrWhiteSpace(paymentNumber))
        {
            Payment = await _paymentService.GetPaymentByNumberAsync(paymentNumber.Trim());
        }
        else
        {
            var studentId = _currentUser.GetCurrentStudentId();
            var history = await _paymentService.GetPaymentHistoryForStudentAsync(studentId);
            var latest = history.FirstOrDefault();
            if (latest != null)
            {
                Payment = await _paymentService.GetPaymentByNumberAsync(latest.PaymentNumber);
            }
        }

        if (Payment != null)
        {
            Assessment = await _assessmentService.GetByIdAsync(Payment.FeeAssessmentId);
        }

        NotFound = Payment == null;
    }
}
