import { useCallback, useEffect, useRef, useState } from 'react';

/**
 * Širina elementa u pikselima, praćena kroz promene veličine. Grafikoni se
 * crtaju u stvarnoj širini (umesto skaliranog viewBox-a), pa tekst i debljine
 * linija ostaju u pravim veličinama na svakom ekranu.
 *
 * Vraća callback ref, pa praćenje počinje i kada se element pojavi kasnije
 * (npr. posle praznog stanja).
 */
export function useElementWidth<T extends HTMLElement>(fallback = 640) {
  const [node, setNode] = useState<T | null>(null);
  const [width, setWidth] = useState(fallback);
  const nodeRef = useRef<T | null>(null);

  const ref = useCallback((element: T | null) => {
    nodeRef.current = element;
    setNode(element);
  }, []);

  useEffect(() => {
    if (!node) return;
    const update = () => {
      const next = Math.round(node.clientWidth);
      if (next > 0) setWidth(next);
    };
    update();
    const observer = new ResizeObserver(update);
    observer.observe(node);
    return () => observer.disconnect();
  }, [node]);

  return { ref, width, nodeRef };
}
