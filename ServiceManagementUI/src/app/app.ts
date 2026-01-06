import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { Navbar } from './components/navbar/navbar';
import { MatMenuModule } from '@angular/material/menu';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterModule, Navbar, MatMenuModule],
  template: `
    <app-navbar></app-navbar>
    <router-outlet></router-outlet>
  `
})
export class App {}