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
import { ProcessesComponent } from './pages/processes/processes';

export const routes: Routes = [
	{ path: '', component: LoginComponent, title: 'Login' },
	{ path: 'home', component: HomeComponent, title: 'Home' },
	{ path: 'clients/:id', component: ClientDetailsComponent, title: 'Client Details' },
	{ path: 'lawyers/:id', component: LawyerDetailsComponent, title: 'Lawyer Details' },
	{ path: 'lawyers/:id/edit', component: EditLawyerComponent, title: 'Edit Lawyer' },
	{ path: 'clients/:id/edit', component: EditClientComponent, title: 'Edit Client' },
	{ path: 'clients', component: ClientsComponent, title: 'Clients' },
	{ path: 'lawyers', component: LawyersComponent, title: 'Lawyers' },
	{ path: 'chatbot', component: ChatbotComponent, title: 'ChatBot' },
	{ path: 'processes', component: ProcessesComponent, title: 'Processes' },
	{ path: 'profiles', component: ProfilesComponent, title: 'Profiles' },
	{ path: 'settings', component: SettingsComponent, title: 'Settings' },
	{ path: 'create-client', component: CreateClientComponent, title: 'Create Client' },
	{ path: 'create-lawyer', component: CreateLawyerComponent, title: 'Create Lawyer' },
	{ path: 'login', component: LoginComponent },
	{ path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})

export class AppRoutingModule { }
