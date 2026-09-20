import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type MouseEvent,
  type ReactNode,
} from 'react';
import { createPortal } from 'react-dom';
import type { EvaluationSummary } from '../../api/types';
import { useIntl } from '../../i18n';
import { useEvaluationStatusHistory, useToast } from '../../hooks';
import { isReturnedEvaluation } from '../../utils/evaluationBuckets';
import { EvaluationStatusHistoryPanel } from './EvaluationStatusHistoryPanel';

const POPOVER_WIDTH = 320;
const VIEWPORT_PADDING = 8;
const POPOVER_GAP = 6;

function isHoverCapable(): boolean {
  if (typeof window === 'undefined') return true;
  return window.matchMedia('(hover: hover) and (pointer: fine)').matches;
}

interface EvaluationStatusWithHistoryProps {
  evaluationId: number;
  evaluation?: Pick<EvaluationSummary, 'status' | 'goalCount' | 'controllerComment'>;
  children: ReactNode;
}

export function EvaluationStatusWithHistory({
  evaluationId,
  evaluation,
  children,
}: EvaluationStatusWithHistoryProps) {
  const { formatMessage } = useIntl();
  const toast = useToast();
  const popoverId = useId();
  const triggerRef = useRef<HTMLSpanElement>(null);
  const popoverRef = useRef<HTMLDivElement>(null);
  const hoverCapableRef = useRef(isHoverCapable());
  const [open, setOpen] = useState(false);
  const [pinned, setPinned] = useState(false);
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null);
  const { items, loading, error } = useEvaluationStatusHistory(evaluationId);

  useEffect(() => {
    if (error) toast.error(error);
  }, [error, toast]);

  const title = formatMessage({ id: 'evaluation.statusHistoryTitle' });
  const countSuffix = !loading && items.length > 0 ? ` (${items.length})` : '';
  const isReturned = evaluation ? isReturnedEvaluation(evaluation) : false;
  const hasHistory = !loading && items.length > 0;

  const updatePosition = useCallback(() => {
    const el = triggerRef.current;
    if (!el) return;

    const rect = el.getBoundingClientRect();
    const popoverHeight = popoverRef.current?.offsetHeight ?? 240;
    const spaceBelow = window.innerHeight - rect.bottom - VIEWPORT_PADDING;
    const showAbove = spaceBelow < popoverHeight && rect.top > spaceBelow;
    const left = Math.min(
      Math.max(VIEWPORT_PADDING, rect.right - POPOVER_WIDTH),
      window.innerWidth - POPOVER_WIDTH - VIEWPORT_PADDING,
    );

    setPosition({
      top: showAbove ? rect.top - POPOVER_GAP - popoverHeight : rect.bottom + POPOVER_GAP,
      left,
    });
  }, []);

  const openPopover = useCallback(() => {
    updatePosition();
    setOpen(true);
  }, [updatePosition]);

  const closePopover = useCallback(() => {
    setOpen(false);
    setPinned(false);
  }, []);

  const isWithinTriggerOrPopover = useCallback((node: Node | null) => {
    if (!node) return false;
    return Boolean(triggerRef.current?.contains(node) || popoverRef.current?.contains(node));
  }, []);

  const handleMouseEnter = () => {
    if (hoverCapableRef.current) openPopover();
  };

  const handleMouseLeave = (event: MouseEvent<HTMLSpanElement>) => {
    if (!hoverCapableRef.current) return;
    if (isWithinTriggerOrPopover(event.relatedTarget as Node | null)) return;
    closePopover();
  };

  const handleClick = () => {
    if (hoverCapableRef.current) return;
    if (open) {
      closePopover();
      return;
    }
    openPopover();
    setPinned(true);
  };

  const handleBlur = (event: React.FocusEvent<HTMLSpanElement>) => {
    if (!hoverCapableRef.current || pinned) return;
    if (isWithinTriggerOrPopover(event.relatedTarget as Node | null)) return;
    closePopover();
  };

  useEffect(() => {
    if (!open) return;
    updatePosition();
    const onScrollOrResize = () => updatePosition();
    window.addEventListener('scroll', onScrollOrResize, true);
    window.addEventListener('resize', onScrollOrResize);
    return () => {
      window.removeEventListener('scroll', onScrollOrResize, true);
      window.removeEventListener('resize', onScrollOrResize);
    };
  }, [open, updatePosition, items.length, loading]);

  useEffect(() => {
    if (!pinned || !open) return;
    const onDocumentPointerDown = (event: Event) => {
      const target = event.target as Node | null;
      if (!isWithinTriggerOrPopover(target)) closePopover();
    };
    document.addEventListener('mousedown', onDocumentPointerDown);
    document.addEventListener('touchstart', onDocumentPointerDown);
    return () => {
      document.removeEventListener('mousedown', onDocumentPointerDown);
      document.removeEventListener('touchstart', onDocumentPointerDown);
    };
  }, [pinned, open, closePopover, isWithinTriggerOrPopover]);

  useEffect(() => {
    if (!open) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') closePopover();
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, closePopover]);

  const className = [
    'evaluation-status-with-history',
    isReturned ? 'evaluation-status-with-history--returned' : '',
    hasHistory ? 'evaluation-status-with-history--has-history' : '',
  ].filter(Boolean).join(' ');

  return (
    <span
      ref={triggerRef}
      className={className}
      tabIndex={0}
      role="button"
      aria-expanded={open}
      aria-controls={popoverId}
      aria-label={`${title}${countSuffix}`}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      onFocus={() => {
        if (hoverCapableRef.current) openPopover();
      }}
      onBlur={handleBlur}
      onClick={handleClick}
      onKeyDown={(event) => {
        if (event.key === 'Escape') {
          closePopover();
          return;
        }
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          if (open) closePopover();
          else {
            openPopover();
            if (!hoverCapableRef.current) setPinned(true);
          }
        }
      }}
    >
      {children}
      {open && position && createPortal(
        <div
          ref={popoverRef}
          id={popoverId}
          className="evaluation-status-history-popover"
          style={{ top: position.top, left: position.left, width: POPOVER_WIDTH }}
          role="dialog"
          aria-label={title}
          onMouseEnter={() => {
            if (hoverCapableRef.current) setOpen(true);
          }}
          onMouseLeave={(event) => {
            if (!hoverCapableRef.current) return;
            if (isWithinTriggerOrPopover(event.relatedTarget as Node | null)) return;
            closePopover();
          }}
        >
          <p className="evaluation-status-history-popover__title">
            {title}{countSuffix}
          </p>
          <EvaluationStatusHistoryPanel items={items} loading={loading} error={error} />
        </div>,
        document.body,
      )}
    </span>
  );
}
