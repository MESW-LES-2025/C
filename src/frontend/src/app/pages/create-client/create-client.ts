import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ButtonComponent } from '../../shared/button/button';
import { PhoneInputComponent } from '../../shared/phone-input/phone-input';
import { ClientService } from '../../services/client.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal';
import { formatAddress } from '../../shared/address.util';
import {
  sanitizeModel,
  sanitizeString,
  isValidEmail,
  isValidPhone,
  isValidNif,
} from '../../shared/input.util';
import { Client } from '../../models/client';

@Component({
  selector: 'app-create-client',
  standalone: true,
  templateUrl: './create-client.html',
  styleUrls: ['./create-client.css'],
  imports: [
    CommonModule,
    FormsModule,
    PageTitleComponent,
    ButtonComponent,
    PhoneInputComponent,
    ConfirmModalComponent,
  ],
})
export class CreateClientComponent {
  model: Partial<
    Client & {
      password?: string;
      confirmPassword?: string;
      addressStreet?: string;
      addressCityState?: string;
      addressCountry?: string;
      addressZip?: string;
      phoneCountryCode?: number;
    }
  > = {
    name: '',
    email: '',
    nif: '',
    phone: '',
    phoneCountryCode: 351,
    // address parts (will be concatenated before sending)
    addressStreet: '',
    addressCityState: '',
    addressCountry: '',
    addressZip: '',
    isActive: true,
    password: '',
    confirmPassword: '',
  };

  submitting = false;
  // server-side validation errors per field
  fieldErrors: Record<string, string[]> = {};
  generalError = '';
  showCancelModal = false;

  constructor(
    private clientService: ClientService,
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

  createClient() {
    if (!this.isComplete() || this.submitting) return;
    if (!this.model.name) return;
    if (!this.isPasswordValid()) {
      this.notifications.showError('Password is required and must match confirmation.');
      return;
    }
    this.submitting = true;
    this.fieldErrors = {};
    this.generalError = '';

    // sanitize model strings first
    const sanitized = sanitizeModel(this.model as Record<string, any>);
    const payload: any = { ...sanitized };

    payload.address = formatAddress({
      street: sanitizeString(this.model.addressStreet || ''),
      cityState: sanitizeString(this.model.addressCityState || ''),
      country: sanitizeString(this.model.addressCountry || ''),
      zip: sanitizeString(this.model.addressZip || ''),
    });

    // Map frontend 'phone' to API expected 'phoneNumber' + optional metadata
    if (sanitized['phone']) {
      payload.phoneNumber = sanitized['phone'];
      payload.phoneCountryCode = sanitized['phoneCountryCode'] || 351;
      payload.phoneIsMain = true;
    }

    // remove transient fields
    delete payload.addressStreet;
    delete payload.addressCityState;
    delete payload.addressCountry;
    delete payload.addressZip;
    delete payload.confirmPassword;
    // remove the transient frontend-only field
    delete payload['phone'];
    this.clientService.createClient(payload).subscribe({
      next: (_) => {
        this.notifications.showSuccess('Client created successfully');
        setTimeout(() => this.router.navigate(['/clients']), 500);
      },
      error: (err) => {
        console.error('Create client failed', err);
        // Try to extract validation errors from the backend
        const body = err?.error || {};
        if (body && typeof body === 'object' && body.errors) {
          // Map backend errors to our fieldErrors object (lowercase keys)
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
          this.notifications.showError(this.generalError || 'Failed to create client.');
        } else {
          this.notifications.showError('Failed to create client.');
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

  /**
   * Return true when all visible fields are non-empty and password is valid.
   * "Everything" means the fields present on the form: name, email, nif,
   * phone, all address parts and a valid password + confirmation.
   */
  isComplete(): boolean {
    const s = (v?: string) => (v || '').toString().trim().length > 0;

    // basic email sanity check
    const email = (this.model.email || '').toString().trim();
    const emailOk = /\S+@\S+\.\S+/.test(email);

    return (
      s(this.model.name) &&
      isValidEmail(email) &&
      isValidNif((this.model.nif || '').toString()) &&
      isValidPhone((this.model.phone || '').toString()) &&
      s(this.model.addressStreet) &&
      s(this.model.addressCityState) &&
      s(this.model.addressCountry) &&
      s(this.model.addressZip) &&
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

  onCancelClick() {
    this.showCancelModal = true;
  }

  onConfirmCancel() {
    this.showCancelModal = false;
    this.router.navigate(['/clients']);
  }

  onCloseModal() {
    this.showCancelModal = false;
  }
}
