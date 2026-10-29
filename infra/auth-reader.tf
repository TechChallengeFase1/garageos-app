# Role somente leitura da Lambda de login por CPF.
#
# A Lambda (garageos-lambda-auth) le a tabela "Clientes", que pertence a este
# servico. Ela e tratada como componente de borda do OS Service, e nao como um
# servico com banco proprio: por isso acessa este banco, mas nunca com o usuario
# master. A role auth_reader so tem SELECT nas colunas que a Lambda usa.
#
# Quem cria a role dentro do PostgreSQL e o Job k8s/jobs/db-bootstrap-job.yaml,
# rodando sql/criar-auth-reader.sql depois das migrations. O Terraform so
# sorteia a senha e publica onde busca-la. O runner do GitHub nao alcanca o RDS
# privado, entao o provider postgresql nao serviria aqui.

resource "random_password" "auth_reader" {
  length  = 32
  special = false
}

resource "aws_secretsmanager_secret" "auth_reader" {
  name                    = "${local.name}/rds/auth-reader"
  description             = "Credencial somente leitura da Lambda de login por CPF (${local.env})"
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "auth_reader" {
  secret_id = aws_secretsmanager_secret.auth_reader.id

  secret_string = jsonencode({
    username = "auth_reader"
    password = random_password.auth_reader.result
    host     = aws_db_instance.main.address
    port     = aws_db_instance.main.port
    dbname   = var.db_name
  })
}

# Contrato com a Lambda (ADR 0001): endereco do banco e ponteiro para o segredo.
resource "aws_ssm_parameter" "rds_endpoint" {
  name        = "${local.prefix}/rds/endpoint"
  description = "Hostname do RDS do OS Service (sem a porta)"
  type        = "String"
  value       = aws_db_instance.main.address
}

resource "aws_ssm_parameter" "rds_port" {
  name  = "${local.prefix}/rds/port"
  type  = "String"
  value = tostring(aws_db_instance.main.port)
}

resource "aws_ssm_parameter" "rds_dbname" {
  name  = "${local.prefix}/rds/dbname"
  type  = "String"
  value = var.db_name
}

resource "aws_ssm_parameter" "auth_reader_secret_arn" {
  name        = "${local.prefix}/rds/auth-reader-secret-arn"
  description = "ARN do segredo com a credencial somente leitura da Lambda de login"
  type        = "String"
  value       = aws_secretsmanager_secret.auth_reader.arn
}
