import {
    COUNTRIES,
    findCountryByDialCode,
    getFlagEmoji,
    getDialPrefix,
    Country
} from './phone-countries';

describe('Phone Countries', () => {
    describe('COUNTRIES array', () => {
        it('should contain Portugal', () => {
            const portugal = COUNTRIES.find(c => c.code === 'PT');
            expect(portugal).toBeTruthy();
            expect(portugal?.name).toBe('Portugal');
            expect(portugal?.dialCode).toBe(351);
        });

        it('should have unique country codes', () => {
            const codes = COUNTRIES.map(c => c.code);
            const uniqueCodes = new Set(codes);
            expect(codes.length).toBe(uniqueCodes.size);
        });

        it('should have all countries with dial codes', () => {
            COUNTRIES.forEach(country => {
                expect(country.dialCode).toBeGreaterThan(0);
            });
        });
    });

    describe('findCountryByDialCode', () => {
        it('should return null for null dial code', () => {
            expect(findCountryByDialCode(null)).toBeNull();
        });

        it('should return null for undefined dial code', () => {
            expect(findCountryByDialCode(undefined)).toBeNull();
        });

        it('should find country by numeric dial code', () => {
            const country = findCountryByDialCode(351);
            expect(country).toBeTruthy();
            expect(country?.code).toBe('PT');
        });

        it('should find country by string dial code', () => {
            const country = findCountryByDialCode('351');
            expect(country).toBeTruthy();
            expect(country?.code).toBe('PT');
        });

        it('should return null for non-existent dial code', () => {
            expect(findCountryByDialCode(99999)).toBeNull();
        });

        it('should handle zero dial code', () => {
            const result = findCountryByDialCode(0);
            expect(result).toBeNull(); // 0 is not a valid dial code in our list
        });
    });

    describe('getFlagEmoji', () => {
        it('should return empty string for null dial code', () => {
            expect(getFlagEmoji(null)).toBe('');
        });

        it('should return empty string for undefined dial code', () => {
            expect(getFlagEmoji(undefined)).toBe('');
        });

        it('should return flag emoji for valid dial code', () => {
            const flag = getFlagEmoji(351);
            expect(flag).toBe('🇵🇹');
        });

        it('should return empty string for non-existent dial code', () => {
            expect(getFlagEmoji(99999)).toBe('');
        });

        it('should work with string dial code', () => {
            const flag = getFlagEmoji('44');
            expect(flag).toBe('🇬🇧');
        });
    });

    describe('getDialPrefix', () => {
        it('should return empty string for null dial code', () => {
            expect(getDialPrefix(null)).toBe('');
        });

        it('should return empty string for undefined dial code', () => {
            expect(getDialPrefix(undefined)).toBe('');
        });

        it('should return formatted prefix for numeric dial code', () => {
            expect(getDialPrefix(351)).toBe('+351');
        });

        it('should return formatted prefix for string dial code', () => {
            expect(getDialPrefix('351')).toBe('+351');
        });

        it('should handle single digit dial codes', () => {
            expect(getDialPrefix(1)).toBe('+1');
        });

        it('should handle zero dial code', () => {
            expect(getDialPrefix(0)).toBe('+0');
        });
    });
});
