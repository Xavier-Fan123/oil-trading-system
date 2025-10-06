/**
 * Date utilities for standardized date handling across the Oil Trading System
 * 
 * This module provides consistent date parsing, formatting, and validation
 * for communication between frontend and backend APIs.
 * 
 * Standard Format: ISO 8601 (YYYY-MM-DDTHH:mm:ss.sssZ)
 */

/**
 * Parse a date string from API response to a Date object
 * Handles ISO 8601 format and various fallback formats
 * 
 * @param dateString - Date string from API (expected ISO 8601)
 * @returns Date object or null if invalid
 */
export function parseApiDate(dateString: string | null | undefined): Date | null {
  if (!dateString) {
    return null;
  }

  // Try to parse as ISO 8601 first
  try {
    const date = new Date(dateString);
    
    // Check if the date is valid
    if (isNaN(date.getTime())) {
      console.warn(`Invalid date string received from API: ${dateString}`);
      return null;
    }
    
    return date;
  } catch (error) {
    console.error(`Failed to parse date string: ${dateString}`, error);
    return null;
  }
}

/**
 * Format a Date object for API requests (ISO 8601 format)
 * 
 * @param date - Date object to format
 * @returns ISO 8601 formatted string or null if invalid
 */
export function formatApiDate(date: Date | null | undefined): string | null {
  if (!date || !isValidDate(date)) {
    return null;
  }

  try {
    // Return ISO 8601 format with timezone
    return date.toISOString();
  } catch (error) {
    console.error('Failed to format date for API:', error);
    return null;
  }
}

/**
 * Format a Date object for display in the UI
 * Uses locale-specific formatting with sensible defaults
 * 
 * @param date - Date object to format
 * @param options - Intl.DateTimeFormat options
 * @returns Formatted date string for display
 */
export function formatDisplayDate(
  date: Date | null | undefined,
  options: Intl.DateTimeFormatOptions = {}
): string {
  if (!date || !isValidDate(date)) {
    return '-';
  }

  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    ...options
  };

  try {
    return new Intl.DateTimeFormat('en-US', defaultOptions).format(date);
  } catch (error) {
    console.error('Failed to format date for display:', error);
    return date.toLocaleDateString();
  }
}

/**
 * Format a Date object for display with time
 * 
 * @param date - Date object to format
 * @param options - Intl.DateTimeFormat options
 * @returns Formatted datetime string for display
 */
export function formatDisplayDateTime(
  date: Date | null | undefined,
  options: Intl.DateTimeFormatOptions = {}
): string {
  if (!date || !isValidDate(date)) {
    return '-';
  }

  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    ...options
  };

  try {
    return new Intl.DateTimeFormat('en-US', defaultOptions).format(date);
  } catch (error) {
    console.error('Failed to format datetime for display:', error);
    return date.toLocaleString();
  }
}

/**
 * Validate if a value is a valid Date object
 * 
 * @param date - Value to validate
 * @returns True if valid Date object
 */
export function isValidDate(date: any): date is Date {
  return date instanceof Date && !isNaN(date.getTime());
}

/**
 * Convert a date to UTC timezone for API consistency
 * 
 * @param date - Date object
 * @returns New Date object in UTC
 */
export function toUtcDate(date: Date): Date {
  if (!isValidDate(date)) {
    throw new Error('Invalid date provided to toUtcDate');
  }

  return new Date(date.getTime() + (date.getTimezoneOffset() * 60000));
}

/**
 * Convert a UTC date to local timezone
 * 
 * @param utcDate - UTC Date object
 * @returns New Date object in local timezone
 */
export function fromUtcDate(utcDate: Date): Date {
  if (!isValidDate(utcDate)) {
    throw new Error('Invalid date provided to fromUtcDate');
  }

  return new Date(utcDate.getTime() - (utcDate.getTimezoneOffset() * 60000));
}

