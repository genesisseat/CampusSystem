@page "{id?}"
@model FinanceMain.Pages.InvoiceModel
@{
    ViewData["Title"] = $"Invoice {Model.Invoice.InvoiceNumber}";
}

<div class="page-heading d-flex justify-content-between align-items-start mb-4">
    <div>
        <span class="eyebrow text-uppercase text-muted fw-semibold">Invoice detail</span>
        <h1 class="h2">Invoice @Model.Invoice.InvoiceNumber</h1>
        <p class="text-muted">@Model.Invoice.Term · Issued @Model.Invoice.IssueDate.ToString("MMM dd, yyyy")</p>
    </div>
    <div>
        <button class="btn btn-outline-dark me-2">Download PDF</button>
        <a asp-page="/Billing" class="btn btn-secondary">Back to Billing</a>
    </div>
</div>

<div class="surface p-4 border rounded shadow-sm bg-white mb-4">
    <div class="row mb-4">
        <div class="col-sm-6">
            <span class="text-muted d-block small fw-semibold">Status</span>
            <span class="badge @(Model.Invoice.IsPaid ? "text-bg-success" : "text-bg-warning")">
                @(Model.Invoice.IsPaid ? "Paid" : "Unpaid")
            </span>
        </div>
        <div class="col-sm-6 text-sm-end">
            <span class="text-muted d-block small fw-semibold">Due Date</span>
            <span class="fw-semibold">@Model.Invoice.DueDate.ToString("MMM dd, yyyy")</span>
        </div>
    </div>

    <div class="table-responsive">
        <table class="table align-middle mb-0">
            <thead class="table-light">
                <tr>
                    <th>Line Item</th>
                    <th class="text-center">Category</th>
                    <th class="text-end">Amount</th>
                </tr>
            </thead>
            <tbody>
                @if (Model.Invoice.LineItems.Any())
                {
                    @foreach (var item in Model.Invoice.LineItems)
                    {
                        <tr>
                            <td>
                                <div class="fw-semibold">@item.Description</div>
                                @if (!string.IsNullOrEmpty(item.Notes))
                                {
                                    <small class="text-muted">@item.Notes</small>
                                }
                            </td>
                            <td class="text-center">
                                <span class="badge text-bg-light border">@item.Category</span>
                            </td>
                            <td class="text-end fw-semibold">@item.Amount.ToString("C")</td>
                        </tr>
                    }
                }
                else
                {
                    <tr>
                        <td colspan="3" class="text-center py-4 text-muted">No line items loaded.</td>
                    </tr>
                }
            </tbody>
            <tfoot class="table-light">
                <tr>
                    <th colspan="2" class="text-end">Total Amount Due</th>
                    <th class="text-end text-primary h5 mb-0">@Model.Invoice.TotalAmount.ToString("C")</th>
                </tr>
            </tfoot>
        </table>
    </div>
</div>
