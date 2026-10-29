import { lazy, type ComponentType } from 'react';
import {
  createBrowserRouter,
  Navigate,
  Outlet,
  useParams,
} from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { AppShell } from './components/AppLayout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { useIntl } from './i18n';
import { LoginPage } from './pages/LoginPage';
import { HomeRedirect } from './pages/HomeRedirect';

// Stranice se preuzimaju tek kad zatrebaju, pa niko ne skida kod uloga koje nema.
function lazyPage<K extends string>(
  load: () => Promise<Record<K, ComponentType>>,
  name: K,
) {
  return lazy(() => load().then((module) => ({ default: module[name] })));
}

const EvaluatorHomePage = lazyPage(
  () => import('./pages/evaluator/EvaluatorHomePage'),
  'EvaluatorHomePage',
);
const EvaluatorGoalsDashboard = lazyPage(
  () => import('./pages/evaluator/EvaluatorGoalsDashboard'),
  'EvaluatorGoalsDashboard',
);
const EvaluatorEmployeePage = lazyPage(
  () => import('./pages/evaluator/EvaluatorEmployeePage'),
  'EvaluatorEmployeePage',
);
const EvaluatorDashboard = lazyPage(
  () => import('./pages/evaluator/EvaluatorDashboard'),
  'EvaluatorDashboard',
);
const EvaluationEditorPage = lazyPage(
  () => import('./pages/evaluator/EvaluationEditorPage'),
  'EvaluationEditorPage',
);
const EvaluatorAnalyticsPage = lazyPage(
  () => import('./pages/evaluator/EvaluatorAnalyticsPage'),
  'EvaluatorAnalyticsPage',
);
const GoalsPlanningPage = lazyPage(
  () => import('./pages/evaluator/GoalsPlanningPage'),
  'GoalsPlanningPage',
);
const ControllerHomePage = lazyPage(
  () => import('./pages/controller/ControllerHomePage'),
  'ControllerHomePage',
);
const ControllerWorkflowPage = lazyPage(
  () => import('./pages/controller/ControllerWorkflowPage'),
  'ControllerWorkflowPage',
);
const ControllerEmployeePage = lazyPage(
  () => import('./pages/controller/ControllerEmployeePage'),
  'ControllerEmployeePage',
);
const ControllerEvaluatorsPage = lazyPage(
  () => import('./pages/controller/ControllerEvaluatorsPage'),
  'ControllerEvaluatorsPage',
);
const ControllerEvaluatorAnalyticsPage = lazyPage(
  () => import('./pages/controller/ControllerEvaluatorAnalyticsPage'),
  'ControllerEvaluatorAnalyticsPage',
);
const ControllerReviewPage = lazyPage(
  () => import('./pages/controller/ControllerReviewPage'),
  'ControllerReviewPage',
);
const AdminCrudLayout = lazyPage(
  () => import('./pages/admin/AdminCrudLayout'),
  'AdminCrudLayout',
);
const AdminVarijabilaLayout = lazyPage(
  () => import('./pages/admin/AdminVarijabilaLayout'),
  'AdminVarijabilaLayout',
);
const AdminEmployees = lazyPage(
  () => import('./pages/admin/AdminEmployees'),
  'AdminEmployees',
);
const AdminUsers = lazyPage(
  () => import('./pages/admin/AdminUsers'),
  'AdminUsers',
);
const AdminEvaluatorSettings = lazyPage(
  () => import('./pages/admin/AdminEvaluatorSettings'),
  'AdminEvaluatorSettings',
);
const AdminLookups = lazyPage(
  () => import('./pages/admin/AdminLookups'),
  'AdminLookups',
);
const AdminRatingConfig = lazyPage(
  () => import('./pages/admin/AdminRatingConfig'),
  'AdminRatingConfig',
);
const AdminCompensation = lazyPage(
  () => import('./pages/admin/AdminCompensation'),
  'AdminCompensation',
);
const AdminCompensationResults = lazyPage(
  () => import('./pages/admin/AdminCompensationResults'),
  'AdminCompensationResults',
);
const AdminCompensationAnalytics = lazyPage(
  () => import('./pages/admin/AdminCompensationAnalytics'),
  'AdminCompensationAnalytics',
);
const AdminSalaries = lazyPage(
  () => import('./pages/admin/AdminSalaries'),
  'AdminSalaries',
);
const EmployeeEvaluationsPage = lazyPage(
  () => import('./pages/employee/EmployeeEvaluationsPage'),
  'EmployeeEvaluationsPage',
);
const EmployeeEvaluationDetailPage = lazyPage(
  () => import('./pages/employee/EmployeeEvaluationDetailPage'),
  'EmployeeEvaluationDetailPage',
);
const AccountSettingsPage = lazyPage(
  () => import('./pages/AccountSettingsPage'),
  'AccountSettingsPage',
);

function GoalsPlanRedirect() {
  const { id } = useParams<{ id: string }>();
  return <Navigate to={`/evaluator/goals/evaluations/${id}`} replace />;
}

function AuthLoadingGate() {
  const { formatMessage } = useIntl();
  const { loading } = useAuth();

  if (loading) {
    return (
      <div className="empty">{formatMessage({ id: 'common.loading' })}</div>
    );
  }

  return <Outlet />;
}

export const router = createBrowserRouter([
  {
    element: <AuthLoadingGate />,
    children: [
      { path: '/login', element: <LoginPage /> },
      {
        element: <AppShell />,
        children: [
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
              <ProtectedRoute roles={['EVALUATOR']}>
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
              <ProtectedRoute
                roles={[
                  'ADMIN',
                  'EVALUATOR',
                  'CONTROLLER',
                  'EMPLOYEE',
                  'PAYROLL',
                ]}
              >
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
              {
                path: 'employees',
                element: <AdminEmployees />,
              },
              {
                path: 'users',
                element: <AdminUsers />,
              },
              {
                path: 'evaluator-settings',
                element: <AdminEvaluatorSettings />,
              },
              {
                path: 'lookups',
                element: <AdminLookups />,
              },
              {
                path: 'rating-config',
                element: <AdminRatingConfig />,
              },
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
              {
                path: 'salaries',
                element: <AdminSalaries />,
              },
              {
                path: 'compensation',
                element: <AdminCompensation />,
              },
              {
                path: 'results',
                element: <AdminCompensationResults />,
              },
              {
                path: 'analytics',
                element: <AdminCompensationAnalytics />,
              },
            ],
          },
        ],
      },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
]);
