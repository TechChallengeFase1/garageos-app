# Banco PostgreSQL proprio deste servico (ADR 0004).
#
# Copiado do garageos-infra-database, com identifier, db_name e segredo
# proprios. Nenhum outro servico recebe a senha deste banco.

resource "aws_db_subnet_group" "main" {
  name        = "${local.name}-rds"
  description = "Subnets privadas do RDS do ${local.service}"
  subnet_ids  = local.private_subnet_ids
}

resource "aws_security_group" "rds" {
  name        = "${local.name}-rds"
  description = "RDS do ${local.service}. Entrada apenas de quem tem o cracha rds-client."
  vpc_id      = local.vpc_id
}

resource "aws_vpc_security_group_ingress_rule" "postgres_do_cliente" {
  security_group_id            = aws_security_group.rds.id
  referenced_security_group_id = local.rds_client_sg_id
  description                  = "PostgreSQL vindo dos nos do EKS (cracha rds-client)"
  ip_protocol                  = "tcp"
  from_port                    = 5432
  to_port                      = 5432
}

resource "random_password" "master" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}:?."
}

resource "aws_db_instance" "main" {
  identifier     = "${local.name}-postgres"
  engine         = "postgres"
  engine_version = var.postgres_version
  instance_class = var.db_instance_class

  allocated_storage = var.allocated_storage
  storage_type      = "gp2"
  storage_encrypted = true

  db_name  = var.db_name
  username = var.db_username
  password = random_password.master.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  publicly_accessible    = false
  multi_az               = false

  backup_retention_period    = var.backup_retention_days
  auto_minor_version_upgrade = true
  apply_immediately          = true

  # Ambiente descartavel, recriado a cada sessao de trabalho.
  skip_final_snapshot          = true
  deletion_protection          = false
  performance_insights_enabled = false
}

resource "aws_secretsmanager_secret" "db" {
  name                    = "${local.name}/rds/master"
  description             = "Credenciais do RDS do ${local.service} (${local.env})"
  recovery_window_in_days = 0
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db.id

  secret_string = jsonencode({
    username = var.db_username
    password = random_password.master.result
    host     = aws_db_instance.main.address
    port     = aws_db_instance.main.port
    dbname   = var.db_name
    connectionString = join(";", [
      "Host=${aws_db_instance.main.address}",
      "Port=${aws_db_instance.main.port}",
      "Database=${var.db_name}",
      "Username=${var.db_username}",
      "Password=${random_password.master.result}",
      "SSL Mode=Require",
      "Trust Server Certificate=true",
    ])
  })
}
