import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  selector: 'app-root',
  templateUrl: './app.html',
})
export class App {
  currentYear = new Date().getFullYear();

  isLoggedIn() {
    return !!localStorage.getItem('financeiro-rhc-token');
  }
}
