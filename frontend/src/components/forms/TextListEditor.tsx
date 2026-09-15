import type { TextItemDraft } from '../../utils/goalsPlanning';
import { useIntl } from '../../i18n';

interface TextListEditorProps {
  items: TextItemDraft[];
  setItems: (items: TextItemDraft[]) => void;
  placeholder: string;
  addLabel: string;
}

export function TextListEditor({ items, setItems, placeholder, addLabel }: TextListEditorProps) {
  const { formatMessage } = useIntl();

  return (
    <>
      <div className="form-list">
        {items.map((item, idx) => (
          <div key={idx} className="form-list__item">
            <span className="form-list__index">{idx + 1}</span>
            <textarea
              className="form-list__input"
              rows={2}
              value={item.description}
              onChange={(e) => {
                const next = [...items];
                next[idx] = { ...item, description: e.target.value };
                setItems(next);
              }}
              placeholder={placeholder}
            />
            {items.length > 1 && (
              <button
                type="button"
                className="btn btn-ghost btn-sm"
                onClick={() => setItems(items.filter((_, i) => i !== idx))}
                aria-label={formatMessage({ id: 'common.removeItem' })}
              >
                {formatMessage({ id: 'common.remove' })}
              </button>
            )}
          </div>
        ))}
      </div>
      <button
        type="button"
        className="btn btn-secondary btn-sm"
        onClick={() => setItems([...items, { description: '', sortOrder: items.length }])}
      >
        {addLabel}
      </button>
    </>
  );
}
