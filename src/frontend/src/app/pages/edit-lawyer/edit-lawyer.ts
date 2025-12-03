import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';
import { PageTitleComponent } from '../../shared/page-title/page-title';
import { ButtonComponent } from '../../shared/button/button';
import { LawyerService } from '../../services/lawyer.service';
import { NotificationService } from '../../shared/notification/notification.service';
import { ConfirmModalComponent } from '../../shared/confirm-modal/confirm-modal';
import {
  sanitizeModel,
  sanitizeString,
  isValidEmail,
  isValidPhone,
  isValidNif,
  isValidRegisterNumber,
} from '../../shared/input.util';
import { Lawyer } from '../../models/lawyer';
import { PhoneInputComponent } from '../../shared/phone-input/phone-input';

@Component({
  selector: 'app-edit-lawyer',
  standalone: true,
  templateUrl: './edit-lawyer.html',
  styleUrls: ['./edit-lawyer.css', '../create-client/create-client.css'],
  imports: [
    CommonModule,
    FormsModule,
    PageTitleComponent,
    ButtonComponent,
    PhoneInputComponent,
    ConfirmModalComponent,
  ],
})
export class EditLawyerComponent implements OnInit {
  id: string | null = null;
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

  private originalModel: Record<string, any> = {};

  submitting = false;
  fieldErrors: Record<string, string[]> = {};
  generalError = '';
  showCancelModal = false;

  constructor(
    private route: ActivatedRoute,
    private lawyerService: LawyerService,
    private notifications: NotificationService,
    public router: Router,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    if (!this.id) {
      this.notifications.showError('No lawyer id provided.');
      return;
    }

    this.loadLawyer(this.id);
  }

  onActiveToggle(checked: boolean) {
    this.model.isActive = !!checked;
  }

  loadLawyer(id: string) {
    this.lawyerService.getLawyer(id).subscribe({
      next: (data: any) => {
        this.model.name = data?.name || '';
        this.model.email = data?.email || '';
        this.model.professionalRegister = data?.professionalRegister || '';
        this.model.nif = data?.nif || '';
        this.model.phone = data?.phone || '';
        this.model.phoneCountryCode = data?.phoneCountryCode ?? this.model.phoneCountryCode ?? 351;
        this.model.isActive = (data?.status || '').toString().toUpperCase() === 'ACTIVE';

        try {
          this.cd.detectChanges();
        } catch (e) {}

        this.originalModel = this.snapshotModel();
      },
      error: (err) => {
        console.error('Failed to load lawyer', err);
        this.notifications.showError('Failed to load lawyer.');
      },
    });
  }

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

  isPasswordValid(): boolean {
    const p = this.model.password || '';
    const c = this.model.confirmPassword || '';
    return p.length === 0 ? true : p.length >= 8 && p === c;
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

  private snapshotModel(): Record<string, any> {
    return {
      name: (this.model.name || '').toString().trim(),
      email: (this.model.email || '').toString().trim(),
      professionalRegister: (this.model.professionalRegister || '').toString().trim(),
      nif: (this.model.nif || '').toString().trim(),
      phone: (this.model.phone || '').toString().trim(),
      phoneCountryCode: this.model.phoneCountryCode ?? null,
      isActive: !!this.model.isActive,
      password: (this.model.password || '').toString(),
    };
  }

  hasChanges(): boolean {
    const current = this.snapshotModel();
    return JSON.stringify(current) !== JSON.stringify(this.originalModel || {});
  }

  save() {
    if (!this.isComplete() || !this.id || this.submitting) return;
    if (!this.isPasswordValid()) {
      this.notifications.showError('Password is required and must match confirmation.');
      return;
    }

    this.submitting = true;
    this.fieldErrors = {};
    this.generalError = '';

    const sanitized = sanitizeModel(this.model as Record<string, any>);
    const payload: any = { ...sanitized };

    // phone mapping
    if (sanitized['phone']) {
      payload.phoneNumber = sanitized['phone'];
      payload.phoneCountryCode = sanitized['phoneCountryCode'] || 351;
      payload.phoneIsMain = true;
      delete payload['phone'];
    }

    payload.isActive = !!this.model.isActive;

    this.lawyerService.updateLawyer(this.id, payload).subscribe({
      next: (_) => {
        this.notifications.showSuccess('Lawyer updated successfully');
        setTimeout(() => this.router.navigate(['/lawyers']), 600);
      },
      error: (err) => {
        console.error('Update lawyer failed', err);
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
          this.notifications.showError(this.generalError || 'Failed to update lawyer.');
        } else {
          this.notifications.showError('Failed to update lawyer.');
        }
        this.submitting = false;
      },
    });
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
