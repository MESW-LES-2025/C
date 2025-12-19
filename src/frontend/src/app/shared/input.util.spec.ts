import {
    sanitizeString,
    sanitizeModel,
    isValidRegisterNumber,
    isValidEmail,
    isValidPhone,
    isValidNif
} from './input.util';

describe('input.util', () => {
    describe('sanitizeString', () => {
        it('should remove control characters', () => {
            expect(sanitizeString('hello\x00world\x1F')).toBe('helloworld');
        });

        it('should normalize whitespace', () => {
            expect(sanitizeString('hello   world\n\n  test')).toBe('hello world test');
        });

        it('should trim string', () => {
            expect(sanitizeString('  hello  ')).toBe('hello');
        });

        it('should enforce max length', () => {
            const long = 'a'.repeat(3000);
            expect(sanitizeString(long, 100).length).toBe(100);
        });

        it('should handle null/undefined', () => {
            expect(sanitizeString(null)).toBe('');
            expect(sanitizeString(undefined)).toBe('');
        });

        it('should convert numbers to strings', () => {
            expect(sanitizeString(123)).toBe('123');
        });
    });

    describe('sanitizeModel', () => {
        it('should sanitize string fields', () => {
            const result = sanitizeModel({ name: '  John  ', age: 30 });
            expect(result['name']).toBe('John');
            expect(result['age']).toBe(30);
        });

        it('should keep numbers and booleans', () => {
            const result = sanitizeModel({ count: 42, active: true });
            expect(result['count']).toBe(42);
            expect(result['active']).toBe(true);
        });

        it('should skip null values', () => {
            const result = sanitizeModel({ name: 'John', middle: null });
            expect(result['name']).toBe('John');
            expect(result['middle']).toBeUndefined();
        });

        it('should stringify objects', () => {
            const result = sanitizeModel({ data: { nested: 'value' } });
            expect(typeof result['data']).toBe('string');
        });
    });

    describe('isValidRegisterNumber', () => {
        it('should accept valid register numbers', () => {
            expect(isValidRegisterNumber('ABC123')).toBeTrue();
            expect(isValidRegisterNumber('12-34/56')).toBeTrue();
            expect(isValidRegisterNumber('A')).toBeTrue();
        });

        it('should reject invalid register numbers', () => {
            expect(isValidRegisterNumber('')).toBeFalse();
            expect(isValidRegisterNumber('ABC@123')).toBeFalse();
            expect(isValidRegisterNumber('12345678901')).toBeFalse();
        });
    });

    describe('isValidEmail', () => {
        it('should accept valid emails', () => {
            expect(isValidEmail('test@example.com')).toBeTrue();
            expect(isValidEmail('user.name@domain.co.uk')).toBeTrue();
        });

        it('should reject invalid emails', () => {
            expect(isValidEmail('notanemail')).toBeFalse();
            expect(isValidEmail('@example.com')).toBeFalse();
            expect(isValidEmail('user@')).toBeFalse();
            expect(isValidEmail('')).toBeFalse();
        });
    });

    describe('isValidPhone', () => {
        it('should accept valid 9-digit phone numbers', () => {
            expect(isValidPhone('123456789')).toBeTrue();
            expect(isValidPhone('987654321')).toBeTrue();
        });

        it('should reject invalid phone numbers', () => {
            expect(isValidPhone('12345678')).toBeFalse();
            expect(isValidPhone('1234567890')).toBeFalse();
            expect(isValidPhone('12345678a')).toBeFalse();
            expect(isValidPhone('')).toBeFalse();
        });
    });

    describe('isValidNif', () => {
        it('should accept valid 9-digit NIFs', () => {
            expect(isValidNif('123456789')).toBeTrue();
            expect(isValidNif('987654321')).toBeTrue();
        });

        it('should reject invalid NIFs', () => {
            expect(isValidNif('12345678')).toBeFalse();
            expect(isValidNif('1234567890')).toBeFalse();
            expect(isValidNif('12345678a')).toBeFalse();
            expect(isValidNif('')).toBeFalse();
        });
    });
});
