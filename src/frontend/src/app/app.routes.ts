import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './pages/home';
import { ClientsComponent } from './pages/clients';
import { ClientDetailsComponent } from './pages/client-details/client-details';
import { LawyerDetailsComponent } from './pages/lawyer-details/lawyer-details';
import { LawyersComponent } from './pages/lawyers';
import { ChatbotComponent } from './pages/chatbot';
import { ProfilesComponent } from './pages/profiles';
import { SettingsComponent } from './pages/settings';
import { CreateClientComponent } from './pages/create-client/create-client';
import { CreateLawyerComponent } from './pages/create-lawyer/create-lawyer';
import { LoginComponent } from './pages/login/login';
import { NgModule } from '@angular/core';
import { EditClientComponent } from './pages/edit-client/edit-client';
import { EditLawyerComponent } from './pages/edit-lawyer/edit-lawyer';
import { roleGuard } from './guards/role.guard';
import { CreateProcessComponent } from './pages/create-process/create-process';
import { ProcessDetailsComponent } from './pages/process-details/process-details';
import { ProcessesComponent } from './pages/processes/processes';

export const routes: Routes = [
  { path: '', component: LoginComponent, title: 'Login' },
  { path: 'home', component: HomeComponent, title: 'Home', canActivate: [roleGuard] },
  {
    path: 'clients/:id',
    component: ClientDetailsComponent,
    title: 'Client Details',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  {
    path: 'lawyers/:id',
    component: LawyerDetailsComponent,
    title: 'Lawyer Details',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  {
    path: 'lawyers/:id/edit',
    component: EditLawyerComponent,
    title: 'Edit Lawyer',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  {
    path: 'clients/:id/edit',
    component: EditClientComponent,
    title: 'Edit Client',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  {
    path: 'clients',
    component: ClientsComponent,
    title: 'Clients',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  {
    path: 'lawyers',
    component: LawyersComponent,
    title: 'Lawyers',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  { path: 'chatbot', component: ChatbotComponent, title: 'ChatBot', canActivate: [roleGuard] },
  {
    path: 'processes',
    component: ProcessesComponent,
    title: 'Processes',
    canActivate: [roleGuard],
    data: { roles: ['Client', 'Lawyer', 'Admin'] },
  },
  {
    path: 'processes/create',
    component: CreateProcessComponent,
    title: 'Create Process',
    canActivate: [roleGuard],
    data: { roles: ['Lawyer', 'Admin'] }
  },
  {
    path: 'processes/:id',
    component: ProcessDetailsComponent,
    title: 'Process Details',
    canActivate: [roleGuard],
    data: { roles: ['Client', 'Lawyer', 'Admin'] }
  },
  { path: 'profiles', component: ProfilesComponent, title: 'Profiles', canActivate: [roleGuard] },
  { path: 'settings', component: SettingsComponent, title: 'Settings', canActivate: [roleGuard] },
  {
    path: 'create-client',
    component: CreateClientComponent,
    title: 'Create Client',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  {
    path: 'create-lawyer',
    component: CreateLawyerComponent,
    title: 'Create Lawyer',
    canActivate: [roleGuard],
    data: { roles: ['Admin', 'Lawyer'] },
  },
  { path: 'login', component: LoginComponent },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
