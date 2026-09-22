@page
@model FinanceMain.Pages.BillingModel
@{
    ViewData["Title"] = "Billing & Payments";
}

<div class="page-heading mb-4">
    <div>
        <span class="eyebrow text-uppercase text-muted fw-semibold">Financial Services</span>
        <h1 class="h2">Billing & Statements</h1>
        <p class="text-muted">Manage tuition fees, view account statements, and process payments.</p>
    </div>
</div>

<!-- Account Summary Cards -->
<div class="row g-3 mb-4">
    <div class="col-md-4">
        <div class="surface p-4 border rounded shadow-sm bg-white">
            <small class="text-muted d-block fw-semibold">Current Balance Due</small>
            <div class="h3 text-danger my-1">@Model.AccountSummary.CurrentBalance.ToString("C")</div>
            <small class="text-muted">Due Date: @Model.AccountSummary.DueDate.ToString("MMM dd, yyyy")</small>
        </div>
    </div>
    <div class="col-md-4">
        <div class="surface p-4 border rounded shadow-sm bg-white">
            <small class="text-muted d-block fw-semibold">Total Pending Credits / Aid</small>
            <div class="h3 text-success my-1">@Model.AccountSummary.PendingCredits.ToString("C")</div>
            <small class="text-muted">Applied to upcoming term</small>
        </div>
    </div>
    <div class="col-md-4">
        <div class="surface p-4 border rounded shadow-sm bg-white">
            <small class="text-muted d-block fw-semibold">Net Account Balance</small>
            <div class="h3 text-primary my-1">@Model.AccountSummary.NetBalance.ToString("C")</div>
            <small class="text-muted">Account Status: <span class="badge text-bg-success">Good Standing</span></small>
        </div>
    </div>
</div>

<!-- Statement \\\\\\\& Invoices -->
<div class="surface p-4 border rounded shadow-sm bg-white mb-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h2 class="h5 mb-0">Active Statement Breakdown</h2>
        <a href="#" class="btn btn-outline-secondary btn-sm">Download PDF Statement</a>
    </div>

    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
                <tr>
                    <th>Description</th>
                    <th>Term</th>
                    <th>Due Date</th>
                    <th class="text-end">Amount</th>
                    <th class="text-center">Status</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var line in Model.AccountSummary.StatementItems)
                {
                    <tr>
                        <td>
                            <div class="fw-semibold">@line.Description</div>
                            <small class="text-muted">Ref: @line.ReferenceNumber</small>
                        </td>
                        <td>@line.Term</td>
                        <td>@line.DueDate.ToString("MMM dd, yyyy")</td>
                        <td class="text-end fw-semibold">@line.Amount.ToString("C")</td>
                        <td class="text-center">
                            @if (line.IsPaid)
                            {
                                <span class="badge text-bg-success">Paid</span>
                            }
                            else
                            {
                                <span class="badge text-bg-warning">Unpaid</span>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>

<!-- Transaction History -->
<div class="surface p-4 border rounded shadow-sm bg-white">
    <h2 class="h5 mb-3">Recent Transactions</h2>
    <div class="table-responsive">
        <table class="table table-sm table-striped align-middle mb-0">
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Transaction Details</th>
                    <th>Method</th>
                    <th class="text-end">Amount Paid</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var tx in Model.AccountSummary.RecentTransactions)
                {
                    <tr>
                        <td>@tx.Date.ToString("yyyy-MM-dd")</td>
                        <td>@tx.Description</td>
                        <td>@tx.PaymentMethod</td>
                        <td class="text-end text-success fw-semibold">-@tx.Amount.ToString("C")</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
