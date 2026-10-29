data "aws_caller_identity" "current" {}

# Publicados pelo garageos-infra-k8s. Se o cluster estiver destruido, o plan
# falha aqui com ParameterNotFound - antes de criar qualquer recurso.
data "aws_ssm_parameter" "oidc_provider_arn" {
  name = "/${var.project}/${local.env}/eks/oidc-provider-arn"
}

data "aws_ssm_parameter" "namespace" {
  name = "/${var.project}/${local.env}/eks/namespace/${local.service}"
}
# Rede e cracha de acesso ao banco, publicados pelo bootstrap e pelo
# garageos-infra-database (ADR 0001). Os nos do EKS carregam o cracha
# rds-client; o SG do RDS deste servico aceita a porta 5432 de quem o
# carrega. O isolamento ENTRE servicos nao e por rede (os pods dividem
# os mesmos nos), e sim por credencial: so este servico recebe o
# segredo com a senha deste banco.
data "aws_ssm_parameter" "vpc_id" {
  name = "/${var.project}/vpc/id"
}

data "aws_ssm_parameter" "private_subnet_ids" {
  name = "/${var.project}/vpc/private-subnet-ids"
}

data "aws_ssm_parameter" "rds_client_sg_id" {
  name = "/${var.project}/${local.env}/rds/client-security-group-id"
}

locals {
  account_id         = data.aws_caller_identity.current.account_id
  oidc_provider_arn  = nonsensitive(data.aws_ssm_parameter.oidc_provider_arn.value)
  oidc_issuer        = replace(local.oidc_provider_arn, "/^.*oidc-provider[/]/", "")
  namespace          = nonsensitive(data.aws_ssm_parameter.namespace.value)
  vpc_id             = nonsensitive(data.aws_ssm_parameter.vpc_id.value)
  private_subnet_ids = split(",", nonsensitive(data.aws_ssm_parameter.private_subnet_ids.value))
  rds_client_sg_id   = nonsensitive(data.aws_ssm_parameter.rds_client_sg_id.value)
}
