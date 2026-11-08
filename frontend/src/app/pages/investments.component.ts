import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ApiService } from '../core/api.service';
import { Investment, InvestmentInput } from '../core/models';

@Component({
  selector: 'app-investments',
  imports: [CurrencyPipe, DatePipe, FormsModule, RouterLink],
  template: `
    <main class="investment-page">
      <a routerLink="/painel" class="back-link">← Voltar ao painel</a>
      <header class="dashboard-heading investment-dashboard-heading">
        <div><span class="eyebrow">PATRIMÔNIO INVESTIDO</span><h1>Investimentos</h1><p>Acompanhe suas aplicações, vencimentos e retiradas.</p></div>
        <div class="period-control"><button type="button" (click)="shiftMonth(-1)" aria-label="Mês anterior">‹</button><strong>{{ monthLabel }} <span>{{ year }}</span></strong><button type="button" (click)="shiftMonth(1)" aria-label="Próximo mês">›</button></div>
      </header>
      @if (error) { <div class="alert-message error-message" role="alert">{{ error }}</div> }
      @if (notice) { <div class="alert-message success-message" role="status">{{ notice }}</div> }

      @if (!loading) {
        <section class="metric-grid investment-metrics">
          <article class="metric-card metric-primary"><div class="metric-top"><span>Total investido no mês</span><span class="metric-icon">✧</span></div><strong>{{ monthAppliedTotal | currency:'BRL' }}</strong><small>Aportes realizados em {{ monthLabel }}</small></article>
          <article class="metric-card"><div class="metric-top"><span>Saldo disponível</span><span class="metric-icon green-icon">◉</span></div><strong>{{ total | currency:'BRL' }}</strong><small>Valor aplicado ainda investido</small></article>
          <article class="metric-card"><div class="metric-top"><span>Ganhos e perdas no mês</span><span class="metric-icon orange-icon">↗</span></div><strong>{{ monthReturnTotal | currency:'BRL' }}</strong><small>Resultado registrado nas retiradas</small></article>
        </section>
      }

      <section class="investment-layout">
        <article class="panel editor-panel">
          <div class="section-heading"><div><span class="eyebrow">NOVA APLICAÇÃO</span><h2>{{ editingId ? 'Editar investimento' : 'Aplicar novo investimento' }}</h2></div></div>
          <form (ngSubmit)="save()">
            <label>Descrição do investimento<input name="description" [(ngModel)]="form.description" placeholder="Ex.: CDB, Tesouro Direto" required /></label>
            <div class="field-pair"><label>Data da aplicação<input type="date" name="applicationDate" [(ngModel)]="form.applicationDate" required /></label><label>Vencimento<input type="date" name="maturityDate" [(ngModel)]="form.maturityDate" required /></label></div>
            <label>Valor aplicado (R$)<input type="number" min="0.01" step="0.01" name="amount" [(ngModel)]="form.amount" required /></label>
            <p class="form-hint">O aporte será lançado automaticamente como despesa paga para ajustar seu saldo.</p>
            <div class="investment-form-actions"><button class="button button-primary" [disabled]="saving">{{ saving ? 'Salvando...' : editingId ? 'Salvar alterações' : 'Salvar aplicação' }}</button>@if (editingId) { <button type="button" class="button button-light" (click)="resetForm()">Cancelar edição</button> }</div>
          </form>
        </article>

        <article class="panel investment-list-panel">
          <div class="section-heading"><div><span class="eyebrow">SUAS APLICAÇÕES</span><h2>Carteira de investimentos</h2></div><span class="record-count">{{ investments.length }} registros</span></div>
          @if (loading) {
            <div class="loading-panel"><span class="loader"></span> Carregando investimentos...</div>
          } @else if (investments.length) {
            <div class="investment-list">
              @for (investment of investments; track investment.id) {
                <article class="investment-card">
                  <div class="investment-card-heading"><div><span class="eyebrow">APLICAÇÃO</span><h3>{{ investment.description }}</h3></div><strong>{{ investment.currentTotal | currency:'BRL' }}</strong></div>
                  <div class="investment-details"><span>Aplicado <b>{{ investment.applicationDate | date:'dd/MM/yyyy' }}</b></span><span>Vencimento <b>{{ investment.maturityDate | date:'dd/MM/yyyy' }}</b></span><span>Valor inicial <b>{{ investment.amount | currency:'BRL' }}</b></span><span>Retirado <b>{{ investment.withdrawnAmount | currency:'BRL' }}</b></span></div>
                  <div class="investment-actions"><button type="button" (click)="edit(investment)">Editar</button><button type="button" (click)="openWithdrawal(investment)" [disabled]="investment.currentTotal <= 0">Retirar</button><button type="button" class="danger-action" (click)="askDelete(investment)">Excluir</button></div>
                  @if (investment.withdrawals.length) {
                    <div class="withdrawal-list"><strong>Retiradas</strong>@for (withdrawal of investment.withdrawals; track withdrawal.id) { <div><span>{{ withdrawal.withdrawalDate | date:'dd/MM/yyyy' }}</span><b>{{ withdrawal.amount | currency:'BRL' }}</b>@if (withdrawal.returnAmount !== 0) { <small [class.positive-return]="withdrawal.returnAmount > 0" [class.negative-return]="withdrawal.returnAmount < 0">{{ withdrawal.returnAmount > 0 ? 'Ganho' : 'Perda' }}: {{ withdrawal.returnAmount | currency:'BRL' }}</small> }<button type="button" class="danger-action" (click)="askDeleteWithdrawal(investment, withdrawal.id)">Excluir</button></div> }</div>
                  }
                </article>
              }
            </div>
          } @else {
            <div class="empty-state"><span>↗</span><strong>Nenhum investimento cadastrado</strong><small>Cadastre uma aplicação para começar a acompanhar seu patrimônio.</small></div>
          }
        </article>
      </section>
    </main>

    @if (withdrawalInvestment) {
      <div class="modal-backdrop" (click)="withdrawalInvestment = null">
        <section class="modal-card" role="dialog" aria-modal="true" aria-labelledby="withdraw-title" (click)="$event.stopPropagation()">
          <button type="button" class="modal-close" (click)="withdrawalInvestment = null" aria-label="Fechar">×</button>
          <span class="eyebrow">RETIRADA DE INVESTIMENTO</span><h2 id="withdraw-title">{{ withdrawalInvestment.description }}</h2>
          <p>Saldo disponível: <strong>{{ withdrawalInvestment.currentTotal | currency:'BRL' }}</strong></p>
          <form class="withdrawal-form" (ngSubmit)="withdraw()">
            <label>Data da retirada<input type="date" name="withdrawalDate" [(ngModel)]="withdrawalDate" required /></label>
            <label>Valor retirado do principal (R$)<input type="number" min="0.01" [max]="withdrawalInvestment.currentTotal" step="0.01" name="withdrawalAmount" [(ngModel)]="withdrawalAmount" required /></label>
            <label>Ganho ou perda (R$)<input type="number" [min]="-withdrawalAmount" step="0.01" name="withdrawalReturnAmount" [(ngModel)]="withdrawalReturnAmount" aria-describedby="withdrawal-return-hint" /></label>
            <p class="form-hint" id="withdrawal-return-hint">Informe um valor positivo para ganho ou negativo para perda. Ganhos entram em Receitas; perdas são registradas em Despesas.</p>
            <div class="delete-actions"><button type="button" class="button button-light" (click)="withdrawalInvestment = null">Cancelar</button><button type="submit" class="button button-primary" [disabled]="saving">Confirmar retirada</button></div>
          </form>
        </section>
      </div>
    }

    @if (pendingDelete) {
      <div class="modal-backdrop deletion-backdrop" (click)="pendingDelete = null">
        <section class="modal-card delete-modal" role="dialog" aria-modal="true" aria-labelledby="investment-delete-title" (click)="$event.stopPropagation()">
          <span class="delete-modal-icon">!</span><span class="eyebrow">CONFIRMAÇÃO</span><h2 id="investment-delete-title">Excluir registro?</h2>
          <p>{{ pendingDelete.message }}</p>
          <div class="delete-actions"><button type="button" class="button button-light" (click)="pendingDelete = null">Cancelar</button><button type="button" class="button button-danger" [disabled]="saving" (click)="confirmDelete()">Sim, excluir</button></div>
        </section>
      </div>
    }
  `,
})
export class InvestmentsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);
  readonly monthNames = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];
  investments: Investment[] = [];
  month = new Date().getMonth() + 1;
  year = new Date().getFullYear();
  loading = true;
  saving = false;
  error = '';
  notice = '';
  editingId?: number;
  withdrawalInvestment: Investment | null = null;
  withdrawalDate = this.today();
  withdrawalAmount = 0;
  withdrawalReturnAmount = 0;
  pendingDelete: { message: string; investmentId: number; withdrawalId?: number } | null = null;
  form: InvestmentInput = this.emptyForm();

  ngOnInit() {
    if (!localStorage.getItem('financeiro-rhc-token')) {
      void this.router.navigateByUrl('/login');
      return;
    }
    this.load();
  }

  get total() {
    return this.investments.reduce((sum, item) => sum + item.currentTotal, 0);
  }

  get monthLabel() {
    return this.monthNames[this.month - 1];
  }

  get monthAppliedTotal() {
    return this.investments
      .filter((item) => this.inMonth(item.applicationDate))
      .reduce((sum, item) => sum + item.amount, 0);
  }

  get monthReturnTotal() {
    return this.investments
      .flatMap((item) => item.withdrawals)
      .filter((item) => this.inMonth(item.withdrawalDate))
      .reduce((sum, item) => sum + item.returnAmount, 0);
  }

  shiftMonth(delta: number) {
    const date = new Date(this.year, this.month - 1 + delta, 1);
    this.month = date.getMonth() + 1;
    this.year = date.getFullYear();
  }

  save() {
    this.saving = true;
    this.error = '';
    this.api.saveInvestment(this.form, this.editingId).subscribe({
      next: () => {
        this.notice = this.editingId ? 'Investimento atualizado.' : 'Aplicação salva e despesa registrada.';
        this.resetForm();
        this.load();
      },
      error: (error: unknown) => this.failed(error),
    });
  }

  edit(investment: Investment) {
    this.editingId = investment.id;
    this.form = {
      description: investment.description,
      applicationDate: investment.applicationDate,
      maturityDate: investment.maturityDate,
      amount: investment.amount,
    };
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  resetForm() {
    this.editingId = undefined;
    this.form = this.emptyForm();
  }

  openWithdrawal(investment: Investment) {
    this.withdrawalInvestment = investment;
    this.withdrawalDate = this.today();
    this.withdrawalAmount = investment.currentTotal;
    this.withdrawalReturnAmount = 0;
  }

  withdraw() {
    if (!this.withdrawalInvestment) return;
    this.saving = true;
    this.api.withdrawInvestment(this.withdrawalInvestment.id, {
      withdrawalDate: this.withdrawalDate,
      amount: this.withdrawalAmount,
      returnAmount: this.withdrawalReturnAmount,
    }).subscribe({
      next: () => {
        this.notice = 'Retirada registrada e resultado refletido no mês.';
        this.withdrawalInvestment = null;
        this.load();
      },
      error: (error: unknown) => this.failed(error),
    });
  }

  askDelete(investment: Investment) {
    this.pendingDelete = {
      message: `Deseja excluir “${investment.description}”? O aporte, as retiradas e os lançamentos de ganhos ou perdas associados também serão removidos.`,
      investmentId: investment.id,
    };
  }

  askDeleteWithdrawal(investment: Investment, withdrawalId: number) {
    this.pendingDelete = {
      message: 'Deseja excluir esta retirada? Os lançamentos correspondentes em Receitas e Despesas também serão removidos.',
      investmentId: investment.id,
      withdrawalId,
    };
  }

  confirmDelete() {
    const pending = this.pendingDelete;
    if (!pending) return;
    this.saving = true;
    const request: Observable<unknown> = pending.withdrawalId
      ? this.api.deleteInvestmentWithdrawal(pending.investmentId, pending.withdrawalId)
      : this.api.deleteInvestment(pending.investmentId);
    request.subscribe({
      next: () => {
        this.pendingDelete = null;
        this.notice = 'Registro excluído.';
        this.load();
      },
      error: (error: unknown) => this.failed(error),
    });
  }

  private load() {
    this.loading = true;
    this.api.getInvestments().subscribe({
      next: (investments) => {
        this.investments = investments;
        this.loading = false;
        this.saving = false;
        this.changeDetector.markForCheck();
      },
      error: (error: unknown) => this.failed(error),
    });
  }

  private failed(error: unknown) {
    const response = error as { error?: { message?: string }; message?: string };
    this.error = response.error?.message ?? response.message ?? 'Não foi possível concluir. Verifique a conexão com a API e tente novamente.';
    this.loading = false;
    this.saving = false;
    this.changeDetector.markForCheck();
  }

  private emptyForm(): InvestmentInput {
    const applicationDate = this.today();
    const maturity = new Date(`${applicationDate}T00:00:00`);
    maturity.setFullYear(maturity.getFullYear() + 1);
    return {
      description: '',
      applicationDate,
      maturityDate: `${maturity.getFullYear()}-${String(maturity.getMonth() + 1).padStart(2, '0')}-${String(maturity.getDate()).padStart(2, '0')}`,
      amount: 0,
    };
  }

  private inMonth(value: string) {
    const date = new Date(`${value}T00:00:00`);
    return date.getFullYear() === this.year && date.getMonth() + 1 === this.month;
  }

  private today() {
    const date = new Date();
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
