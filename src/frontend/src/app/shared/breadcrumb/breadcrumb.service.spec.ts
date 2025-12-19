import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { BreadcrumbService } from './breadcrumb.service';

describe('BreadcrumbService', () => {
    let service: BreadcrumbService;

    beforeEach(() => {
        const routerSpy = {
            events: { pipe: () => ({ subscribe: () => { } }) },
            url: '/test',
            config: []
        };
        const routeSpy = {
            root: { children: [] }
        };

        TestBed.configureTestingModule({
            providers: [
                provideZonelessChangeDetection(),
                { provide: Router, useValue: routerSpy },
                { provide: ActivatedRoute, useValue: routeSpy }
            ]
        });
        service = TestBed.inject(BreadcrumbService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should set label override', () => {
        service.setLabelOverride('/test', 'Custom Label');
        expect(service['overrides'].get('/test')).toBe('Custom Label');
    });

    it('should clear label override', () => {
        service.setLabelOverride('/test', 'Label');
        service.clearLabelOverride('/test');
        expect(service['overrides'].has('/test')).toBeFalse();
    });
});
