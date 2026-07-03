# Providers conectam ao cluster kind criado em cluster.tf, usando o kubeconfig
# gerado pelo proprio kind (mais robusto que passar os certificados soltos, que
# podem apontar para um endpoint/porta inconsistente).

provider "kind" {}

provider "kubernetes" {
  config_path = kind_cluster.garageos.kubeconfig_path
}

provider "helm" {
  kubernetes {
    config_path = kind_cluster.garageos.kubeconfig_path
  }
}

provider "kubectl" {
  config_path      = kind_cluster.garageos.kubeconfig_path
  load_config_file = true
}
