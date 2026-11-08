# Financeiro RHC

Aplicação SPA para organizar receitas, despesas, compras no cartão e saldo de vale-alimentação. Os dados financeiros ficam separados por usuário; a área de investimentos é somente um espaço reservado no MVP.

## Executar com Docker Compose

1. Copie `.env.example` para `.env` na raiz do projeto e, se necessário, ajuste as credenciais locais.
2. Na raiz, execute:

   ```powershell
   docker compose up --build -d
   ```

3. Acesse:
   - Aplicação Angular: <http://localhost:4201>
   - API REST: <http://localhost:5002>
   - MySQL: `localhost:3308`

O `bibliotecarhc` já ocupa a porta 3307; por isso, para não interrompê-lo, este projeto publica o MySQL em 3308. O Compose mantém nomes e volume próprios (`financeiro-rhc-*`) e não altera os containers do outro sistema. A API cria o esquema inicial ao iniciar. Para parar os serviços sem apagar os dados, use `docker compose down`; o volume do MySQL só é removido com uma ação explícita (`docker compose down -v`).

As credenciais do `.env.example` são apenas para desenvolvimento local. Troque-as antes de qualquer implantação compartilhada ou pública. O arquivo `.env` é ignorado pelo Git.

## Executar a API localmente

Inicie o MySQL pelo Compose e configure os segredos locais do ASP.NET Core:

```powershell
docker compose --env-file .env.example up -d mysql
dotnet user-secrets init --project backend\FinanceiroRHC.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3308;Database=financeirorhc;User=financeiro;Password=LocalOnly_RHC_MySQL_User_2026" --project backend\FinanceiroRHC.Api
dotnet user-secrets set "Jwt:Key" "Escolha-uma-chave-local-com-pelo-menos-32-bytes" --project backend\FinanceiroRHC.Api
dotnet run --project backend\FinanceiroRHC.Api
```

Para executar o Angular fora do container, em outro terminal:

```powershell
npm --prefix frontend install
npm --prefix frontend start -- --host 0.0.0.0 --port 4201
```

O endereço da API usado pelo navegador é `http://localhost:5002/api`.

## Testes

```powershell
dotnet test FinanceiroRHC.slnx
npm --prefix frontend test -- --watch=false
npm --prefix frontend run build
docker compose --env-file .env.example config --quiet
```

## Estrutura

- `backend/FinanceiroRHC.Api`: API .NET 10, ASP.NET Identity, JWT, EF Core, MySQL, repositórios, serviços e controllers.
- `backend/FinanceiroRHC.Tests`: testes xUnit dos cálculos financeiros.
- `frontend`: Angular standalone com Bootstrap 5, navegação mensal/anual, autenticação e formulários de edição.
- `docker-compose.yml`: containers isolados para MySQL, API e Angular, publicados nas portas 3307, 5002 e 4201.

O saldo inicial de cada mês é a diferença acumulada entre receitas e despesas anteriores, ou o último saldo inicial ajustado. A projeção subtrai todas as despesas; o saldo disponível subtrai somente as despesas pagas. O saldo do benefício acumula recargas e compras registradas até o mês selecionado.
