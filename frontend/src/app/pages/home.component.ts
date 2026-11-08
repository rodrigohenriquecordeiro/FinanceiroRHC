import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <main class="landing container">
      <section class="hero-panel">
        <div class="hero-copy">
          <span class="eyebrow">FINANÇAS COM MAIS CLAREZA</span>
          <h1>Seu dinheiro,<br /><em>no seu ritmo.</em></h1>
          <p>Organize receitas, despesas e benefícios em um só lugar. Um panorama simples para decisões melhores, mês após mês.</p>
          <div class="hero-actions">
            <a class="button button-primary" routerLink="/cadastro">Criar minha conta <span>→</span></a>
            <a class="button button-quiet" routerLink="/login">Já tenho uma conta</a>
          </div>
          <div class="landing-note"><span class="note-dot"></span> Visão mensal e anual, sempre atualizada</div>
        </div>
        <div class="hero-art" aria-hidden="true">
          <div class="art-orbit orbit-one"></div><div class="art-orbit orbit-two"></div>
          <div class="balance-card">
            <span class="balance-label">SALDO PROJETADO</span><strong>R$ 8.420,50</strong>
            <div class="balance-trend"><span>↗</span> Seu mês em equilíbrio</div>
            <div class="mini-chart"><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i><i></i></div>
            <div class="chart-labels"><span>01</span><span>15</span><span>30</span></div>
          </div>
          <div class="floating-chip chip-one"><span>↗</span><div><small>Receitas</small><b>+ R$ 5.200</b></div></div>
          <div class="floating-chip chip-two"><span class="chip-icon">✳</span><div><small>Despesas pagas</small><b>R$ 1.840</b></div></div>
          <div class="art-sun"></div>
        </div>
      </section>
      <section class="landing-features">
        <div><span>01</span><h2>Um mês de cada vez</h2><p>Encontre rapidamente o que entrou, o que saiu e o que ainda vem pela frente.</p></div>
        <div><span>02</span><h2>Do seu jeito</h2><p>Contas, categorias, cartões e benefícios configurados para sua rotina.</p></div>
        <div><span>03</span><h2>Visão do todo</h2><p>Compare seus meses e veja o ano ganhar forma com um resumo visual.</p></div>
      </section>
    </main>
  `,
})
export class HomeComponent {}
