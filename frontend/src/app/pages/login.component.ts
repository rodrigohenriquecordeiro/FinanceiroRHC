import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-wrap">
      <section class="auth-card">
        <a routerLink="/" class="back-link">← Início</a>
        <span class="eyebrow">BOM TER VOCÊ DE VOLTA</span>
        <h1>Entre na sua conta</h1>
        <p class="auth-intro">Suas finanças estão a um passo de ficarem organizadas.</p>
        <form (ngSubmit)="submit()">
          <label>E-mail<input type="email" name="email" [(ngModel)]="email" autocomplete="email" required /></label>
          <label>Senha<input type="password" name="password" [(ngModel)]="password" autocomplete="current-password" required /></label>
          @if (error) { <p class="form-error">{{ error }}</p> }
          <button class="button button-primary button-full" [disabled]="loading">{{ loading ? 'Aguarde...' : 'Entrar' }}</button>
        </form>
        <p class="auth-switch">Ainda não tem uma conta? <a routerLink="/cadastro">Cadastre-se</a></p>
      </section>
      <aside class="auth-aside"><div class="aside-mark">“</div><p>Pequenas escolhas conscientes hoje constroem a tranquilidade de amanhã.</p><span>SEU FUTURO FINANCEIRO COMEÇA AQUI</span></aside>
    </main>
  `,
})
export class LoginComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  email = '';
  password = '';
  error = '';
  loading = false;

  submit() {
    this.error = '';
    this.loading = true;
    this.api.login(this.email, this.password).subscribe({
      next: (response) => {
        localStorage.setItem('financeiro-rhc-token', response.token);
        localStorage.setItem('financeiro-rhc-name', response.name);
        void this.router.navigateByUrl('/painel');
      },
      error: (error: { error?: { message?: string } }) => {
        this.error = error.error?.message ?? 'Não foi possível entrar. Verifique sua conexão e tente novamente.';
        this.loading = false;
      },
    });
  }
}
