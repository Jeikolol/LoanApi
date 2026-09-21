using Application.Features.Loans;
using Domain.Enums;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class LoanLifecycleRulesTests
    {
        [TestCase(LoanStatus.Pending, LoanAction.Approve, LoanStatus.Approved)]
        [TestCase(LoanStatus.Approved, LoanAction.Disburse, LoanStatus.Disbursed)]
        [TestCase(LoanStatus.Pending, LoanAction.Cancel, LoanStatus.Closed)]
        [TestCase(LoanStatus.Approved, LoanAction.Cancel, LoanStatus.Closed)]
        [TestCase(LoanStatus.FullyPaid, LoanAction.Close, LoanStatus.Closed)]
        public void Apply_AllowsTheStepsOfTheLifeCycle(LoanStatus current, LoanAction action, LoanStatus expected)
        {
            LoanLifecycleRules.Apply(current, action).Should().Be(expected);
        }

        [TestCase(LoanStatus.Approved, LoanAction.Approve)]
        [TestCase(LoanStatus.Pending, LoanAction.Disburse)]
        [TestCase(LoanStatus.Active, LoanAction.Cancel)]
        [TestCase(LoanStatus.Disbursed, LoanAction.Cancel)]
        [TestCase(LoanStatus.Active, LoanAction.Close)]
        [TestCase(LoanStatus.Defaulted, LoanAction.Close)]
        [TestCase(LoanStatus.Closed, LoanAction.Approve)]
        [TestCase(LoanStatus.WrittenOff, LoanAction.Close)]
        public void Apply_RejectsEverythingElse(LoanStatus current, LoanAction action)
        {
            LoanLifecycleRules.Apply(current, action).Should().BeNull();
        }

        [Test]
        public void AvailableActions_ListsWhatTheStatusAllows()
        {
            LoanLifecycleRules.AvailableActions(LoanStatus.Pending).Should().BeEquivalentTo([LoanAction.Approve, LoanAction.Cancel]);
            LoanLifecycleRules.AvailableActions(LoanStatus.Approved).Should().BeEquivalentTo([LoanAction.Disburse, LoanAction.Cancel]);
            LoanLifecycleRules.AvailableActions(LoanStatus.FullyPaid).Should().BeEquivalentTo([LoanAction.Close]);
            LoanLifecycleRules.AvailableActions(LoanStatus.Active).Should().BeEmpty();
            LoanLifecycleRules.AvailableActions(LoanStatus.Closed).Should().BeEmpty();
        }

        [Test]
        public void EveryStatusReachedByAnActionIsANormalStatus()
        {
            foreach (LoanStatus status in Enum.GetValues<LoanStatus>())
            {
                foreach (LoanAction action in Enum.GetValues<LoanAction>())
                {
                    LoanStatus? next = LoanLifecycleRules.Apply(status, action);
                    if (next is not null) next.Should().NotBe(status);
                }
            }
        }

        [Test]
        public void ApprovalProblem_BlocksOnlyTheCreator()
        {
            Guid creator = Guid.NewGuid();

            LoanLifecycleRules.ApprovalProblem(creator, creator).Should().NotBeNull();
            LoanLifecycleRules.ApprovalProblem(creator, Guid.NewGuid()).Should().BeNull();
            LoanLifecycleRules.ApprovalProblem(null, creator).Should().BeNull("imported loans have no recorded creator");
            LoanLifecycleRules.ApprovalProblem(creator, null).Should().BeNull();
        }

        [Test]
        public void EligibilityProblems_IsEmptyForAGoodCustomer()
        {
            LoanLifecycleRules.EligibilityProblems(false, CustomerStatus.Active, 100_000m, 20_000m, 0, 50_000m).Should().BeEmpty();
        }

        [Test]
        public void EligibilityProblems_AllowsExactlyTheCreditLimit()
        {
            LoanLifecycleRules.EligibilityProblems(false, CustomerStatus.Active, 100_000m, 50_000m, 0, 50_000m).Should().BeEmpty();
        }

        [Test]
        public void EligibilityProblems_ReportsEveryReason()
        {
            List<string> problems = LoanLifecycleRules.EligibilityProblems(true, CustomerStatus.Suspended, 10_000m, 9_000m, 45, 5_000m);

            problems.Should().HaveCount(4);
            problems.Should().Contain(p => p.Contains("blacklisted"));
            problems.Should().Contain(p => p.Contains("Suspended"));
            problems.Should().Contain(p => p.Contains("overdue"));
            problems.Should().Contain(p => p.Contains("credit limit"));
        }

        [Test]
        public void EligibilityProblems_ToleratesUpToThirtyDaysOverdue()
        {
            LoanLifecycleRules.EligibilityProblems(false, CustomerStatus.Active, 100_000m, 0m, 30, 1_000m).Should().BeEmpty();
            LoanLifecycleRules.EligibilityProblems(false, CustomerStatus.Active, 100_000m, 0m, 31, 1_000m).Should().ContainSingle();
        }

        [Test]
        public void Installment_MatchesTheFirstInstallmentOfTheSchedule()
        {
            foreach ((decimal principal, decimal rate, int months) in new[] { (120_000m, 24m, 24), (50_000m, 0m, 10), (300_000m, 18.5m, 60) })
            {
                AmortizationSchedule.Installment(principal, rate, months).Should().Be(AmortizationSchedule.Build(principal, rate, months)[0].Payment);
            }
        }

        [Test]
        public void TotalInterest_IsZeroWithoutRate_AndPositiveOtherwise()
        {
            AmortizationSchedule.TotalInterest(12_000m, 0m, 12).Should().Be(0m);
            AmortizationSchedule.TotalInterest(12_000m, 24m, 12).Should().BeGreaterThan(0m);
        }
    }
}
