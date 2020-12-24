import { Component, OnInit } from '@angular/core';
import {FormBuilder, FormGroup, Validators} from '@angular/forms';

@Component({
  selector: 'app-on-boarding',
  templateUrl: './on-boarding.component.html',
  styleUrls: ['./on-boarding.component.scss']
})
export class OnBoardingComponent implements OnInit {
  isLinear = false;
  firstFormGroup: FormGroup;
  secondFormGroup: FormGroup;
  thirdFormGroup: FormGroup;

  logo = '/assets/Images/clickup.png';
  git = '/assets/Images/github.png';
  gitlab = '/assets/Images/gitlab.png';
  teams = '/assets/Images/teams.png';
  slack = '/assets/Images/slack.png';
  intercom = '/assets/Images/intercom.png';
  sentry = '/assets/Images/sentry.png';



  hide = true;
  color: string;
  userMainTheme = '#7b68ee';

  colorSlideToggle = '#7b68ee';
  checkedSlideToggle = false;
  disabledSlideToggle = false;
  constructor(private _formBuilder: FormBuilder) { }

  ngOnInit() {
    this.firstFormGroup = this._formBuilder.group({
      firstCtrl: ['', Validators.required],
      secondCtrl: ['', Validators.required]
    });
    this.secondFormGroup = this._formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.thirdFormGroup = this._formBuilder.group({
      password: ['', [Validators.required, Validators.minLength(5)]]
    });
  }

  grid(id): void {

    // console.log(document.getElementById(id).style.opacity);
    if (document.getElementById(id).style.opacity == '1') {
      document.getElementById(id).style.opacity = '0.2';
    } else {
      document.getElementById(id).style.opacity = '1';
    }
  }
  selectTheme(id): void {
    this.userMainTheme = '#' + id;
    this.colorSlideToggle = '#' + id;
  }

}
