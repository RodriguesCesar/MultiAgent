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
    "AgentApiKey": "",
    "AgentApiKeys": [
      "sua-api-key-ativa",
      "sua-api-key-em-rotacao"
    ],
    "AllowAnonymousInDevelopment": false
  }
}
```

Alternativa por variável de ambiente:

```bash
OpenAI__ApiKey=sua-chave-aqui
OpenAI__Model=gpt-4o
Authentication__AgentApiKeys__0=sua-api-key-ativa
Authentication__AgentApiKeys__1=sua-api-key-em-rotacao
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
- rotação de API key (chave ativa + chave de transição)

## Endpoint

- `POST /api/agent`
- `GET /health`

### Autenticação do endpoint de agente

`POST /api/agent` exige o header `X-Agent-Api-Key` com uma chave válida configurada em:

- `Authentication:AgentApiKeys` (recomendado, lista de chaves para rotação sem downtime)
- `Authentication:AgentApiKey` (compatibilidade com chave única)

- Em ambientes **não Development**, a chave é obrigatória.
- Em **Development**, é possível permitir bypass configurando `Authentication:AllowAnonymousInDevelopment=true` (apenas para desenvolvimento local).

#### Rotação sem downtime (recomendado)

1. Adicione nova chave em `Authentication:AgentApiKeys`, mantendo a antiga.
2. Atualize os clientes para usar a nova chave.
3. Remova a chave antiga da lista após migração completa.

### Rate limiting

O endpoint `POST /api/agent` possui limitação de taxa configurável para reduzir abuso:

- `RateLimiting:Agent:PermitLimit` (padrão: `20`)
- `RateLimiting:Agent:WindowSeconds` (padrão: `60`)
- `RateLimiting:Agent:QueueLimit` (padrão: `0`)

Ao exceder o limite, a API retorna `429 Too Many Requests` com `ProblemDetails` e `traceId`.

### Observabilidade de segurança

A API registra eventos de segurança com logs estruturados (incluindo `traceId`) para:

- tentativas com API key inválida
- bloqueios por rate limiting
- erro de configuração de autenticação

Também publica métricas via `System.Diagnostics.Metrics`:

- `agent_auth_success_total`
- `agent_auth_failures_total`
- `agent_auth_config_errors_total`
- `agent_rate_limit_rejections_total`

As métricas são expostas no endpoint:

- `GET /metrics` (formato Prometheus)

> Observação: o exporter Prometheus do OpenTelemetry para ASP.NET Core ainda é distribuído como pacote `beta` no ecossistema OpenTelemetry .NET atual.

#### Integração com Prometheus e Grafana

Exemplo de scrape no `prometheus.yml`:

```yaml
scrape_configs:
  - job_name: "multiagent-api"
    metrics_path: /metrics
    static_configs:
      - targets: ["localhost:5132"]
```

No Grafana, crie painéis com consultas como:

- `sum(rate(agent_auth_failures_total[5m]))`
- `sum(rate(agent_rate_limit_rejections_total[5m]))`
- `sum(rate(agent_auth_success_total[5m]))`

### Stack local de observabilidade (Docker Compose)

Arquivos prontos em [observability/](/C:/CesarCurso/MultiAgent-work/observability/):

- [docker-compose.yml](/C:/CesarCurso/MultiAgent-work/observability/docker-compose.yml)
- [prometheus.yml](/C:/CesarCurso/MultiAgent-work/observability/prometheus/prometheus.yml)
- [alerts.yml](/C:/CesarCurso/MultiAgent-work/observability/prometheus/alerts.yml)
- [multiagent-security.json](/C:/CesarCurso/MultiAgent-work/observability/grafana/dashboards/multiagent-security.json)

Passos:

1. Execute a API localmente (`dotnet run`) na porta `5132`.
2. Suba Prometheus e Grafana:

```bash
docker compose -f observability/docker-compose.yml up -d
```

3. Acesse:
   - Prometheus: `http://localhost:9090`
   - Grafana: `http://localhost:3000` (`admin` / `admin`)

4. O dashboard **MultiAgent Security Observability** será provisionado automaticamente.

5. Alertas de segurança ficam disponíveis em Prometheus > **Alerts**:
   - `MultiAgentHighAuthFailureRate`
   - `MultiAgentHighRateLimitRejections`
   - `MultiAgentAuthConfigError`

Para encerrar:

```bash
docker compose -f observability/docker-compose.yml down
```

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

## Melhorias realizadas no projeto

### Segurança

- remoção de segredo OpenAI hardcoded (agora via configuração/ambiente)
- autenticação de `POST /api/agent` por API key
- suporte a rotação de API key com múltiplas chaves válidas
- respostas de erro seguras com `ProblemDetails` e `traceId`
- logs e métricas para eventos críticos de autenticação/rate limiting

### Confiabilidade e operação

- health check em `GET /health`
- rate limiting configurável para mitigar abuso
- propagação de `CancellationToken` no fluxo de execução

### Qualidade e entrega

- suíte inicial de testes unitários e de integração
- CI no GitHub Actions (restore + build + test)
- atualização de dependências críticas e correção de vulnerabilidades conhecidas
- endpoint de métricas para monitoramento contínuo em Prometheus/Grafana
