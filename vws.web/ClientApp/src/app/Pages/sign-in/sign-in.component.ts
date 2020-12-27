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
      UsernameOrEmail: new FormControl(
        null,
        [
          Validators.required
        ]
      ),
      Password: new FormControl(),
      RememberMe: new FormControl(),
    });
  }

  submitSignInForm() {
    const signInData = new SignInUserDTO(
      this.signInForm.controls.UsernameOrEmail.value,
      this.signInForm.controls.Password.value,
      this.signInForm.controls.RememberMe.value ? true : false,
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
