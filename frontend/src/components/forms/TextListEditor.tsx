import type { TextItemDraft } from '../../utils/goalsPlanning';
import { TEXT_LIMITS } from '../../utils/textLimits';
import { AddItemButton, RemoveItemButton } from './ListItemButtons';

const maxLength = TEXT_LIMITS.planItem;
// Brojač se pojavljuje tek kad se tekst približi granici.
const showCountFrom = Math.floor(maxLength * 0.8);

interface TextListEditorProps {
  items: TextItemDraft[];
  setItems: (items: TextItemDraft[]) => void;
  placeholder: string;
  addLabel: string;
}

export function TextListEditor({
  items,
  setItems,
  placeholder,
  addLabel,
}: TextListEditorProps) {
  return (
    <>
      <div className="form-list">
        {items.map((item, idx) => (
          <div key={idx} className="form-list__item">
            <span className="form-list__index">{idx + 1}</span>
            <div className="form-list__field">
              <textarea
                className="form-list__input"
                rows={2}
                maxLength={maxLength}
                value={item.description}
                onChange={(e) => {
                  const next = [...items];
                  next[idx] = { ...item, description: e.target.value };
                  setItems(next);
                }}
                placeholder={placeholder}
              />
              {item.description.length >= showCountFrom && (
                <span className="form-list__count">
                  {item.description.length} / {maxLength}
                </span>
              )}
            </div>
            {items.length > 1 && (
              <RemoveItemButton
                onClick={() => setItems(items.filter((_, i) => i !== idx))}
              />
            )}
          </div>
        ))}
      </div>
      <div className="form-list__footer">
        <AddItemButton
          label={addLabel}
          onClick={() =>
            setItems([...items, { description: '', sortOrder: items.length }])
          }
        />
      </div>
    </>
  );
}
