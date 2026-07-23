using Fundo.Domain.Enums;

namespace Fundo.Domain.Entities;

public class Loan
{
    public Guid Id { get; private set; }
    public string ApplicantName { get; private set; } = null!;
    public decimal AmountRequested { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public LoanStatus Status { get; private set; }

    private Loan() { }

    public Loan(string applicantName, decimal amountRequested)
    {
        if (string.IsNullOrWhiteSpace(applicantName))
            throw new ArgumentException("Applicant name is required.", nameof(applicantName));
        if (amountRequested <= 0)
            throw new ArgumentException("Amount requested must be greater than zero.", nameof(amountRequested));

        Id = Guid.NewGuid();
        ApplicantName = applicantName;
        AmountRequested = amountRequested;
        CurrentBalance = amountRequested;
        Status = LoanStatus.Active;
    }

    public void RegisterPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));
        if (Status == LoanStatus.Paid)
            throw new InvalidOperationException("Cannot register a payment on a loan that is already paid.");
        if (amount > CurrentBalance)
            throw new InvalidOperationException("Payment amount cannot exceed the current balance.");

        CurrentBalance -= amount;
        if (CurrentBalance == 0)
            Status = LoanStatus.Paid;
    }
}
