import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ButtonComponent } from '../../shared/button/button';
import { PhoneInputComponent } from '../../shared/phone-input/phone-input';
import { LawyerService } from '../../services/lawyer.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal';
import {
  sanitizeModel,
  isValidEmail,
  isValidPhone,
  isValidNif,
  isValidRegisterNumber,
} from '../../shared/input.util';
import { Lawyer } from '../../models/lawyer';

@Component({
  selector: 'app-create-lawyer',
  standalone: true,
  templateUrl: './create-lawyer.html',
  styleUrls: ['./create-lawyer.css'],
  imports: [
    CommonModule,
    FormsModule,
    PageTitleComponent,
    ButtonComponent,
    PhoneInputComponent,
    ConfirmModalComponent,
  ],
})
export class CreateLawyerComponent {
  model: Partial<
    Lawyer & { password?: string; confirmPassword?: string; phoneCountryCode?: number }
  > = {
    name: '',
    email: '',
    professionalRegister: '',
    nif: '',
    phone: '',
    phoneCountryCode: 351,
    isActive: true,
    password: '',
    confirmPassword: '',
  };

  submitting = false;
  fieldErrors: Record<string, string[]> = {};
  generalError = '';
  showCancelModal = false;

  constructor(
    private lawyerService: LawyerService,
    public router: Router,
    private notifications: NotificationService
  ) {}

  get initials(): string {
    const name = (this.model.name || '').trim();
    if (!name) return '👤';
    const parts = name.split(/\s+/).filter((p) => p.length > 0);
    const initials = parts
      .map((p) => p.charAt(0))
      .join('')
      .slice(0, 2)
      .toUpperCase();
    return initials || '👤';
  }

  createLawyer() {
    if (!this.isComplete() || this.submitting) return;
    if (!this.model.name) return;
    if (!this.isPasswordValid()) {
      this.notifications.showError('Password is required and must match confirmation.');
      return;
    }
    this.submitting = true;
    this.fieldErrors = {};
    this.generalError = '';

    const sanitized = sanitizeModel(this.model as Record<string, any>);
    const payload: any = { ...sanitized };

    if (sanitized['phone']) {
      payload.phoneNumber = sanitized['phone'];
      payload.phoneCountryCode = sanitized['phoneCountryCode'] || 351;
      payload.phoneIsMain = true;
    }

    delete payload['phone'];
    delete payload.confirmPassword;

    this.lawyerService.createLawyer(payload).subscribe({
      next: (_) => {
        this.notifications.showSuccess('Lawyer created successfully');
        setTimeout(() => this.router.navigate(['/lawyers']), 500);
      },
      error: (err) => {
        console.error('Create lawyer failed', err);
        const body = err?.error || {};
        if (body && typeof body === 'object' && body.errors) {
          for (const k of Object.keys(body.errors)) {
            const key = k.toString().toLowerCase();
            const vals = Array.isArray(body.errors[k])
              ? body.errors[k].map((v: any) => String(v))
              : [String(body.errors[k])];
            this.fieldErrors[key] = vals;
          }
          this.notifications.showError('Please correct the highlighted fields.');
        } else if (body && (body.message || body.error)) {
          this.generalError = (body.message || body.error).toString();
          this.notifications.showError(this.generalError || 'Failed to create lawyer.');
        } else {
          this.notifications.showError('Failed to create lawyer.');
        }

        this.submitting = false;
      },
    });
  }

  isPasswordValid(): boolean {
    const p = this.model.password || '';
    const c = this.model.confirmPassword || '';
    return p.length >= 8 && p === c;
  }

  isComplete(): boolean {
    const s = (v?: string) => (v || '').toString().trim().length > 0;
    const email = (this.model.email || '').toString().trim();
    const emailOk = /\S+@\S+\.\S+/.test(email);

    return (
      s(this.model.name) &&
      isValidEmail(email) &&
      isValidRegisterNumber(this.model.professionalRegister || '') &&
      isValidNif((this.model.nif || '').toString()) &&
      isValidPhone((this.model.phone || '').toString()) &&
      this.isPasswordValid()
    );
  }

  get isEmailValid() {
    return isValidEmail(this.model.email || '');
  }
  get isPhoneValid() {
    return isValidPhone(this.model.phone || '');
  }
  get isNifValid() {
    return isValidNif(this.model.nif || '');
  }
  get isRegisterNumberValid() {
    return isValidRegisterNumber(this.model.professionalRegister || '');
  }

  onCancelClick() {
    this.showCancelModal = true;
  }

  onConfirmCancel() {
    this.showCancelModal = false;
    this.router.navigate(['/lawyers']);
  }

  onCloseModal() {
    this.showCancelModal = false;
  }
}
