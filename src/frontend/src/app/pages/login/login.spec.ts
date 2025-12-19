import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty form', () => {
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
  });

  it('should have email field required', () => {
    const email = component.loginForm.get('email');
    expect(email?.hasError('required')).toBe(true);
  });

  it('should validate email format', () => {
    const email = component.loginForm.get('email');
    email?.setValue('invalid-email');
    expect(email?.hasError('email')).toBe(true);

    email?.setValue('valid@email.com');
    expect(email?.hasError('email')).toBe(false);
  });

  it('should have password field required', () => {
    const password = component.loginForm.get('password');
    expect(password?.hasError('required')).toBe(true);
  });

  it('should require password minimum length of 6', () => {
    const password = component.loginForm.get('password');
    password?.setValue('12345');
    expect(password?.hasError('minlength')).toBe(true);

    password?.setValue('123456');
    expect(password?.hasError('minlength')).toBe(false);
  });

  it('should not submit when form is invalid', () => {
    component.onSubmit();
    httpMock.expectNone(() => true);
  });

  it('should handle successful login', () => {
    spyOn(localStorage, 'setItem');
    component.loginForm.setValue({ email: 'test@test.com', password: '123456' });
    component.onSubmit();

    const req = httpMock.expectOne(req => req.url.includes('/auth/login'));
    req.flush({ token: 'test-token' });

    expect(localStorage.setItem).toHaveBeenCalledWith('auth_token', 'test-token');
    expect(router.navigate).toHaveBeenCalledWith(['/home']);
    expect(component.loading).toBe(false);
  });
});
