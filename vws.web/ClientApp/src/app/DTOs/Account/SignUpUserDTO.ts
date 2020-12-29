export class SignUpUserDTO {
  public username: string;
  public email: string;
  public password: string;
  constructor(
    username: string,
    email: string,
    password: string
  ) {
    this.username = username;
    this.email = email;
    this.password = password;
  }
}
