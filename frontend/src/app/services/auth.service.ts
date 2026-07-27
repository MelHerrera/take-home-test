import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { TokenResponse } from '../models/token-response.model';

const TOKEN_STORAGE_KEY = 'fundo_access_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  // Signal so components (e.g. AppComponent) can reactively show/hide the login form.
  readonly isAuthenticated = signal(this.getToken() !== null);

  constructor(private readonly http: HttpClient) {}

  login(username: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.baseUrl}/token`, { username, password }).pipe(
      tap((response) => {
        localStorage.setItem(TOKEN_STORAGE_KEY, response.accessToken);
        this.isAuthenticated.set(true);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this.isAuthenticated.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }
}
