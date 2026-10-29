import { useLayoutEffect, useRef, type TextareaHTMLAttributes } from 'react';

/** Polje koje raste sa tekstom, umesto ručke za razvlačenje i skrolovanja. */
export function AutoGrowTextarea(
  props: TextareaHTMLAttributes<HTMLTextAreaElement>,
) {
  const ref = useRef<HTMLTextAreaElement>(null);

  useLayoutEffect(() => {
    const textarea = ref.current;
    if (!textarea) return;
    fitToContent(textarea);
  }, [props.value]);

  // Uža kolona prelama tekst u više redova, pa visina prati i širinu polja.
  useLayoutEffect(() => {
    const textarea = ref.current;
    if (!textarea || typeof ResizeObserver === 'undefined') return;
    let width = textarea.offsetWidth;
    const observer = new ResizeObserver(() => {
      if (textarea.offsetWidth === width) return;
      width = textarea.offsetWidth;
      fitToContent(textarea);
    });
    observer.observe(textarea);
    return () => observer.disconnect();
  }, []);

  return <textarea ref={ref} rows={1} {...props} />;
}

function fitToContent(textarea: HTMLTextAreaElement) {
  textarea.style.height = 'auto';
  const borders = textarea.offsetHeight - textarea.clientHeight;
  textarea.style.height = `${textarea.scrollHeight + borders}px`;
}
