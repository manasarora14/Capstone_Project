export function pad(n: number) { return String(n).padStart(2, '0'); }

export function toUtcIso(date: Date): string {
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString();
}

export function timespanFromDate(date: Date): string {
  const hh = pad(date.getHours());
  const mm = pad(date.getMinutes());
  const ss = pad(date.getSeconds());
  return `${hh}:${mm}:${ss}`;
}

export function addHoursIso(iso: string, hours: number): string {
  const d = new Date(iso);
  d.setHours(d.getHours() + hours);
  return d.toISOString();
}

export function addHoursDisplay(iso: string, hours: number): string {
  const d = new Date(iso);
  d.setHours(d.getHours() + hours);
  return d.toLocaleString();
}
