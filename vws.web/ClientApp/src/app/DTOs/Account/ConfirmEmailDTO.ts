export class ConfirmEmailDTO {
  public email: string;
  public validationCode: string;

  constructor(
    email: string,
    validationCode: string
  ) {
    this.email = email;
    this.validationCode = validationCode;
  }
}
