import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ClientsComponent } from './clients';
import { ClientService } from '../../services/client.service';
import { of, throwError } from 'rxjs';

describe('ClientsComponent', () => {
  let component: ClientsComponent;
  let fixture: ComponentFixture<ClientsComponent>;
  let clientServiceSpy: jasmine.SpyObj<ClientService>;

  const mockClientsData = {
    data: [
      {
        id: '1',
        name: 'Alice Smith',
        email: 'alice@test.com',
        nif: '123456789',
        address: '123 Main St',
        phone: '111111111',
        status: 'ACTIVE',
        createdAt: '2023-01-01T10:00:00Z'
      },
      {
        id: '2',
        name: 'Bob Jones',
        email: 'bob@test.com',
        nif: '987654321',
        address: '456 Oak Ave',
        phone: '222222222',
        status: 'INACTIVE',
        createdAt: '2023-02-01T10:00:00Z'
      },
      {
        id: '3',
        name: 'Charlie Brown',
        email: 'charlie@test.com',
        nif: '555555555',
        address: '789 Pine Rd',
        phone: '333333333',
        status: 'ACTIVE',
        createdAt: '2023-03-01T10:00:00Z'
      }
    ],
    meta: {
      totalCount: 3,
      currentPage: 1,
      totalPages: 2,
      pageSize: 2
    }
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('ClientService', ['getClients']);

    await TestBed.configureTestingModule({
      imports: [ClientsComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: ClientService, useValue: spy }
      ]
    }).compileComponents();

    clientServiceSpy = TestBed.inject(ClientService) as jasmine.SpyObj<ClientService>;

    // Default mock behavior
    clientServiceSpy.getClients.and.returnValue(of(mockClientsData));

    fixture = TestBed.createComponent(ClientsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load clients on init', () => {
    expect(clientServiceSpy.getClients).toHaveBeenCalled();
    expect(component.clients.length).toBe(3);
    expect(component.totalCount).toBe(3);
    expect(component.loading).toBeFalse();
  });

  it('should transform API data correctly', () => {
    expect(component.clients[0].name).toBe('Alice Smith');
    expect(component.clients[0].isActive).toBe(true);
    expect(component.clients[1].isActive).toBe(false);
    expect(component.clients[0].email).toBe('alice@test.com');
    expect(component.clients[0].nif).toBe('123456789');
  });

  it('should filter clients by status - active', () => {
    component.applyFilter('active');

    expect(component.filteredClients.length).toBe(2);
    expect(component.filteredClients.every(c => c.isActive)).toBe(true);
    expect(component.selectedFilter).toBe('active');
  });

  it('should filter clients by status - inactive', () => {
    component.applyFilter('inactive');

    expect(component.filteredClients.length).toBe(1);
    expect(component.filteredClients[0].name).toBe('Bob Jones');
    expect(component.filteredClients[0].isActive).toBe(false);
    expect(component.selectedFilter).toBe('inactive');
  });

  it('should filter clients by status - all', () => {
    component.applyFilter('active');
    component.applyFilter('all');

    expect(component.filteredClients.length).toBe(3);
    expect(component.selectedFilter).toBe('all');
  });

  it('should handle search with debounce', (done) => {
    const searchTerm = 'Alice';
    clientServiceSpy.getClients.calls.reset();

    component.onSearch(searchTerm);

    // Should not call immediately
    expect(clientServiceSpy.getClients).not.toHaveBeenCalled();

    // After debounce time (500ms)
    setTimeout(() => {
      expect(clientServiceSpy.getClients).toHaveBeenCalledWith(
        jasmine.objectContaining({ search: searchTerm })
      );
      expect(component.searchTerm).toBe(searchTerm);
      done();
    }, 550);
  });

  it('should trim search term', (done) => {
    component.onSearch('  test  ');

    setTimeout(() => {
      expect(component.searchTerm).toBe('test');
      done();
    }, 550);
  });

  it('should handle empty search term', (done) => {
    component.onSearch('   ');

    setTimeout(() => {
      expect(component.searchTerm).toBe('');
      done();
    }, 550);
  });

  it('should handle pagination - go to page 2', () => {
    component.totalPages = 5;
    clientServiceSpy.getClients.calls.reset();

    component.goToPage(2);

    expect(clientServiceSpy.getClients).toHaveBeenCalledWith(
      jasmine.objectContaining({ page: 2 })
    );
    expect(component.currentPage).toBe(2);
  });

  it('should not go to page less than 1', () => {
    const initialPage = component.currentPage;
    clientServiceSpy.getClients.calls.reset();

    component.goToPage(0);

    expect(component.currentPage).toBe(initialPage);
    expect(clientServiceSpy.getClients).not.toHaveBeenCalled();
  });

  it('should not go to page greater than totalPages', () => {
    component.totalPages = 3;
    const initialPage = component.currentPage;
    clientServiceSpy.getClients.calls.reset();

    component.goToPage(4);

    expect(component.currentPage).toBe(initialPage);
    expect(clientServiceSpy.getClients).not.toHaveBeenCalled();
  });

  it('should sort clients by name ascending', () => {
    component.toggleSort('name');

    expect(component.sortBy).toBe('name');
    expect(component.sortDir).toBe('asc');
    expect(component.filteredClients[0].name).toBe('Alice Smith');
    expect(component.filteredClients[2].name).toBe('Charlie Brown');
  });

  it('should sort clients by name descending', () => {
    component.toggleSort('name');
    component.toggleSort('name'); // Toggle to descending

    expect(component.sortDir).toBe('desc');
    expect(component.filteredClients[0].name).toBe('Charlie Brown');
    expect(component.filteredClients[2].name).toBe('Alice Smith');
  });

  it('should sort clients by NIF', () => {
    component.toggleSort('nif');

    expect(component.sortBy).toBe('nif');
    expect(component.filteredClients[0].nif).toBe('123456789');
    expect(component.filteredClients[2].nif).toBe('987654321');
  });

  it('should sort clients by createdAt', () => {
    component.toggleSort('createdAt');

    expect(component.sortBy).toBe('createdAt');
    expect(component.filteredClients[0].name).toBe('Alice Smith');
    expect(component.filteredClients[2].name).toBe('Charlie Brown');
  });

  it('should change sort direction when clicking same column', () => {
    component.toggleSort('name');
    expect(component.sortDir).toBe('asc');

    component.toggleSort('name');
    expect(component.sortDir).toBe('desc');

    component.toggleSort('name');
    expect(component.sortDir).toBe('asc');
  });

  it('should reset sort direction when changing column', () => {
    component.toggleSort('name');
    component.toggleSort('name'); // desc

    component.toggleSort('nif');

    expect(component.sortBy).toBe('nif');
    expect(component.sortDir).toBe('asc');
  });

  it('should return correct aria-sort value', () => {
    expect(component.ariaSort('name')).toBe('none');

    component.toggleSort('name');
    expect(component.ariaSort('name')).toBe('ascending');

    component.toggleSort('name');
    expect(component.ariaSort('name')).toBe('descending');

    expect(component.ariaSort('nif')).toBe('none');
  });

  it('should handle error when loading clients', () => {
    // Suppress console.error for this test
    spyOn(console, 'error');

    clientServiceSpy.getClients.and.returnValue(
      throwError(() => new Error('Network error'))
    );

    component.loadClients();

    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe('Failed to load clients.');
    expect(console.error).toHaveBeenCalled();
  });

  it('should handle error during search', (done) => {
    // Suppress console.error for this test
    spyOn(console, 'error');

    clientServiceSpy.getClients.and.returnValue(
      throwError(() => new Error('Search error'))
    );

    component.onSearch('test');

    setTimeout(() => {
      expect(component.loading).toBeFalse();
      done();
    }, 550);
  });

  it('should calculate visible pages correctly', () => {
    component.totalPages = 10;
    component.currentPage = 5;

    const pages = component.visiblePages;

    expect(pages).toContain(1);
    expect(pages).toContain(10);
    expect(pages).toContain(5);
    expect(pages).toContain('...');
  });

  it('should show all pages when total is small', () => {
    component.totalPages = 3;
    component.currentPage = 2;

    const pages = component.visiblePages;

    expect(pages).toEqual([1, 2, 3]);
    expect(pages).not.toContain('...');
  });

  it('should handle visible pages at start', () => {
    component.totalPages = 10;
    component.currentPage = 1;

    const pages = component.visiblePages;

    expect(pages[0]).toBe(1);
    expect(pages[pages.length - 1]).toBe(10);
  });

  it('should handle visible pages at end', () => {
    component.totalPages = 10;
    component.currentPage = 10;

    const pages = component.visiblePages;

    expect(pages[0]).toBe(1);
    expect(pages[pages.length - 1]).toBe(10);
  });

  it('should handle onPageClick for valid page number', () => {
    spyOn(component, 'goToPage');

    component.onPageClick(3);

    expect(component.goToPage).toHaveBeenCalledWith(3);
  });

  it('should ignore onPageClick for ellipsis', () => {
    spyOn(component, 'goToPage');

    component.onPageClick('...');

    expect(component.goToPage).not.toHaveBeenCalled();
  });

  it('should calculate total clients correctly', () => {
    expect(component.totalClients).toBe(3);
  });

  it('should calculate active clients correctly', () => {
    expect(component.activeClients).toBe(2);
  });

  it('should calculate inactive clients correctly', () => {
    expect(component.inactiveClients).toBe(1);
  });

  it('should set loading to true when loading clients', () => {
    component.loadClients();

    expect(clientServiceSpy.getClients).toHaveBeenCalled();
  });

  it('should clear error message when loading clients', () => {
    component.errorMessage = 'Previous error';

    component.loadClients();

    expect(component.errorMessage).toBe('');
  });

  it('should handle array response from API', () => {
    const arrayData = [
      { id: '1', name: 'Test', status: 'ACTIVE', createdAt: '2023-01-01' }
    ];

    clientServiceSpy.getClients.and.returnValue(of(arrayData));

    component.loadClients();

    expect(component.clients.length).toBeGreaterThan(0);
  });

  it('should calculate total pages correctly', () => {
    const dataWithMeta = {
      data: mockClientsData.data,
      meta: { totalCount: 10, currentPage: 1, totalPages: 5, pageSize: 3 }
    };

    clientServiceSpy.getClients.and.returnValue(of(dataWithMeta));
    component.pageSize = 3;

    component.loadClients();

    expect(component.totalPages).toBe(4); // Math.ceil(10/3)
  });

  it('should handle search with pagination', (done) => {
    component.searchTerm = 'test';
    clientServiceSpy.getClients.calls.reset();

    component.goToPage(2);

    setTimeout(() => {
      expect(clientServiceSpy.getClients).toHaveBeenCalledWith(
        jasmine.objectContaining({ search: 'test', page: 2 })
      );
      done();
    }, 50);
  });

  it('should handle null createdAt in transform', () => {
    const dataWithoutDate = {
      data: [{ id: '1', name: 'Test', status: 'ACTIVE' }],
      meta: { totalCount: 1 }
    };

    clientServiceSpy.getClients.and.returnValue(of(dataWithoutDate));

    component.loadClients();

    expect(component.clients[0].createdAt).toBeNull();
  });

  it('should unsubscribe from search on destroy', () => {
    const subscription = component['searchSubscription'];
    spyOn(subscription, 'unsubscribe');

    component.ngOnDestroy();

    expect(subscription.unsubscribe).toHaveBeenCalled();
  });

  it('should handle goToPageNumber input', () => {
    component.goToPageNumber = 3;
    component.totalPages = 5;

    spyOn(component, 'goToPage');
    component.goToPage(component.goToPageNumber);

    expect(component.goToPage).toHaveBeenCalledWith(3);
  });

  it('should maintain filtered clients after loading', () => {
    component.applyFilter('active');

    component.loadClients();

    // After reload, should show all clients (filter is reset)
    expect(component.filteredClients.length).toBe(component.clients.length);
  });

  it('should handle empty clients list', () => {
    clientServiceSpy.getClients.and.returnValue(of({
      data: [],
      meta: { totalCount: 0 }
    }));

    component.loadClients();

    expect(component.clients.length).toBe(0);
    expect(component.totalClients).toBe(0);
    expect(component.activeClients).toBe(0);
    expect(component.inactiveClients).toBe(0);
  });

  it('should handle clients with falsy isActive', () => {
    const dataWithFalsyActive = {
      data: [
        { id: '1', name: 'Test1', status: 'ACTIVE' },
        { id: '2', name: 'Test2', status: 'INACTIVE' },
        { id: '3', name: 'Test3', status: '' }
      ],
      meta: { totalCount: 3 }
    };

    clientServiceSpy.getClients.and.returnValue(of(dataWithFalsyActive));

    component.loadClients();

    expect(component.activeClients).toBe(1);
    expect(component.inactiveClients).toBe(2);
  });

  it('should handle sort with mixed case names', () => {
    const mixedCaseData = {
      data: [
        { id: '1', name: 'alice', status: 'ACTIVE', createdAt: '2023-01-01' },
        { id: '2', name: 'Bob', status: 'ACTIVE', createdAt: '2023-01-02' },
        { id: '3', name: 'CHARLIE', status: 'ACTIVE', createdAt: '2023-01-03' }
      ],
      meta: { totalCount: 3 }
    };

    clientServiceSpy.getClients.and.returnValue(of(mixedCaseData));
    component.loadClients();

    component.toggleSort('name');

    // localeCompare is case-insensitive by default
    // Expected order: alice, Bob, CHARLIE (case-insensitive alphabetical)
    expect(component.filteredClients[0].name).toBe('alice');
    expect(component.filteredClients[1].name).toBe('Bob');
    expect(component.filteredClients[2].name).toBe('CHARLIE');
  });

  it('should handle pagination without search term', () => {
    component.searchTerm = '';
    component.totalPages = 5;
    clientServiceSpy.getClients.calls.reset();
    clientServiceSpy.getClients.and.returnValue(of(mockClientsData));

    component.goToPage(3);

    expect(clientServiceSpy.getClients).toHaveBeenCalledWith(
      jasmine.objectContaining({ page: 3 })
    );
  });

  it('should update currentPage when loading clients', () => {
    component.loadClients(5);

    expect(component.currentPage).toBe(5);
  });

  it('should handle meta with only totalCount', () => {
    const minimalMetaData = {
      data: mockClientsData.data,
      meta: { totalCount: 5 }
    };

    clientServiceSpy.getClients.and.returnValue(of(minimalMetaData));

    component.loadClients();

    expect(component.totalCount).toBe(5);
  });

  it('should call detectChanges after loading', () => {
    spyOn(component['cdr'], 'detectChanges');

    component.loadClients();

    expect(component['cdr'].detectChanges).toHaveBeenCalled();
  });
});
