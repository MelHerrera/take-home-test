import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Loan } from '../models/loan.model';

@Injectable({ providedIn: 'root' })
export class LoanService {
  private readonly baseUrl = `${environment.apiUrl}/loans`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Loan[]> {
    return this.http.get<Loan[]>(this.baseUrl);
  }

  registerPayment(id: string, amount: number): Observable<Loan> {
    return this.http.post<Loan>(`${this.baseUrl}/${id}/payment`, { amount });
  }
}
