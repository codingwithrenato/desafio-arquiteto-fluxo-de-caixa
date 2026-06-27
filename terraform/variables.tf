variable "environment" {
  description = "Nome do ambiente (dev, staging, prod)."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Região do Azure."
  type        = string
  default     = "brazilsouth"
}

variable "tenant_id" {
  description = "Tenant ID do Azure AD (para o Key Vault)."
  type        = string
}

variable "registry" {
  description = "Container registry (ex.: acrfluxocaixa.azurecr.io)."
  type        = string
}

variable "image_tag" {
  description = "Tag das imagens a implantar."
  type        = string
  default     = "latest"
}

variable "pg_admin_login" {
  description = "Login do administrador do PostgreSQL."
  type        = string
  default     = "fcadmin"
}

variable "pg_admin_password" {
  description = "Senha do administrador do PostgreSQL (injetar via TF_VAR / pipeline secret)."
  type        = string
  sensitive   = true
}
