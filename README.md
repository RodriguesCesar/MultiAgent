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
  }
}
```

Alternativa por variável de ambiente:

```bash
OpenAI__ApiKey=sua-chave-aqui
OpenAI__Model=gpt-4o
```

## Executar

```bash
dotnet run --project MultiAgent.API/MultiAgent.API.csproj
```

## Endpoint

- `POST /api/agent`
- `GET /health`

Exemplo de payload:

```json
{
  "message": "Crie um plano para uma API de cadastro de produtos."
}
```
