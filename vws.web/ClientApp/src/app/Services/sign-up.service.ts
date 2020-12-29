import { Injectable } from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {SignUpUserDTO} from '../DTOs/Account/SignUpUserDTO';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignUpService {

  constructor(private http: HttpClient) { }

  signUp(signUpData: SignUpUserDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/register', signUpData);
  }
}
