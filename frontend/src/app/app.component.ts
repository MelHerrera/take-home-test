import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { Loan } from './models/loan.model';
import { ProblemDetails } from './models/problem-details.model';
import { LoanService } from './services/loan.service';
import { PaymentDialogComponent, PaymentDialogData } from './components/payment-dialog/payment-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule,
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  displayedColumns: string[] = [
    'applicantName',
    'amountRequested',
    'currentBalance',
    'status',
    'actions',
  ];
  loans: Loan[] = [];
  loading = true;
  errorMessage: string | null = null;
  payingLoanId: string | null = null;

  constructor(
    private readonly loanService: LoanService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.loadLoans();
  }

  openPaymentDialog(loan: Loan): void {
    const dialogRef = this.dialog.open<PaymentDialogComponent, PaymentDialogData, number>(
      PaymentDialogComponent,
      { data: { loan }, width: '360px' },
    );

    dialogRef.afterClosed().subscribe((amount) => {
      if (amount == null) {
        return;
      }

      this.payingLoanId = loan.id;

      this.loanService.registerPayment(loan.id, amount).subscribe({
        next: (updatedLoan) => {
          const index = this.loans.findIndex((l) => l.id === updatedLoan.id);
          if (index !== -1) {
            this.loans[index] = updatedLoan;
            this.loans = [...this.loans];
          }
          this.payingLoanId = null;
          this.snackBar.open('Payment registered successfully.', 'Close', { duration: 3000 });
        },
        error: (error: HttpErrorResponse) => {
          const problem = error.error as ProblemDetails | undefined;
          this.payingLoanId = null;
          this.snackBar.open(problem?.detail ?? 'Could not register the payment.', 'Close', {
            duration: 4000,
          });
        },
      });
    });
  }

  private loadLoans(): void {
    this.loanService.getAll().subscribe({
      next: (loans) => {
        this.loans = loans;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'No se pudieron cargar los préstamos. Intenta de nuevo más tarde.';
        this.loading = false;
      },
    });
  }
}
