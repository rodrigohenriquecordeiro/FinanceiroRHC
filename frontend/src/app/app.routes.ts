import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard.component';
import { HomeComponent } from './pages/home.component';
import { InvestmentsComponent } from './pages/investments.component';
import { LoginComponent } from './pages/login.component';
import { RegisterComponent } from './pages/register.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: 'Financeiro RHC · Organize suas finanças' },
  { path: 'login', component: LoginComponent, title: 'Entrar · Financeiro RHC' },
  { path: 'cadastro', component: RegisterComponent, title: 'Criar conta · Financeiro RHC' },
  { path: 'painel', component: DashboardComponent, title: 'Meu painel · Financeiro RHC' },
  { path: 'investimentos', component: InvestmentsComponent, title: 'Investimentos · Financeiro RHC' },
  { path: '**', redirectTo: '' },
];
