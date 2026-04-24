# Feature Ordem de Serviço

## Issues:

### Criar estrutura base da Ordem de Serviço #11
Nos endpoints de OS somente deixar sem JWT um endpoint especifico de GET passando o numero da OS para o cliente conseguir visualizar o seu progresso
Este endpoint irá retornar principalmente o Status da OS, além de retornar quais serviços que estão incluídos na OS


### CRUD da Ordem de Serviço #12
-> Table: OrdemDeServico

    Id
    CriadoEm
    FinalizadaEm
    AtualizadoEm
    Numero OS (alfanumerico)
    Status (enum: Recebida, Em diagnóstico, Aguardando aprovação, Em execução, Finalizada, Entregue)
    fk: ClienteId
    fk: VeiculoId
    fk: Lista de Estoque (Peças e insumos)
    fk: Lista de Serviços (N:n)

-> Status (OrdemDeServico)

    Recebida: Criou a OS
    Em diagnóstico: Se incluir Peças e insumos ou Serviços
    Aguardando aprovação: Quando gerar o orçamento (valor do orçamento é gerado automaticamente a partir dos serviços)
    Em execução: Orçamento com o status aprovado
    Finalizada: Somente chamar endpoint "Alterar status" (passando o id da OS e o Status para alterar)
    Entregue: Somente chamar endpoint "Alterar status" (passando o id da OS e o Status para alterar)



### Fluxo da geração de orçamento #13


Esta relacionada com esta task #14

-> Criar uma table de Orcamento

    Id
    Status (enum: Aprovado ou Rejeitado)
    Verificar se tem uma entidade para herdar (CriadoEm, AlteradoEm, .....)
    fk: OrdemServicoId
    Preco (gerado automaticamente com a soma do preco de todos os servicos)


### API de acompanhamento da Ordem de Serviço #16

-> Table: OrdemDeServico

    Id
    CriadoEm
    FinalizadaEm
    AtualizadoEm
    Numero OS (alfanumerico)
    Status (enum: Recebida, Em diagnóstico, Aguardando aprovação, Em execução, Finalizada, Entregue)
    fk: ClienteId
    fk: VeiculoId
    fk: Lista de Estoque (Peças e insumos)
    fk: Lista de Serviços (N:n)

-> Status (OrdemDeServico)

    Recebida: Criou a OS
    Em diagnóstico: Se incluir Peças e insumos ou Serviços
    Aguardando aprovação: Quando gerar o orçamento (valor do orçamento é gerado automaticamente a partir dos serviços)
    Em execução: Orçamento com o status aprovado
    Finalizada: Somente chamar endpoint "Alterar status" (passando o id da OS e o Status para alterar)
    Entregue: Somente chamar endpoint "Alterar status" (passando o id da OS e o Status para alterar)



### Monitoramento de tempo de execução do Serviço #19
-> Table (Intermediaria) OrdemServico : Servico

fk: OrdemDeServico
fk: Servico
Status (enum: Iniciado e finalizado)
CriadoEm
InciadaEm
FinalizadaEm

-> Status (OrdemServico : Servico)

    Inciado: endpoint "Alterar Status" e seta InciadaEm dateTimeNow
    Finalizado: endpoint "Alterar Status" e seta FinalizadaEm dateTimeNow
    Desta forma conseguimos pegar o tempo medio atraves de uma conta feita no back, com um endpoint que passa o id do serviço e outro que lista todos os tempos medios do serviços finalizados, assim temos uma massa que ja esta filtrada pelo servicoId, ou não se for listar, e também pelo status finalizado -> Assim fazendo a conta com os campos InciadaEm e FinalizadaEm


### Fluxo de aprovação do cliente #14

Fazer esta task após Fluxo da geração de orçamento #13
Verificar se há a necessidade de realmente configurar o envio de email via SMTP, podemos criar um e-mail somente para isso ou usar o da pós
Após o cliente aprovar temos que setar o status da tabela de Orcamento o status para aprovado
