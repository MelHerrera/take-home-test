import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';

import { Loan } from '../../models/loan.model';

export interface PaymentDialogData {
  loan: Loan;
}

@Component({
  selector: 'app-payment-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './payment-dialog.component.html',
  styleUrls: ['./payment-dialog.component.scss'],
})
export class PaymentDialogComponent {
  amountControl: FormControl<number | null>;

  constructor(
    private readonly dialogRef: MatDialogRef<PaymentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PaymentDialogData,
  ) {
    this.amountControl = new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(0.01),
      Validators.max(this.data.loan.currentBalance),
    ]);
  }

  submit(): void {
    if (this.amountControl.invalid) {
      this.amountControl.markAsTouched();
      return;
    }
    this.dialogRef.close(this.amountControl.value);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
