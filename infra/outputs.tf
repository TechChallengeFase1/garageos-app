output "cluster_name" {
  description = "Nome do cluster kind provisionado"
  value       = kind_cluster.garageos.name
}

output "cluster_endpoint" {
  description = "Endpoint da API do cluster Kubernetes"
  value       = kind_cluster.garageos.endpoint
}

output "kubeconfig_path" {
  description = "Caminho do kubeconfig gerado pelo kind (usado pelo kubectl e pela pipeline)"
  value       = kind_cluster.garageos.kubeconfig_path
}

output "namespace" {
  description = "Namespace onde a aplicacao deve ser implantada pelo CI/CD"
  value       = kubernetes_namespace.garageos.metadata[0].name
}

output "api_url" {
  description = "URL da API apos o CI/CD aplicar os manifestos de k8s/"
  value       = "http://localhost:${var.api_node_port}/swagger"
}
