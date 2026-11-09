# MultiAgent

API ASP.NET com orquestração multiagente usando Semantic Kernel.

## Segurança (ação imediata)

Este projeto tinha chave de API em código-fonte. Antes de usar:

1. Rotacione/revogue a chave OpenAI antiga imediatamente.
2. Configure a chave apenas por ambiente local/seguro.
3. Nunca versione segredos no repositório.

## Configuração

1. Copie o arquivo de exemplo:

```bash
cp MultiAgent.API/appsettings.Local.example.json MultiAgent.API/appsettings.Local.json
```

2. Defina a chave em `MultiAgent.API/appsettings.Local.json`:

```json
{
  "OpenAI": {
    "Model": "gpt-4o",
    "ApiKey": "sua-chave-aqui"
  },
  "Authentication": {
    "AgentApiKey": "sua-api-key-interna",
    "AllowAnonymousInDevelopment": false
  }
}
```

Alternativa por variável de ambiente:

```bash
OpenAI__ApiKey=sua-chave-aqui
OpenAI__Model=gpt-4o
Authentication__AgentApiKey=sua-api-key-interna
Authentication__AllowAnonymousInDevelopment=false
```

## Executar

```bash
dotnet run --project MultiAgent.API/MultiAgent.API.csproj
```

## Testes

```bash
dotnet test MultiAgent.slnx
```

Projeto de testes:

- `MultiAgent.API.Tests`

Coberturas iniciais:

- validações de entrada (`AgentRequest`)
- validação de configuração (`KernelFactory`)
- validação de argumento no serviço orquestrador (`AgentService`)
- health check (`GET /health`)
- rate limiting (`POST /api/agent` com resposta `429`)
- autenticação (`POST /api/agent` com resposta `401` sem/erro de API key)

## Endpoint

- `POST /api/agent`
- `GET /health`

### Autenticação do endpoint de agente

`POST /api/agent` exige o header `X-Agent-Api-Key` com o valor configurado em `Authentication:AgentApiKey`.

- Em ambientes **não Development**, a chave é obrigatória.
- Em **Development**, é possível permitir bypass configurando `Authentication:AllowAnonymousInDevelopment=true` (apenas para desenvolvimento local).

### Rate limiting

O endpoint `POST /api/agent` possui limitação de taxa configurável para reduzir abuso:

- `RateLimiting:Agent:PermitLimit` (padrão: `20`)
- `RateLimiting:Agent:WindowSeconds` (padrão: `60`)
- `RateLimiting:Agent:QueueLimit` (padrão: `0`)

Ao exceder o limite, a API retorna `429 Too Many Requests` com `ProblemDetails` e `traceId`.

Exemplo de payload:

```json
{
  "message": "Crie um plano para uma API de cadastro de produtos."
}
```

## CI

Workflow em `.github/workflows/ci.yml` executa:

1. restore
2. build (Release)
3. test
