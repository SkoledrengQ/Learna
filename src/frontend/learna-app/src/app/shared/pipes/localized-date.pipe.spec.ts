import { LocalizedDatePipe } from './localized-date.pipe';

describe('LocalizedDatePipe', () => {
  const pipe = new LocalizedDatePipe();
  const date = new Date(2026, 6, 15);

  it('uses Buddhist Era years in Thai', () => {
    expect(pipe.transform(date, 'th', 'd MMM y')).toContain('2569');
    expect(pipe.transform(date, 'th', 'fullDate')).toContain('2569');
  });

  it('keeps Gregorian years in English', () => {
    expect(pipe.transform(date, 'en', 'd MMM y')).toContain('2026');
  });
});
