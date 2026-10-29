variable "aws_region" {
  description = "Regiao. A mesma do bootstrap - a VPC nao atravessa regioes."
  type        = string
  default     = "us-east-1"
}

variable "project" {
  description = "Prefixo dos recursos"
  type        = string
  default     = "garageos"
}

variable "service_account" {
  description = "Nome da ServiceAccount dos pods, que o manifesto k8s/ do servico cria"
  type        = string
  default     = "os"
}

variable "db_name" {
  description = "Nome do banco criado dentro da instancia"
  type        = string
  default     = "garageos_os"
}

variable "db_username" {
  description = "Usuario master. Nao pode ser postgres, admin nem rdsadmin."
  type        = string
  default     = "garageos_os"
}

variable "db_instance_class" {
  description = "db.t4g.micro e elegivel ao free tier (750 h/mes, somando todas as instancias)"
  type        = string
  default     = "db.t4g.micro"
}

variable "allocated_storage" {
  description = "Armazenamento em GB. 20 e o minimo do RDS."
  type        = number
  default     = 20
}

variable "backup_retention_days" {
  description = "Dias de backup automatico. Ambiente descartavel."
  type        = number
  default     = 1
}

variable "postgres_version" {
  description = "So o major: a AWS aplica o minor mais recente sem gerar diff."
  type        = string
  default     = "16"
}
