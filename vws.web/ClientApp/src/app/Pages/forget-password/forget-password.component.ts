import { Component, OnInit } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {ForgetPasswordUserDTO} from '../../DTOs/Account/ForgetPasswordUserDTO';
import {ForgetPasswordService} from 'src/app/Services/forget-password.service';

@Component({
  selector: 'app-forget-password',
  templateUrl: './forget-password.component.html',
  styleUrls: ['./forget-password.component.scss']
})
export class ForgetPasswordComponent implements OnInit {

  logo = '/assets/Images/logo.png';
  forgetPasswordForm: FormGroup;

  constructor(private _formBuilder: FormBuilder, public forgetPasswordService: ForgetPasswordService) {
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
    this.forgetPasswordService.forgetPassword(forgetPasswordData).subscribe(res => {
      console.log(res);
      if (res.status === 200) {
        this.forgetPasswordForm.reset();
      }
    });

  }

}
