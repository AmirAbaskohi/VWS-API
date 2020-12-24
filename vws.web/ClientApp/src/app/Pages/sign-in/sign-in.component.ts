import {Component, OnInit} from '@angular/core';
import {FormControl, FormGroup, Validators} from "@angular/forms";
import {SignInUserDTO} from "../../DTOs/Account/SignInUserDTO";
import {SignInService} from "../../Services/sign-in.service";

@Component({
  selector: 'app-sign-in',
  templateUrl: './sign-in.component.html',
  styleUrls: ['./sign-in.component.scss']
})
export class SignInComponent implements OnInit {

  public signInForm: FormGroup;

  constructor(private signInService: SignInService) {
  }

  ngOnInit() {

    this.signInForm = new FormGroup({
      usernameOrEmail: new FormControl(
        null,
        [
          Validators.required
        ]
      ),
      password: new FormControl(),
      rememberMe: new FormControl(),
    });
  }

  submitSignInForm() {
    const signInData = new SignInUserDTO(
      this.signInForm.controls.usernameOrEmail.value,
      this.signInForm.controls.password.value,
      this.signInForm.controls.rememberMe.value,
    );
    console.log(signInData);
    this.signInService.signIn(signInData).subscribe(res => {
      console.log(res);
      if (res.status === 200) {
        this.signInForm.reset();
      }
    });

  }
}
