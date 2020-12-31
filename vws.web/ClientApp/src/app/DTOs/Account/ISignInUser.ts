import DateTimeFormat = Intl.DateTimeFormat;

export interface ISignInUser {
  status: string;
  data: {
    token: string;
    expiration: Date;
  }
}
