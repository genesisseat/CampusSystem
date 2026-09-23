@page
@page
@{
    ViewData["Title"] = "Make a payment";
}

<div class="page-heading">
    <div>
        <span class="eyebrow">Payment center</span>
        <h1>Make a payment</h1>
        <p class="text-muted">This form is a preview and does not process payments.</p>
    </div>
</div>

<div class="surface p-4" style="max-width: 640px">
    <form>
        <label class="form-label" for="accountNumber">Account or Reference No.</label>
        <div class="input-group mb-3">
            <span class="input-group-text"><i class="bi bi-person"></i></span>
            <input id="accountNumber" class="form-control" type="text" placeholder="Enter account or reference number" />
        </div>

        <label class="form-label" for="amount">Amount</label>
        <div class="input-group mb-3">
            <span class="input-group-text">$</span>
            <input id="amount" class="form-control" type="number" step="0.01" placeholder="0.00" />
        </div>

        <label class="form-label" for="paymentMethod">Payment method</label>
        <select id="paymentMethod" class="form-select mb-3">
            <option selected disabled>Select a method</option>
            <option value="bank">Bank transfer</option>
            <option value="card">Card</option>
            <option value="paypal">PayPal</option>
            <option value="crypto">Cryptocurrency</option>
        </select>

        <label class="form-label" for="paymentNotes">Notes (Optional)</label>
        <div class="mb-3">
            <textarea id="paymentNotes" class="form-control" rows="2" placeholder="Add a note or description..."></textarea>
        </div>

        <div class="d-flex justify-content-between align-items-center">
            <button class="btn btn-outline-secondary" type="reset">Clear</button>
            <button class="btn btn-dark" type="button">Continue</button>
        </div>
    </form>
</div>
