import { fireEvent, render, screen } from '@testing-library/react';
import { IntlProvider } from 'react-intl';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import type { AdminUser, Employee } from '../../../api/types';
import {
  AdminUserForm,
  emptyUserForm,
  userToForm,
  type UserFormValues,
} from './AdminUserForm';

const ana = {
  id: 7,
  fullName: 'Ana Anić',
  organizationUnitName: 'IT',
} as Employee;

function Harness({ editingUser = null }: { editingUser?: AdminUser | null }) {
  const [values, setValues] = useState<UserFormValues>(
    editingUser ? userToForm(editingUser) : emptyUserForm(),
  );
  return (
    <AdminUserForm
      values={values}
      editingUser={editingUser}
      saving={false}
      controllerOptions={[]}
      employeeOptions={[ana]}
      alreadyConfiguredEvaluator={false}
      onChange={setValues}
      onSubmit={() => undefined}
      onCancel={() => undefined}
    />
  );
}

function renderForm(editingUser?: AdminUser) {
  render(
    <IntlProvider locale="sr" messages={{}} onError={() => undefined}>
      <Harness editingUser={editingUser ?? null} />
    </IntlProvider>,
  );
}

const createButton = () =>
  screen.getByRole('button', { name: 'admin.users.createUser' });

describe('AdminUserForm', () => {
  it('asks for the employee of a new account with the employee role', () => {
    renderForm();

    const employee = screen.getByLabelText('admin.users.employee');
    expect(createButton()).toHaveProperty('disabled', true);

    fireEvent.change(employee, { target: { value: '7' } });
    expect(createButton()).toHaveProperty('disabled', false);
  });

  it('lets a new account become an evaluator right away', () => {
    renderForm();

    fireEvent.click(screen.getByRole('checkbox', { name: 'roles.EVALUATOR' }));

    expect(screen.getByLabelText('admin.users.employee')).toBeTruthy();
    expect(
      screen.getByLabelText('admin.users.evaluatorController'),
    ).toBeTruthy();
  });

  it('needs no employee for an administrator or payroll account', () => {
    renderForm();

    fireEvent.click(screen.getByRole('checkbox', { name: 'roles.EMPLOYEE' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'roles.PAYROLL' }));

    expect(screen.queryByLabelText('admin.users.employee')).toBeNull();
    expect(createButton()).toHaveProperty('disabled', false);
  });

  it('refuses employee roles for an account without an employee', () => {
    renderForm({
      id: 3,
      version: 0,
      email: 'plate@local.dev',
      roles: ['PAYROLL'],
      isActive: true,
      employeeId: null,
      employeeFullName: null,
    } as AdminUser);

    fireEvent.click(screen.getByRole('checkbox', { name: 'roles.CONTROLLER' }));

    expect(
      screen.getByText('admin.users.rolesNeedLinkedEmployee'),
    ).toBeTruthy();
    expect(
      screen.getByRole('button', { name: 'buttons.saveChanges' }),
    ).toHaveProperty('disabled', true);
  });
});
