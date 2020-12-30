export class ResetPasswordDTO {
  public email: string;
  public validationCode: string;
  public newPassword: string;

  constructor(
    email: string,
    newPassword: string,
    validationCode: string
  ) {
    this.email = email;
    this.newPassword = newPassword;
    this.validationCode = validationCode;
  }
}
