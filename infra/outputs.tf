output "ecr_url" {
  value = aws_ecr_repository.app.repository_url
}

output "irsa_role_arn" {
  value = aws_iam_role.irsa.arn
}

output "rds_endpoint" {
  value = aws_db_instance.main.address
}

output "db_secret_arn" {
  value = aws_secretsmanager_secret.db.arn
}
