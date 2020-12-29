export class SignInUserDTO {
  public UsernameOrEmail: string;
  public Password: string;
  public RememberMe: boolean;
  constructor(
    UsernameOrEmail: string,
    Password: string,
    RememberMe: boolean
  ) {
    this.UsernameOrEmail = UsernameOrEmail;
    this.Password = Password;
    this.RememberMe = RememberMe;
  }
}
