import { useCallback, useEffect, useRef, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useIntl } from '../i18n';
import { resolvePageRole } from '../utils/homeNavigation';
import { roleLabel } from '../utils/status';

interface Props {
  children: React.ReactNode;
  roles?: string[];
}

/** Stanje navigacije kojim prekidač uloga kaže da korisnik traži tu ulogu. */
export interface ActiveRoleNavigationState {
  switchingTo?: string;
}

/**
 * Pušta na stranicu samo prijavljenog korisnika čija sesija radi u ulozi za
 * koju je stranica. Ako je stranica za neku drugu njegovu ulogu, prelazi u nju
 * jednom, kada je korisnik sam tražio prekidačem, a inače nudi prelazak. Ne
 * prelazi sam, jer svi tabovi dele sesiju, pa bi se dva taba nadmetala oko
 * uloge.
 */
export function ProtectedRoute({ children, roles }: Props) {
  const { user, activeRole, selectRole } = useAuth();
  const { formatMessage } = useIntl();
  const location = useLocation();
  const [switching, setSwitching] = useState(false);
  const [failed, setFailed] = useState(false);
  const honoured = useRef<string | null>(null);

  const pageRole = user ? resolvePageRole(user.roles, roles, activeRole) : null;

  // Uloga koju je korisnik tražio prekidačem ima prednost nad ulogom stranice.
  // Admin sesija sme na svaku stranicu, pa bi bez ovoga prelazak iz nje samo
  // navigirao, a sesija bi ostala admin.
  const switchingTo = (location.state as ActiveRoleNavigationState | null)
    ?.switchingTo;
  const requestedRole =
    switchingTo && user?.roles.includes(switchingTo) ? switchingTo : null;

  const targetRole = requestedRole ?? pageRole;
  const needsSwitch = targetRole !== null && targetRole !== activeRole;

  // Zahtev prekidača važi jednom po navigaciji. Bez toga bi tab koji je jednom
  // tražio ulogu vraćao sebi svaki put kad je drugi tab promeni, pa bi se dva
  // taba nadmetala oko sesije.
  const asked =
    requestedRole !== null && !failed && honoured.current !== location.key;

  const switchTo = useCallback(
    async (role: string) => {
      setSwitching(true);
      setFailed(false);
      try {
        await selectRole(role);
      } catch {
        setFailed(true);
      } finally {
        setSwitching(false);
      }
    },
    [selectRole],
  );

  useEffect(() => {
    if (!needsSwitch || targetRole === null || !asked) return;
    // StrictMode pokreće efekat dvaput sa istim vrednostima, a ref preživi oba.
    if (honoured.current === location.key) return;
    honoured.current = location.key;
    void switchTo(targetRole);
  }, [asked, location.key, needsSwitch, targetRole, switchTo]);

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (roles && pageRole === null) {
    return <Navigate to="/" replace />;
  }

  if (needsSwitch && targetRole !== null) {
    const role = roleLabel(targetRole, formatMessage);

    if (switching || asked) {
      return (
        <div className="empty">
          {formatMessage({ id: 'roleSwitch.inProgress' })}
        </div>
      );
    }

    return (
      <div className="empty card role-switch-prompt">
        <p>{formatMessage({ id: 'roleSwitch.pageNeedsRole' }, { role })}</p>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => void switchTo(targetRole)}
        >
          {formatMessage({ id: 'roleSwitch.action' }, { role })}
        </button>
        {failed && (
          <p className="role-switch-prompt__error">
            {formatMessage({ id: 'roleSwitch.failed' })}
          </p>
        )}
      </div>
    );
  }

  return <>{children}</>;
}
