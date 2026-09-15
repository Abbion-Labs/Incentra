import { useEffect, useState } from 'react';

export function useDebouncedSearch(initial = '', delayMs = 300) {
  const [input, setInput] = useState(initial);
  const [debounced, setDebounced] = useState(initial);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(input), delayMs);
    return () => window.clearTimeout(timer);
  }, [input, delayMs]);

  return { input, debounced, setInput };
}
