---
name: domain-entities
description: "Entidades, value objects, enums e regras de negócio do domínio GarageOS"
metadata: 
  node_type: memory
  type: project
  originSessionId: 30c16610-3cae-44f4-be13-18f823207726
---

## Domínio GarageOS (Code/GarageOS.Domain/)

### Entidades

| Entidade | Descrição | Dependências |
|---|---|---|
| **Cliente** | Pessoa/empresa que leva veículos | Documento (VO), Endereco (VO) |
| **Veiculo** | Veículo a ser consertado | Vinculado a Cliente (FK opcional) |
| **Servico** | Tipo de serviço oferecido (ex: troca de óleo) | Independente |
| **Estoque** | Peças/itens em estoque | Gerenciamento de quantidade e status |
| **OrdemDeServico** | Agregado raiz principal | Cliente, Veiculo, Servicos, Estoques, Orcamento |
| **OrdemDeServicoServico** | Serviço associado a uma OS | Vincula Servico ↔ OrdemDeServico |
| **OrdemDeServicoEstoque** | Peça associada a uma OS | Vincula Estoque ↔ OrdemDeServico |
| **Orcamento** | Orçamento vinculado à OS | OrdemDeServico |

### Value Objects (Code/GarageOS.Domain/ValueObjects/)

- **Documento** — valida e armazena CPF ou CNPJ (algoritmo de validação incluso)
- **Endereco** — endereço completo (logradouro, número, bairro, cidade, estado, CEP)

### Enums (Code/GarageOS.Domain/Enums/)

- `StatusOrdemDeServico`: Recebida → EmDiagnostico → AguardandoAprovacao → EmExecucao → Finalizada → Entregue
- `StatusOrcamento`: Pendente → Aprovado | Rejeitado
- `StatusExecucaoServico`: Criada → Iniciado → Finalizado
- `StatusEstoque`: Disponivel | Indisponivel
- `TipoDocumento`: CPF | CNPJ

### Interfaces de Repositório (Code/GarageOS.Domain/Repositories/)

- `IClienteRepository`
- `IVeiculoRepository`
- `IServicoRepository`
- `IEstoqueRepository`
- `IOrdemDeServicoRepository`
- `IOrcamentoRepository`

### Regras de Negócio Importantes

**Cliente:**
- Documento (CPF/CNPJ) único — validado por algoritmo
- Email único, obrigatório
- Soft delete (campo `Ativo`)

**Veiculo:**
- Placa com validação regex (formato XXX9X99)
- Ano > 0 e Preço > 0
- FK para Cliente é opcional (Guid?)

**Estoque:**
- Status automático: `Disponivel` se Quantidade > 0, senão `Indisponivel`
- Baixa de estoque apenas se quantidade suficiente

**OrdemDeServico:**
- Número automático: `OS-{ANO}-{SEQUENCIAL:D5}` (ex: OS-2026-00001)
- Transições de status são controladas — não permite saltos arbitrários
- Alteração manual de status só permite → Finalizada ou → Entregue

**Orcamento:**
- Preço = Σ(Serviços.Preço) + Σ(Estoques.Valor × Quantidade)
- Apenas uma resposta por orçamento (aprovar ou rejeitar)
