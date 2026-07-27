using Fundo.Domain.Entities;
using Fundo.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(loan => loan.Id);

        builder.Property(loan => loan.ApplicantName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(loan => loan.AmountRequested)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(loan => loan.CurrentBalance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(loan => loan.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicantName = "John Doe",
                AmountRequested = 25000m,
                CurrentBalance = 18750m,
                Status = LoanStatus.Active,
            },
            new
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ApplicantName = "Jane Smith",
                AmountRequested = 15000m,
                CurrentBalance = 0m,
                Status = LoanStatus.Paid,
            },
            new
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ApplicantName = "Robert Johnson",
                AmountRequested = 50000m,
                CurrentBalance = 32500m,
                Status = LoanStatus.Active,
            });
    }
}
