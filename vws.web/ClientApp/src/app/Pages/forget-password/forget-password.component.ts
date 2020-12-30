import {Component, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {ForgetPasswordUserDTO} from '../../DTOs/Account/ForgetPasswordUserDTO';
import {AccountService} from 'src/app/Services/AccountService/account.service';
import {ResetPasswordDTO} from "../../DTOs/Account/ResetPasswordDTO";
import {Router} from "@angular/router";

@Component({
  selector: 'app-forget-password',
  templateUrl: './forget-password.component.html',
  styleUrls: ['./forget-password.component.scss']
})
export class ForgetPasswordComponent implements OnInit {

  logo = '/assets/Images/logo.png';
  forgetPasswordForm: FormGroup;
  emailSent = false;
  hide = false;

  constructor(private _formBuilder: FormBuilder, private accountService: AccountService, private router: Router) {
  }

  ngOnInit() {
    this.forgetPasswordForm = this._formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      newPassword: ['', [Validators.required, Validators.email]],
      validationCode: ['', [Validators.required, Validators.email]],
    });
  }


  submitForgetPasswordForm() {
    const forgetPasswordData = new ForgetPasswordUserDTO(
      this.forgetPasswordForm.controls.email.value
    );
    console.log(forgetPasswordData);
    this.accountService.forgetPassword(forgetPasswordData).subscribe(res => {
      console.log(res);
      if (res.status === 'Success') {
        this.emailSent = true;
        //this.forgetPasswordForm.reset();
      }
    });

  }

  changePassword() {
    if (this.emailSent) {
      const resetPasswordData = new ResetPasswordDTO(
        this.forgetPasswordForm.controls.email.value,
        this.forgetPasswordForm.controls.newPassword.value,
        this.forgetPasswordForm.controls.validationCode.value,
      );
      this.accountService.resetPassword(resetPasswordData).subscribe(res => {
        if (res.status === 'Success') {
          this.router.navigate(["/sign-in"]);
          console.log('so happy:)');
        }
      })
    }
  }

}
