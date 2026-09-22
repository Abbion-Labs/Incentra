import { useCallback, useEffect, useRef } from 'react';
import { useBlocker } from 'react-router-dom';

export function useUnsavedChangesGuard(enabled: boolean, message: string) {
  const enabledRef = useRef(enabled);
  enabledRef.current = enabled;

  // Set right before a programmatic navigate() that follows a successful save.
  // React has not re-rendered yet at that point, so the blocker would still see
  // the stale "dirty" flag and warn about changes that are already persisted.
  const skipNextNavigation = useRef(false);

  const allowNextNavigation = useCallback(() => {
    skipNextNavigation.current = true;
  }, []);

  useEffect(() => {
    if (!enabled) return;

    function handleBeforeUnload(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = message;
    }

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [enabled, message]);

  const blocker = useBlocker(({ currentLocation, nextLocation }) => {
    if (currentLocation.pathname === nextLocation.pathname) return false;
    if (skipNextNavigation.current) {
      skipNextNavigation.current = false;
      return false;
    }
    return enabledRef.current;
  });

  const confirmedKey = useRef<string | null>(null);

  useEffect(() => {
    if (blocker.state !== 'blocked') {
      confirmedKey.current = null;
      return;
    }
    if (confirmedKey.current === blocker.location.key) return;
    confirmedKey.current = blocker.location.key;

    if (window.confirm(message)) {
      blocker.proceed();
    } else {
      blocker.reset();
    }
  }, [blocker, message]);

  return { allowNextNavigation };
}
