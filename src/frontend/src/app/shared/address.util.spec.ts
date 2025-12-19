import { formatAddress, parseAddress, AddressParts } from './address.util';

describe('address.util', () => {
    describe('formatAddress', () => {
        it('should format complete address', () => {
            const parts: AddressParts = {
                street: '123 Main St',
                cityState: 'New York, NY',
                country: 'USA',
                zip: '10001'
            };
            expect(formatAddress(parts)).toBe('123 Main St; New York, NY; USA — 10001');
        });

        it('should handle missing street', () => {
            const parts: AddressParts = {
                cityState: 'New York, NY',
                country: 'USA',
                zip: '10001'
            };
            expect(formatAddress(parts)).toBe('New York, NY; USA — 10001');
        });

        it('should handle missing zip', () => {
            const parts: AddressParts = {
                street: '123 Main St',
                cityState: 'New York, NY',
                country: 'USA'
            };
            expect(formatAddress(parts)).toBe('123 Main St; New York, NY; USA');
        });

        it('should handle empty parts', () => {
            expect(formatAddress({})).toBe('');
        });

        it('should handle only zip', () => {
            const parts: AddressParts = { zip: '10001' };
            expect(formatAddress(parts)).toBe('10001');
        });

        it('should trim whitespace', () => {
            const parts: AddressParts = {
                street: '  123 Main St  ',
                zip: '  10001  '
            };
            expect(formatAddress(parts)).toBe('123 Main St — 10001');
        });
    });

    describe('parseAddress', () => {
        it('should parse complete address', () => {
            const result = parseAddress('123 Main St; New York, NY; USA — 10001');
            expect(result).toEqual({
                street: '123 Main St',
                cityState: 'New York, NY',
                country: 'USA',
                zip: '10001'
            });
        });

        it('should parse address without zip', () => {
            const result = parseAddress('123 Main St; New York, NY; USA');
            expect(result).toEqual({
                street: '123 Main St',
                cityState: 'New York, NY',
                country: 'USA',
                zip: ''
            });
        });

        it('should handle empty string', () => {
            const result = parseAddress('');
            expect(result).toEqual({});
        });

        it('should handle only zip', () => {
            const result = parseAddress(' — 10001');
            expect(result).toEqual({
                street: '',
                cityState: '',
                country: '',
                zip: '10001'
            });
        });

        it('should parse partial address', () => {
            const result = parseAddress('123 Main St — 10001');
            expect(result).toEqual({
                street: '123 Main St',
                cityState: '',
                country: '',
                zip: '10001'
            });
        });
    });
});
