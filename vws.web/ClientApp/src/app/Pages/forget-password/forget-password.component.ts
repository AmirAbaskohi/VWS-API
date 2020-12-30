import { Component, OnInit } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {ForgetPasswordUserDTO} from '../../DTOs/Account/ForgetPasswordUserDTO';
import {AccountService} from 'src/app/Services/AccountService/account.service';

@Component({
  selector: 'app-forget-password',
  templateUrl: './forget-password.component.html',
  styleUrls: ['./forget-password.component.scss']
})
export class ForgetPasswordComponent implements OnInit {

  logo = '/assets/Images/logo.png';
  forgetPasswordForm: FormGroup;

  constructor(private _formBuilder: FormBuilder, public accountService: AccountService) {
  }

  ngOnInit() {
    this.forgetPasswordForm = this._formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }


  submitForgetPasswordForm() {
    const forgetPasswordData = new ForgetPasswordUserDTO(
      this.forgetPasswordForm.controls.email.value
    );
    console.log(forgetPasswordData);
    this.accountService.forgetPassword(forgetPasswordData).subscribe(res => {
      console.log(res);
      if (res.status === 200) {
        this.forgetPasswordForm.reset();
      }
    });

  }

}
