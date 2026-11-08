import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="auth-wrap register-wrap">
      <section class="auth-card">
        <a routerLink="/" class="back-link">← Início</a>
        <span class="eyebrow">SEU ESPAÇO FINANCEIRO</span>
        <h1>Vamos começar?</h1>
        <p class="auth-intro">Crie sua conta e personalize seu painel em poucos passos.</p>
        <form (ngSubmit)="submit()">
          <div class="form-grid">
            <label>Seu nome<input name="name" [(ngModel)]="name" autocomplete="name" required /></label>
            <label>E-mail<input type="email" name="email" [(ngModel)]="email" autocomplete="email" required /></label>
            <label>Senha <small>(8+ caracteres, com maiúscula, minúscula e número)</small><input type="password" name="password" [(ngModel)]="password" minlength="8" autocomplete="new-password" required /></label>
            <label>Empresa <small>(opcional)</small><input name="companyName" [(ngModel)]="companyName" /></label>
            <label>Nome do benefício<input name="benefitName" [(ngModel)]="benefitName" required /></label>
            <label>Cartões de crédito <small>(separados por vírgula)</small><input name="cards" [(ngModel)]="cards" placeholder="Itaú, Nubank" required /></label>
          </div>
          @if (error) { <p class="form-error">{{ error }}</p> }
          <button class="button button-primary button-full" [disabled]="loading">{{ loading ? 'Criando sua conta...' : 'Criar conta' }}</button>
        </form>
        <p class="auth-switch">Já tem cadastro? <a routerLink="/login">Fazer login</a></p>
      </section>
      <aside class="auth-aside register-aside"><div class="aside-mark">✳</div><p>Seu planejamento deve funcionar para você — e não o contrário.</p><span>PERSONALIZE. ORGANIZE. SIGA EM FRENTE.</span></aside>
    </main>
  `,
})
export class RegisterComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  name = '';
  email = '';
  password = '';
  companyName = '';
  benefitName = 'Flash';
  cards = 'Itaú';
  error = '';
  loading = false;

  submit() {
    this.error = '';
    this.loading = true;
    this.api.register({
      name: this.name,
      email: this.email,
      password: this.password,
      companyName: this.companyName,
      benefitName: this.benefitName,
      creditCards: this.cards.split(',').map((card) => card.trim()).filter(Boolean),
    }).subscribe({
      next: (response) => {
        localStorage.setItem('financeiro-rhc-token', response.token);
        localStorage.setItem('financeiro-rhc-name', response.name);
        void this.router.navigateByUrl('/painel');
      },
      error: (error: { error?: { message?: string; errors?: Record<string, string[]> } }) => {
        this.error = error.error?.message
          ?? Object.values(error.error?.errors ?? {}).flat().join(' ')
          ?? 'Não foi possível criar a conta. Verifique os dados e tente novamente.';
        this.loading = false;
      },
    });
  }
}
