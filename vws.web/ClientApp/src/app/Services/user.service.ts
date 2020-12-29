import { Injectable } from '@angular/core';
import {HttpClient} from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  postUrl = 'http://localhost:4300/api/Account/register';
  constructor(private httpClient: HttpClient) { }

  register(information) {
    // return this.httpClient.get('https://google.com');
    return this.httpClient.post('http://localhost:4300/api/Account/register', information);
  }
}
