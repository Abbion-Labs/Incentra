import { createBrowserRouter, Navigate, Outlet, useParams } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { useIntl } from './i18n';
import { LoginPage } from './pages/LoginPage';
import { HomeRedirect } from './pages/HomeRedirect';
import { EvaluatorHomePage } from './pages/evaluator/EvaluatorHomePage';
import { EvaluatorGoalsDashboard } from './pages/evaluator/EvaluatorGoalsDashboard';
import { EvaluatorEmployeePage } from './pages/evaluator/EvaluatorEmployeePage';
import { EvaluatorDashboard } from './pages/evaluator/EvaluatorDashboard';
import { EvaluationEditorPage } from './pages/evaluator/EvaluationEditorPage';
import { EvaluatorAnalyticsPage } from './pages/evaluator/EvaluatorAnalyticsPage';
import { GoalsPlanningPage } from './pages/evaluator/GoalsPlanningPage';
import { ControllerHomePage } from './pages/controller/ControllerHomePage';
import { ControllerWorkflowPage } from './pages/controller/ControllerWorkflowPage';
import { ControllerEmployeePage } from './pages/controller/ControllerEmployeePage';
import { ControllerEvaluatorsPage } from './pages/controller/ControllerEvaluatorsPage';
import { ControllerEvaluatorAnalyticsPage } from './pages/controller/ControllerEvaluatorAnalyticsPage';
import { ControllerReviewPage } from './pages/controller/ControllerReviewPage';
import { AdminCrudLayout } from './pages/admin/AdminCrudLayout';
import { AdminVarijabilaLayout } from './pages/admin/AdminVarijabilaLayout';
import { AdminEmployees } from './pages/admin/AdminEmployees';
import { AdminUsers } from './pages/admin/AdminUsers';
import { AdminEvaluatorSettings } from './pages/admin/AdminEvaluatorSettings';
import { AdminLookups } from './pages/admin/AdminLookups';
import { AdminRatingConfig } from './pages/admin/AdminRatingConfig';
import { AdminCompensation } from './pages/admin/AdminCompensation';
import { AdminCompensationResults } from './pages/admin/AdminCompensationResults';
import { AdminCompensationAnalytics } from './pages/admin/AdminCompensationAnalytics';
import { AdminSalaries } from './pages/admin/AdminSalaries';
import { EmployeeEvaluationsPage } from './pages/employee/EmployeeEvaluationsPage';
import { EmployeeEvaluationDetailPage } from './pages/employee/EmployeeEvaluationDetailPage';
import { AccountSettingsPage } from './pages/AccountSettingsPage';

function GoalsPlanRedirect() {
  const { id } = useParams<{ id: string }>();
  return <Navigate to={`/evaluator/goals/evaluations/${id}`} replace />;
}

function AuthLoadingGate() {
  const { formatMessage } = useIntl();
  const { loading } = useAuth();

  if (loading) {
    return <div className="empty">{formatMessage({ id: 'common.loading' })}</div>;
  }

  return <Outlet />;
}

export const router = createBrowserRouter([
  {
    element: <AuthLoadingGate />,
    children: [
      { path: '/login', element: <LoginPage /> },
      {
        path: '/',
        element: (
          <ProtectedRoute>
            <HomeRedirect />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <EvaluatorHomePage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/workflow',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <EvaluatorDashboard />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/employees/:employeeId',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <EvaluatorEmployeePage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/goals',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <EvaluatorGoalsDashboard />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/analytics',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <EvaluatorAnalyticsPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/goals/evaluations/:id',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <GoalsPlanningPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/evaluations/:id/plan',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <GoalsPlanRedirect />
          </ProtectedRoute>
        ),
      },
      {
        path: '/evaluator/evaluations/:id',
        element: (
          <ProtectedRoute roles={['EVALUATOR', 'ADMIN']}>
            <EvaluationEditorPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/controller',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <ControllerHomePage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/controller/workflow',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <ControllerWorkflowPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/controller/employees/:employeeId',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <ControllerEmployeePage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/controller/evaluators',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <ControllerEvaluatorsPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/controller/evaluators/:evaluatorEmployeeId/analytics',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <ControllerEvaluatorAnalyticsPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/controller/evaluations/:id',
        element: (
          <ProtectedRoute roles={['CONTROLLER', 'ADMIN']}>
            <ControllerReviewPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/employee',
        element: (
          <ProtectedRoute roles={['EMPLOYEE', 'ADMIN']}>
            <EmployeeEvaluationsPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/employee/evaluations/:id',
        element: (
          <ProtectedRoute roles={['EMPLOYEE', 'ADMIN']}>
            <EmployeeEvaluationDetailPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/account',
        element: (
          <ProtectedRoute roles={['ADMIN', 'EVALUATOR', 'CONTROLLER', 'EMPLOYEE', 'PAYROLL']}>
            <AccountSettingsPage />
          </ProtectedRoute>
        ),
      },
      {
        path: '/admin',
        element: (
          <ProtectedRoute roles={['ADMIN']}>
            <Navigate to="/admin/crud/employees" replace />
          </ProtectedRoute>
        ),
      },
      {
        path: '/admin/crud',
        element: (
          <ProtectedRoute roles={['ADMIN']}>
            <AdminCrudLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Navigate to="employees" replace /> },
          { path: 'employees', element: <AdminEmployees /> },
          { path: 'users', element: <AdminUsers /> },
          { path: 'evaluator-settings', element: <AdminEvaluatorSettings /> },
          { path: 'lookups', element: <AdminLookups /> },
          { path: 'rating-config', element: <AdminRatingConfig /> },
        ],
      },
      {
        path: '/admin/varijabila',
        element: (
          <ProtectedRoute roles={['PAYROLL']}>
            <AdminVarijabilaLayout />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <Navigate to="salaries" replace /> },
          { path: 'salaries', element: <AdminSalaries /> },
          { path: 'compensation', element: <AdminCompensation /> },
          { path: 'results', element: <AdminCompensationResults /> },
          { path: 'analytics', element: <AdminCompensationAnalytics /> },
        ],
      },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
]);
