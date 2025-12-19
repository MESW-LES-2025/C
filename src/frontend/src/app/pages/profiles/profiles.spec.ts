import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ProfilesComponent } from './profiles';

describe('ProfilesComponent', () => {
    let component: ProfilesComponent;
    let fixture: ComponentFixture<ProfilesComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ProfilesComponent],
            providers: [provideZonelessChangeDetection()]
        }).compileComponents();

        fixture = TestBed.createComponent(ProfilesComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
