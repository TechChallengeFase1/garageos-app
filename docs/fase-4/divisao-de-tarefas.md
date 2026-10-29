# Fase 4 — Divisão de tarefas

Proposta para o grupo preencher. Cada frente tem um dono, que pode puxar ajuda das outras.

| Frente | Dono | Escopo |
|---|---|---|
| **OS Service e saga** | _a definir_ | Extração do OS, máquina de estados da saga, histórico de status, BDD do fluxo completo |
| **Billing e Mercado Pago** | _a definir_ | Orçamento e pagamento em DynamoDB, integração com o Mercado Pago e webhook |
| **Execução** | _a definir_ | Estoque e reserva de peças, fila de execução, aging |
| **Infra, CI/CD e observabilidade** | _a definir_ | t3.medium, namespaces, Traefik, `infra/` de cada serviço, pipelines, SonarCloud, New Relic e dashboard da saga |

## Combinados

- Contratos de mensagem (`Contracts/V1`) mudam só por PR revisado pelos donos dos serviços afetados.
- Gate de 80% de cobertura e Sonar ligados desde o primeiro commit de cada serviço.
- Ambiente na AWS destruído ao fim de cada sessão de trabalho.

## Datas

| Marco | Data |
|---|---|
| Os 3 serviços sobem no cluster, sem saga | _a definir_ |
| Saga completa, com compensações | _a definir_ |
| Ensaio do vídeo | _a definir_ |
| **Gravação do vídeo** | _a definir_ |
| Entrega no portal | _a definir_ |
