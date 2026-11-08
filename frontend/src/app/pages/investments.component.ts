import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-investments',
  imports: [RouterLink],
  template: `
    <main class="placeholder-page container">
      <a routerLink="/painel" class="back-link">← Voltar ao painel</a>
      <span class="eyebrow">EM BREVE</span>
      <div class="placeholder-icon">↗</div>
      <h1>Investimentos</h1>
      <p>Esta área está reservada para uma próxima etapa. Por enquanto, você pode acompanhar o retorno dos seus investimentos registrando-o como uma receita.</p>
      <a routerLink="/painel" class="button button-primary">Voltar ao painel</a>
    </main>
  `,
})
export class InvestmentsComponent {}
