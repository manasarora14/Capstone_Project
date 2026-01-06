import { Routes } from '@angular/router';
import { roleGuard } from './guards/role-guard';


import { Login } from './components/login/login';
import { Register } from './components/register/register';
import { Dashboard } from './components/dashboard/dashboard';
import { AdminDashboard } from './components/admin-dashboard/admin-dashboard';
import { AdminPanel } from './components/admin/admin-panel';
import { AdminCategories } from './components/admin/admin-categories';
import { Reports } from './components/reports/reports';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  
 
  { 
    path: 'dashboard', 
    component: Dashboard, 
    canActivate: [roleGuard], 
    data: { roles: ['Admin', 'Manager', 'Technician', 'Customer'] } 
  },

  
 
  { 
  path: 'admin', 
  canActivate: [roleGuard], 
  data: { roles: ['Admin'] },
  children: [
    
    { path: 'dashboard', component: AdminDashboard }, 
    
    
    { path: 'users', component: AdminPanel }, 
    
    { path: 'categories', component: AdminCategories }, 
    
    
    
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
  ]
},
  
 


{ 
    path: 'reports', 
    loadComponent: () => import('./components/reports/reports').then(m => m.Reports),
    canActivate: [roleGuard],
    data: { roles: ['Manager'] }
  },
  
  { 
    path: 'assign', 
    loadComponent: () => import('./components/manager/assign-request').then(m => m.AssignRequest),
    canActivate: [roleGuard],
    data: { roles: ['Manager'] }
  },
  { 
    path: 'monitor', 
    loadComponent: () => import('./components/manager/monitor-progress').then(m => m.MonitorProgress),
    canActivate: [roleGuard],
    data: { roles: ['Manager', 'Admin'] }
  },
  { 
    path: 'billing', 
    loadComponent: () => import('./components/billing/billing').then(m => m.Billing),
    canActivate: [roleGuard],
    data: { roles: ['Customer'] } 
  },

 
  { 
    path: 'tasks', 
    loadComponent: () => import('./components/technician/task-list').then(m => m.TaskList),
    canActivate: [roleGuard],
    data: { roles: ['Technician'] }
  },

  {
    path: 'tech/workload',
    loadComponent: () => import('./components/technician/workload').then(m => m.TechnicianWorkload),
    canActivate: [roleGuard],
    data: { roles: ['Technician'] }
  },

 
  { 
    path: 'create-request', 
    loadComponent: () => import('./components/customer/create-request').then(m => m.CreateRequest),
    canActivate: [roleGuard],
    data: { roles: ['Customer'] }
  },
  { 
    path: 'my-services', 
    loadComponent: () => import('./components/customer/my-service').then(m => m.MyServices),
    canActivate: [roleGuard],
    data: { roles: ['Customer'] }
  },
  
 
  {
    path: 'service-details/:id',
    loadComponent: () => import('./components/service-details/service-details').then(m => m.ServiceDetails),
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Manager', 'Technician', 'Customer'] }
  },
  
  
  {
    path: 'service-catalog',
    loadComponent: () => import('./components/service-catalog/service-catalog').then(m => m.ServiceCatalog),
    canActivate: [roleGuard],
    data: { roles: ['Customer'] }
  },

  
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];