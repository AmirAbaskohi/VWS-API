import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {SignInUserDTO} from "../DTOs/Account/SignInUserDTO";

@Injectable({
  providedIn: 'root'
})
export class SignInService {

  constructor(private http: HttpClient) {
  }


  signIn(signInData: SignInUserDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/login', signInData);
  }

}
