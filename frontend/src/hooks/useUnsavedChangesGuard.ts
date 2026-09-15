import { useEffect } from 'react';
import { useBlocker } from 'react-router-dom';

export function useUnsavedChangesGuard(enabled: boolean, message: string) {
  useEffect(() => {
    if (!enabled) return;

    function handleBeforeUnload(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = message;
    }

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [enabled, message]);

  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      enabled && currentLocation.pathname !== nextLocation.pathname,
  );

  useEffect(() => {
    if (blocker.state !== 'blocked') return;

    const leave = window.confirm(message);
    if (leave) {
      blocker.proceed();
    } else {
      blocker.reset();
    }
  }, [blocker, message]);
}
