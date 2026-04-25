using Bogus;
using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Application.FakeData
{
    public static class FakeDbQueryDelinquencyPolicies
    {
        public static readonly IEnumerable<DelinquencyPolicy> DelinquencyPolicies;

        static FakeDbQueryDelinquencyPolicies()
        {
            Randomizer.Seed = new Random(111111);

            var policies = new List<DelinquencyPolicy>
            {
                new DelinquencyPolicy
                {
                    Id = Guid.NewGuid(),
                    Name = "Standard",
                    DaysUntilEarlyDelinquency = 1,
                    EarlyDelinquencyPenalty = 0.05m,
                    DaysUntilSevereDelinquency = 30,
                    SevereDelinquencyPenalty = 0.15m,
                    DaysUntilWriteOff = 90,
                    AutomaticallyChargeLateFees = true,
                    DailyPenaltyRate = 0.001m
                },
                new DelinquencyPolicy
                {
                    Id = Guid.NewGuid(),
                    Name = "Aggressive",
                    DaysUntilEarlyDelinquency = 0,
                    EarlyDelinquencyPenalty = 0.10m,
                    DaysUntilSevereDelinquency = 15,
                    SevereDelinquencyPenalty = 0.25m,
                    DaysUntilWriteOff = 60,
                    AutomaticallyChargeLateFees = true,
                    DailyPenaltyRate = 0.002m
                },
                new DelinquencyPolicy
                {
                    Id = Guid.NewGuid(),
                    Name = "Conservative",
                    DaysUntilEarlyDelinquency = 5,
                    EarlyDelinquencyPenalty = 0.03m,
                    DaysUntilSevereDelinquency = 45,
                    SevereDelinquencyPenalty = 0.10m,
                    DaysUntilWriteOff = 120,
                    AutomaticallyChargeLateFees = false,
                    DailyPenaltyRate = 0.0005m
                }
            };

            DelinquencyPolicies = policies;
        }
    }
}
