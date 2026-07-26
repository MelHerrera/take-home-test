export interface Loan {
  id: string;
  applicantName: string;
  amountRequested: number;
  currentBalance: number;
  status: 'Active' | 'Paid';
}
