variable "cluster_name" {
  description = "Nome do cluster kind criado pelo Terraform"
  type        = string
  default     = "garageos"
}

variable "namespace" {
  description = "Namespace onde a aplicacao e o banco sao provisionados"
  type        = string
  default     = "garageos"
}

variable "api_node_port" {
  description = "NodePort da API exposto no host (deve casar com k8s/api-service.yaml)"
  type        = number
  default     = 30080
}

variable "node_image" {
  description = "Imagem do no do kind. v1.30.0 mantem compatibilidade com cgroup v1 (WSL2/Docker Desktop)"
  type        = string
  default     = "kindest/node:v1.30.0"
}

variable "postgres_user" {
  description = "Usuario do PostgreSQL"
  type        = string
  default     = "garageos"
}

variable "postgres_db" {
  description = "Nome do banco de dados"
  type        = string
  default     = "garageos"
}

variable "postgres_password" {
  description = "Senha do PostgreSQL (deve casar com a connection string em k8s/secret.yaml)"
  type        = string
  default     = "garageos@123"
  sensitive   = true
}
