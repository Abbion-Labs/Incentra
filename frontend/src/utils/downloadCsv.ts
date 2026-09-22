const DEFAULT_DELIMITER = ';';

function escapeCsvCell(value: string): string {
  const normalized = String(value ?? '')
    .replace(/\r\n/g, '\n')
    .replace(/\r/g, '\n');
  return `"${normalized.replace(/"/g, '""')}"`;
}

/**
 * Downloads a CSV tuned for Excel in sr-RS locale:
 * - semicolon delimiter
 * - sep=; hint so Excel splits columns correctly
 * - all fields quoted (numbers use comma as decimal separator)
 * - UTF-8 BOM for special characters
 */
export function downloadCsv(
  filename: string,
  headers: string[],
  rows: string[][],
): void {
  const delimiter = DEFAULT_DELIMITER;
  const lines = [
    `sep=${delimiter}`,
    headers.map((cell) => escapeCsvCell(cell)).join(delimiter),
    ...rows.map((row) =>
      row.map((cell) => escapeCsvCell(String(cell ?? ''))).join(delimiter),
    ),
  ];
  const blob = new Blob([`\uFEFF${lines.join('\r\n')}`], {
    type: 'text/csv;charset=utf-8;',
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename.endsWith('.csv') ? filename : `${filename}.csv`;
  link.style.display = 'none';
  document.body.appendChild(link);
  link.click();
  setTimeout(() => {
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }, 200);
}
