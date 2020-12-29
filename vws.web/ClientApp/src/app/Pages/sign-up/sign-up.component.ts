import {Component, OnInit} from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';
import {UserService} from 'src/app/Services/user.service';


@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.scss']
})
export class SignUpComponent implements OnInit {

  hide = true;
  logo = '/assets/Images/logo.png';

  signUpForm: FormGroup;

  constructor(private _formBuilder: FormBuilder, public userService: UserService) {
  }

  ngOnInit() {
    this.signUpForm = this._formBuilder.group({
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(5)]]
    });
  }

  clicked(): void {
    console.log(this.signUpForm.get('username').value);
    console.log(this.signUpForm.get('email').value);
    console.log(this.signUpForm.get('password').value);
  }

  registerUser() {
    const newUser = { username: this.signUpForm.get('username').value, email: this.signUpForm.get('email').value, password: this.signUpForm.get('password').value};
    this.userService.register(newUser).subscribe(data =>{
      console.log("hi");
    });

  }

}
