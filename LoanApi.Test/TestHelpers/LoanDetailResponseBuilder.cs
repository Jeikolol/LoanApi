using Application.Models.Responses;
using Domain.Enums;

namespace LoanApi.Tests.TestHelpers;

public class LoanDetailResponseBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _loanNumber = "LOAN-001";
    private Guid _customerId = Guid.NewGuid();
    private string _customerName = "John Doe";
    private decimal _principalAmount = 10000;
    private decimal _interestRate = 5;
    private int _termMonths = 12;
    private decimal _installmentAmount = 850;
    private LoanStatus _loanStatus = LoanStatus.Active;
    private DateTime _disbursementDate = DateTime.UtcNow;
    private DateTime _maturityDate = DateTime.UtcNow.AddMonths(12);
    private DateTime _nextPaymentDueDate = DateTime.UtcNow.AddMonths(1);
    private int _daysOverdue = 0;
    private decimal _delinquencyPercentage = 0;
    private decimal _principalRemaining = 9000;
    private Guid _branchId = Guid.NewGuid();
    private string _currencyCode = "DOP";
    private LoanPurpose? _loanPurpose = null;
    private decimal _totalInterestAmount = 1200;
    private decimal _paidInterest = 100;
    private decimal _remainingInterest = 1100;
    private decimal _amortizationBalance = 9000;
    private decimal _principalPaid = 1000;
    private decimal _maxCredit = 50000;
    private bool _requiresGuarantor = false;
    private Guid? _guarantorId = null;
    private DateTime? _lastPaymentDate = null;
    private DateTime _createdOn = DateTime.UtcNow;
    private DateTime? _updatedOn = null;

    public LoanDetailResponseBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public LoanDetailResponseBuilder WithStatus(LoanStatus status)
    {
        _loanStatus = status;
        return this;
    }

    public LoanDetailResponseBuilder WithPrincipalAmount(decimal amount)
    {
        _principalAmount = amount;
        _principalRemaining = amount * 0.9m;
        return this;
    }

    public LoanDetailResponseBuilder WithCustomerId(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public LoanDetailResponseBuilder WithBranchId(Guid branchId)
    {
        _branchId = branchId;
        return this;
    }

    public LoanDetailResponseBuilder WithDaysOverdue(int days)
    {
        _daysOverdue = days;
        _delinquencyPercentage = days > 0 ? (days / 30m) * 100 : 0;
        return this;
    }

    public LoanDetailResponseBuilder WithInterestRate(decimal rate)
    {
        _interestRate = rate;
        return this;
    }

    public LoanDetailResponseBuilder WithTermMonths(int months)
    {
        _termMonths = months;
        _maturityDate = DateTime.UtcNow.AddMonths(months);
        return this;
    }

    public LoanDetailResponseBuilder WithCustomerName(string name)
    {
        _customerName = name;
        return this;
    }

    public LoanDetailResponseBuilder RequiresGuarantor(Guid? guarantorId = null)
    {
        _requiresGuarantor = true;
        _guarantorId = guarantorId ?? Guid.NewGuid();
        return this;
    }

    public LoanDetailResponseBuilder WithLastPaymentDate(DateTime date)
    {
        _lastPaymentDate = date;
        return this;
    }

    public LoanDetailResponse Build()
    {
        return new LoanDetailResponse(
            Id: _id,
            LoanNumber: _loanNumber,
            CustomerId: _customerId,
            CustomerName: _customerName,
            PrincipalAmount: _principalAmount,
            InterestRate: _interestRate,
            TermMonths: _termMonths,
            InstallmentAmount: _installmentAmount,
            LoanStatus: _loanStatus,
            DisbursementDate: _disbursementDate,
            MaturityDate: _maturityDate,
            NextPaymentDueDate: _nextPaymentDueDate,
            DaysOverdue: _daysOverdue,
            DelinquencyPercentage: _delinquencyPercentage,
            PrincipalRemaining: _principalRemaining,
            BranchId: _branchId,
            CurrencyCode: _currencyCode,
            LoanPurpose: _loanPurpose,
            TotalInterestAmount: _totalInterestAmount,
            PaidInterest: _paidInterest,
            RemainingInterest: _remainingInterest,
            AmortizationBalance: _amortizationBalance,
            PrincipalPaid: _principalPaid,
            MaxCredit: _maxCredit,
            RequiresGuarantor: _requiresGuarantor,
            GuarantorId: _guarantorId,
            LastPaymentDate: _lastPaymentDate,
            CreatedOn: _createdOn,
            UpdatedOn: _updatedOn
        );
    }
}
