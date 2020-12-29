import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { HomeComponent } from './Home/home.component';
import { SignInComponent } from './Pages/sign-in/sign-in.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import {MatStepperModule} from '@angular/material/stepper';
import {ReactiveFormsModule} from '@angular/forms';
import {MatFormFieldModule} from '@angular/material/form-field';
import {MatIconModule} from '@angular/material/icon';
import {MatButtonModule} from '@angular/material/button';
import {MatSliderModule} from '@angular/material/slider';
import {MatInputModule} from '@angular/material/input';
import {MatRippleModule} from '@angular/material/core';
import {MatGridListModule} from '@angular/material/grid-list';
import {MatListModule} from '@angular/material/list';
import {MatSlideToggleModule} from '@angular/material/slide-toggle';
import {MatCardModule} from '@angular/material/card';
import {MatAutocompleteModule} from '@angular/material/autocomplete';
import {MatCheckboxModule} from '@angular/material/checkbox';
import {MatRadioModule} from '@angular/material/radio';
import { OnBoardingComponent } from './Pages/on-boarding/on-boarding.component';
import { SignUpComponent } from './Pages/sign-up/sign-up.component';
import { ForgetPasswordComponent } from './Pages/forget-password/forget-password.component';
import { ProfileSettingsComponent } from './Pages/profile-settings/profile-settings.component';
import {UserService} from 'src/app/Services/user.service';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    SignInComponent,
    OnBoardingComponent,
    SignUpComponent,
    ForgetPasswordComponent,
    ProfileSettingsComponent
  ],
    imports: [
        BrowserModule.withServerTransition({appId: 'ng-cli-universal'}),
        HttpClientModule,
        FormsModule,
        RouterModule.forRoot([
            {path: '', component: HomeComponent, pathMatch: 'full'},
            {path: 'sign-in', component: SignInComponent, pathMatch: 'full'},
            {path: 'on-boarding', component: OnBoardingComponent, pathMatch: 'full'},
            {path: 'sign-up', component: SignUpComponent, pathMatch: 'full'},
            {path: 'forget-password', component: ForgetPasswordComponent, pathMatch: 'full'},
            {path: 'profile-settings', component: ProfileSettingsComponent, pathMatch: 'full'}
        ]),
        BrowserAnimationsModule,
        MatStepperModule,
        MatIconModule,
        FormsModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatButtonModule,
        MatSliderModule,
        MatInputModule,
        MatRippleModule,
        MatGridListModule,
        MatListModule,
        MatSlideToggleModule,
        MatCardModule,
        MatAutocompleteModule,
        MatCheckboxModule,
        MatRadioModule
    ],
  providers: [
    UserService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
