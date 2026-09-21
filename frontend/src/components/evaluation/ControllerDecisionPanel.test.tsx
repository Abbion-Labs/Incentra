import { render, screen } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { describe, expect, it } from 'vitest';
import { ControllerDecisionPanel } from './ControllerDecisionPanel';

function renderPanel(saving: boolean) {
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <ControllerDecisionPanel
        controllerComment=""
        revisionComment=""
        saving={saving}
        onControllerCommentChange={() => undefined}
        onRevisionCommentChange={() => undefined}
        onApprove={() => undefined}
        onReturnForRevision={() => undefined}
      />
    </IntlProvider>,
  );
}

describe('ControllerDecisionPanel', () => {
  it('lets the controller edit comments when idle', () => {
    renderPanel(false);

    expect(screen.getByLabelText('controller.commentOptional')).toHaveProperty(
      'disabled',
      false,
    );
    expect(
      screen.getByLabelText('controller.revisionCommentRequired'),
    ).toHaveProperty('disabled', false);
  });

  it('locks both comments while a decision is being sent', () => {
    renderPanel(true);

    expect(screen.getByLabelText('controller.commentOptional')).toHaveProperty(
      'disabled',
      true,
    );
    expect(
      screen.getByLabelText('controller.revisionCommentRequired'),
    ).toHaveProperty('disabled', true);
  });
});
