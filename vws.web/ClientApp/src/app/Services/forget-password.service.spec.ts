import { TestBed } from '@angular/core/testing';

import { ForgetPasswordService } from './forget-password.service';

describe('ForgetPasswordService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ForgetPasswordService = TestBed.get(ForgetPasswordService);
    expect(service).toBeTruthy();
  });
});
