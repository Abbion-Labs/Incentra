import { useEffect, useRef, useState } from 'react';

export function useAnalyticsChartContainerHeight(minHeight = 280) {
  const ref = useRef<HTMLDivElement>(null);
  const [height, setHeight] = useState(minHeight);
  const [width, setWidth] = useState(0);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;

    const update = () => {
      const nextHeight = element.clientHeight;
      const nextWidth = element.clientWidth;
      if (nextHeight > 0) {
        setHeight(Math.max(minHeight, Math.round(nextHeight)));
      }
      if (nextWidth > 0) {
        setWidth(Math.round(nextWidth));
      }
    };

    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    return () => observer.disconnect();
  }, [minHeight]);

  return { ref, height, width };
}
