interface SectionAverageFooterProps {
  label: string;
  value: string;
}

export function SectionAverageFooter({ label, value }: SectionAverageFooterProps) {
  return (
    <div className="form-section__footer form-section__footer--average">
      <span className="section-average">
        {label}: <strong>{value}</strong>
      </span>
    </div>
  );
}
