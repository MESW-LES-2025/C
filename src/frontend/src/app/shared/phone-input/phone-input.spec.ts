import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PhoneInputComponent } from './phone-input';
import { COUNTRIES } from '../phone-countries/phone-countries';

describe('PhoneInputComponent', () => {
    let component: PhoneInputComponent;
    let fixture: ComponentFixture<PhoneInputComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [PhoneInputComponent],
            providers: [provideZonelessChangeDetection()]
        }).compileComponents();

        fixture = TestBed.createComponent(PhoneInputComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should open and close dropdown', () => {
        component.openDropdown();
        expect(component.dropdownOpen).toBeTrue();
        component.closeDropdown();
        expect(component.dropdownOpen).toBeFalse();
    });

    it('should select country', () => {
        const country = COUNTRIES[0];
        spyOn(component.countryChange, 'emit');
        component.selectCountry(country);
        expect(component.country).toBe(country);
        expect(component.countryChange.emit).toHaveBeenCalledWith(country.dialCode);
        expect(component.dropdownOpen).toBeFalse();
    });

    it('should emit number change', () => {
        spyOn(component.numberChange, 'emit');
        component.onNumberInput('123456789');
        expect(component.number).toBe('123456789');
        expect(component.numberChange.emit).toHaveBeenCalledWith('123456789');
    });

    it('should filter countries by search', () => {
        component.search = 'port';
        const filtered = component.filteredCountries();
        expect(filtered.length).toBeGreaterThan(0);
        expect(filtered.some(c => c.name.toLowerCase().includes('port'))).toBeTrue();
    });

    it('should emit formatted value', () => {
        spyOn(component.valueChange, 'emit');
        component.country = COUNTRIES.find(c => c.dialCode === 351) || null;
        component.number = '123456789';
        component.emitValue();
        expect(component.valueChange.emit).toHaveBeenCalledWith({
            dialCode: 351,
            number: '123456789',
            e164: '+351123456789'
        });
    });
});
