import { Injectable } from '@angular/core';
import {ForgetPasswordUserDTO} from '../DTOs/Account/ForgetPasswordUserDTO';
import {Observable} from 'rxjs';
import {HttpClient} from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ForgetPasswordService {

  constructor(private http: HttpClient) { }

  forgetPassword(forgetPasswordData: ForgetPasswordUserDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/register', forgetPasswordData);
  }
}
