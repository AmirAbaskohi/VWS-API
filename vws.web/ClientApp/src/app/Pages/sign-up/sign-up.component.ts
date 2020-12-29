import {Component, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {SignUpService} from 'src/app/Services/sign-up.service';
import {SignUpUserDTO} from '../../DTOs/Account/SignUpUserDTO';



@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.scss']
})
export class SignUpComponent implements OnInit {

  hide = true;
  logo = '/assets/Images/logo.png';

  signUpForm: FormGroup;

  constructor(private _formBuilder: FormBuilder, public signUpService: SignUpService) {
  }

  ngOnInit() {
    this.signUpForm = this._formBuilder.group({
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(5)]]
    });
  }


  submitSignUpForm() {
    const signUpData = new SignUpUserDTO(
      this.signUpForm.controls.username.value,
      this.signUpForm.controls.email.value,
      this.signUpForm.controls.password.value ,
    );
    console.log(signUpData);
    this.signUpService.signUp(signUpData).subscribe(res => {
      console.log(res);
      if (res.status === 200) {
        this.signUpForm.reset();
      }
    });

  }

}
