import {Component, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {SignUpUserDTO} from '../../DTOs/Account/SignUpUserDTO';
import {AccountService} from 'src/app/Services/AccountService/account.service';
import {SendConfirmEmailDTO} from "../../DTOs/Account/SendConfirmEmailDTO";
import {ConfirmEmailDTO} from "../../DTOs/Account/ConfirmEmailDTO";
import {Router} from "@angular/router";


@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.scss']
})
export class SignUpComponent implements OnInit {

  hide = true;
  logo = '/assets/Images/logo.png';
  signUpSuccessful = false;
  emailSent = false;


  signUpForm: FormGroup;

  constructor(private _formBuilder: FormBuilder, private accountService: AccountService,private router: Router) {
  }

  ngOnInit() {
    this.signUpForm = this._formBuilder.group({
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(5)]],
      confirm: ['', Validators.required]
    });
  }


  submitSignUpForm() {
    const signUpData = new SignUpUserDTO(
      this.signUpForm.controls.username.value,
      this.signUpForm.controls.email.value,
      this.signUpForm.controls.password.value,
    );
    const sendConfirmEmailData = new SendConfirmEmailDTO(this.signUpForm.controls.email.value);
    console.log(signUpData);
    this.accountService.signUp(signUpData).subscribe(res => {
      console.log(res);
      if (res.status === 'Success') {
        this.signUpSuccessful = true;
        this.accountService.sendConfirmEmail(sendConfirmEmailData).subscribe(response => {
          console.log(response);
          if (response.status === 'Success') {
            this.emailSent = true;
          }
        });
        //this.signUpForm.reset();
      }
    });
  }

  confirmAccount() {
    if (this.emailSent && this.signUpSuccessful) {
      const confirmatEmailData = new ConfirmEmailDTO(this.signUpForm.controls.email.value, this.signUpForm.controls.confirm.value);
      this.accountService.confirmEmail(confirmatEmailData).subscribe(res => {
        console.log(res);
        if (res.status === 'Success') {
          this.router.navigate(["/sign-in"]);
          console.log("so happy:)");
        }
      })
    } else {
      console.log("error in confirmAccount func");
    }
  }

}