/**
 * Parse date fields in an API response object
 * Recursively processes all string fields that look like dates
 * 
 * @param obj - Object with potential date fields
 * @param dateFields - Array of field names that contain dates
 * @returns New object with parsed dates
 */
export function parseApiDateFields<T extends Record<string, any>>(
  obj: T,
  dateFields: readonly string[]
): T {
  if (!obj || typeof obj !== 'object') {
    return obj;
  }

  const result = { ...obj };

  for (const field of dateFields) {
    if (field in result && typeof result[field] === 'string') {
      const parsedDate = parseApiDate(result[field]);
      if (parsedDate) {
        (result as any)[field] = parsedDate;
      }
    }
  }

  return result;
}

/**
 * Format date fields in an object for API requests
 * 
 * @param obj - Object with Date fields
 * @param dateFields - Array of field names that contain dates
 * @returns New object with formatted date strings
 */
export function formatApiDateFields<T extends Record<string, any>>(
  obj: T,
  dateFields: readonly string[]
): T {
  if (!obj || typeof obj !== 'object') {
    return obj;
  }

  const result = { ...obj };

  for (const field of dateFields) {
    if (field in result) {
      const value = result[field];
      if (isValidDate(value)) {
        const formattedDate = formatApiDate(value);
        if (formattedDate) {
          (result as any)[field] = formattedDate;
        }
      }
    }
  }

  return result;
}

/**
 * Get the start of day for a given date
 * 
 * @param date - Date object
 * @returns New Date object at start of day (00:00:00.000)
 */
export function getStartOfDay(date: Date): Date {
  if (!isValidDate(date)) {
    throw new Error('Invalid date provided to getStartOfDay');
  }

  const result = new Date(date);
  result.setHours(0, 0, 0, 0);
  return result;
}

/**
 * Get the end of day for a given date
 * 
 * @param date - Date object
 * @returns New Date object at end of day (23:59:59.999)
 */
export function getEndOfDay(date: Date): Date {
  if (!isValidDate(date)) {
    throw new Error('Invalid date provided to getEndOfDay');
  }

  const result = new Date(date);
  result.setHours(23, 59, 59, 999);
  return result;
}

/**
 * Add days to a date
 * 
 * @param date - Base date
 * @param days - Number of days to add (can be negative)
 * @returns New Date object
 */
export function addDays(date: Date, days: number): Date {
  if (!isValidDate(date)) {
    throw new Error('Invalid date provided to addDays');
  }

  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/**
 * Check if two dates are the same day (ignoring time)
 * 
 * @param date1 - First date
 * @param date2 - Second date
 * @returns True if same day
 */
export function isSameDay(date1: Date, date2: Date): boolean {
  if (!isValidDate(date1) || !isValidDate(date2)) {
    return false;
  }

  return (
    date1.getFullYear() === date2.getFullYear() &&
    date1.getMonth() === date2.getMonth() &&
    date1.getDate() === date2.getDate()
  );
}

/**
 * Get timezone offset in ISO format (+HH:mm or -HH:mm)
 * 
 * @param date - Date object (optional, defaults to now)
 * @returns Timezone offset string
 */
export function getTimezoneOffset(date: Date = new Date()): string {
  const offset = -date.getTimezoneOffset();
  const hours = Math.floor(Math.abs(offset) / 60);
  const minutes = Math.abs(offset) % 60;
  const sign = offset >= 0 ? '+' : '-';
  
  return `${sign}${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
}

/**
 * Default date field names commonly used in the Oil Trading System
 */
export const COMMON_DATE_FIELDS = [
  'createdAt',
  'updatedAt',
  'laycanStart',
  'laycanEnd',
  'pricingPeriodStart',
  'pricingPeriodEnd',
  'eventDate',
  'timestamp',
  'lastUsedAt',
  'assignedAt',
  'approvedAt',
  'rejectedAt',
  'completedAt',
  'cancelledAt'
] as const;

/**
 * Type for common date field names
 */
export type CommonDateField = typeof COMMON_DATE_FIELDS[number];