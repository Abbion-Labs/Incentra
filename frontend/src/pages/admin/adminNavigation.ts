export interface PageBackState {

  backTo: string;

  backLabelKey: string;

}



export function adminEmployeeProfileState(): PageBackState {

  return { backTo: '/admin/crud/employees', backLabelKey: 'admin.backToEmployees' };

}



export function adminEvaluatorAnalyticsState(): PageBackState {

  return { backTo: '/admin/crud/evaluator-settings', backLabelKey: 'admin.backToEvaluators' };

}

