import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {SignInUserDTO} from "../../DTOs/Account/SignInUserDTO";
import {SignUpUserDTO} from "../../DTOs/Account/SignUpUserDTO";
import {ForgetPasswordUserDTO} from "../../DTOs/Account/ForgetPasswordUserDTO";
import {SendConfirmEmailDTO} from "../../DTOs/Account/SendConfirmEmailDTO";
import {ConfirmEmailDTO} from "../../DTOs/Account/ConfirmEmailDTO";
import {ResetPasswordDTO} from "../../DTOs/Account/ResetPasswordDTO";

@Injectable({
  providedIn: 'root'
})
export class AccountService {

  constructor(private http: HttpClient) {
  }


  signIn(signInData: SignInUserDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/login', signInData);
  }

  signUp(signUpData: SignUpUserDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/register', signUpData);
  }

  forgetPassword(forgetPasswordData: ForgetPasswordUserDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/sendResetPassEmail', forgetPasswordData);
  }

  resetPassword(resetPasswordData: ResetPasswordDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/resetPassword', resetPasswordData)
  }

  sendConfirmEmail(sendConfirmEmailData: SendConfirmEmailDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/sendConfirmEmail', sendConfirmEmailData)
  }

  confirmEmail(ConfirmEmailData: ConfirmEmailDTO): Observable<any> {
    return this.http.post<any>('/en-US/Account/confirmEmail', ConfirmEmailData)
  }


}
